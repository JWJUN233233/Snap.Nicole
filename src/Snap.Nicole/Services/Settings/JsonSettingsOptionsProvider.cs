using Microsoft.Extensions.FileProviders;
using Sentry;
using Snap.Nicole.Core;
using Snap.Nicole.Core.Diagnostics;
using Snap.Nicole.Core.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace Snap.Nicole.Services.Settings;

// A single change in the options can trigger the whole object graph to be serialized and written to disk,
// so it's recommended to use this provider only for relatively small options objects and avoid putting large data in them.
internal sealed class JsonSettingsOptionsProvider<TOptions> : IOptionsProvider<TOptions>, IDisposable
    where TOptions : class, INotifyPropertyChanged, ICopyFrom<TOptions>, new()
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly Lock syncRoot = new();

    private readonly string filePath;
    private readonly string fileName;

    private readonly PhysicalFileProvider fileProvider;
    private readonly List<INotifyPropertyChanged> observableChildren = [];
    private readonly TOptions cachedValue;

    private IDisposable? watchRegistration;
    private bool isExternalChange;
    private volatile bool disposed;

    public JsonSettingsOptionsProvider(string fileNameWithoutExtension)
    {
        string directory = WellKnownLocations.Settings;
        Directory.CreateDirectory(directory);

        fileName = $"{fileNameWithoutExtension}.json";
        filePath = Path.Combine(directory, fileName);

        if (!TryLoadCore(out TOptions? value))
        {
            value = new TOptions();
        }

        cachedValue = value;
        cachedValue.PropertyChanged += OnRootPropertyChanged;
        UpdateObservableChildren();

        fileProvider = new(directory);
        StartWatchingFileChange();
    }

    public TOptions CurrentValue
    {
        get
        {
            lock (syncRoot)
            {
                return cachedValue;
            }
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, true))
        {
            return;
        }

        lock (syncRoot)
        {
            watchRegistration?.Dispose();
            fileProvider.Dispose();
        }

        cachedValue.PropertyChanged -= OnRootPropertyChanged;
        ClearObservableChildren();
    }

    private static void OnFileChanged(object? state)
    {
        if (state is not JsonSettingsOptionsProvider<TOptions> self)
        {
            return;
        }

        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan("settings.json.file_changed", $"Reload {self.fileName}");
        span.SetTag("settings.options", typeof(TOptions).Name);

        if (self.disposed)
        {
            span.Finish(SpanStatus.Cancelled);
            return;
        }

        try
        {
            TOptions? newValue;
            bool loaded;

            lock (self.syncRoot)
            {
                loaded = self.TryLoadCore(out newValue);
            }

            if (loaded)
            {
                self.BeginApplyExternalChangeOnMainThread(newValue!);
            }
            else
            {
                span.Finish(SpanStatus.FailedPrecondition);
            }
        }
        catch (Exception ex)
        {
            SentryDiagnostics.CaptureException(ex, span, "settings.json.file_changed");
            throw;
        }
        finally
        {
            self.StartWatchingFileChange();
        }
    }

    private void StartWatchingFileChange()
    {
        if (disposed)
        {
            return;
        }

        IDisposable registration = fileProvider.Watch(fileName).RegisterChangeCallback(OnFileChanged, this);

        lock (syncRoot)
        {
            if (disposed)
            {
                registration.Dispose();
                return;
            }

            watchRegistration?.Dispose();
            watchRegistration = registration;
        }
    }

    private void BeginApplyExternalChangeOnMainThread(TOptions value)
    {
        if (ReferenceEquals(SynchronizationContext.Current, App.Current.Threading.SynchronizationContext))
        {
            ApplyExternalChange(value);
            return;
        }

        App.Current.Threading.SynchronizationContext.Post(static state =>
        {
            if (state is (JsonSettingsOptionsProvider<TOptions> provider, TOptions change))
            {
                provider.ApplyExternalChange(change);
            }
        }, Tuple.Create(this, value));
    }

    private void ApplyExternalChange(TOptions value)
    {
        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan("settings.json.apply_external_change", $"Apply {fileName}");
        span.SetTag("settings.options", typeof(TOptions).Name);

        if (disposed)
        {
            span.Finish(SpanStatus.Cancelled);
            return;
        }

        isExternalChange = true;
        try
        {
            cachedValue.CopyFrom(value);
            UpdateObservableChildren();
        }
        catch (Exception ex)
        {
            SentryDiagnostics.CaptureException(ex, span, "settings.json.apply_external_change");
            throw;
        }
        finally
        {
            isExternalChange = false;
        }
    }

    private void OnRootPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (isExternalChange)
        {
            return;
        }

        UpdateObservableChildren();
        PersistObservedChange();
    }

    private void OnObservableChildPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (isExternalChange)
        {
            return;
        }

        UpdateObservableChildren();
        PersistObservedChange();
    }

    private void PersistObservedChange()
    {
        if (disposed || /* defensive check */ isExternalChange)
        {
            return;
        }

        lock (syncRoot)
        {
            if (disposed || /* defensive check */ isExternalChange)
            {
                return;
            }

            SaveCore(cachedValue);
        }
    }

    private void UpdateObservableChildren()
    {
        ClearObservableChildren();

        if (cachedValue is not IOptionsObservableChildrenProvider provider)
        {
            return;
        }

        foreach (INotifyPropertyChanged source in provider.EnumerateObservableChildren())
        {
            source.PropertyChanged += OnObservableChildPropertyChanged;
            observableChildren.Add(source);
        }
    }

    private void ClearObservableChildren()
    {
        foreach (INotifyPropertyChanged source in observableChildren)
        {
            source.PropertyChanged -= OnObservableChildPropertyChanged;
        }

        observableChildren.Clear();
    }

    private bool TryLoadCore([NotNullWhen(true)] out TOptions? value)
    {
        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan("settings.json.load", $"Load {fileName}");
        span.SetTag("settings.options", typeof(TOptions).Name);

        if (!File.Exists(filePath))
        {
            value = new TOptions();
            span.SetTag("settings.file.exists", false);
            return true;
        }

        span.SetTag("settings.file.exists", true);

        for (int retry = 0; retry < 3; retry++)
        {
            try
            {
                using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    value = JsonSerializer.Deserialize<TOptions>(stream, JsonOptions) ?? new TOptions();
                    span.SetData("settings.load.retry", retry);
                    return true;
                }
            }
            catch (IOException ex)
            {
                span.SetData("settings.load.retry", retry + 1);
                SentryDiagnostics.AddBreadcrumb("Retry settings load", "settings.json", "default");
                if (retry == 2)
                {
                    SentryDiagnostics.CaptureException(ex, span, "settings.json.load");
                }

                Thread.Sleep(50);
            }
            catch (JsonException ex)
            {
                SentryDiagnostics.CaptureException(ex, span, "settings.json.load");
                break;
            }
        }

        value = null;
        span.Finish(SpanStatus.FailedPrecondition);
        return false;
    }

    private void SaveCore(TOptions value)
    {
        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan("settings.json.save", $"Save {fileName}");
        span.SetTag("settings.options", typeof(TOptions).Name);

        string tempFile = $"{filePath}.tmp";

        try
        {
            using (FileStream stream = File.Open(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, value, JsonOptions);
            }

            File.Move(tempFile, filePath, true);
        }
        catch (Exception ex)
        {
            SentryDiagnostics.CaptureException(ex, span, "settings.json.save");
            throw;
        }
    }
}
