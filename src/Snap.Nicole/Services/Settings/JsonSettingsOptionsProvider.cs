using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Snap.Nicole.Core;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace Snap.Nicole.Services.Settings;

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
    private readonly List<INotifyPropertyChanged> observedChangeSources = [];
    private IDisposable? watchRegistration;

    private ConfigurationReloadToken changeToken = new();
    private TOptions cachedValue = new();

    private bool suppressObservedChanges;
    private volatile bool disposed;

    public JsonSettingsOptionsProvider(string fileNameWithoutExtension)
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "Settings");
        Directory.CreateDirectory(directory);

        fileName = $"{fileNameWithoutExtension}.json";
        filePath = Path.Combine(directory, fileName);

        if (!TryLoadCore(out TOptions? value))
        {
            value = new TOptions();
        }

        cachedValue = value;
        cachedValue.PropertyChanged += OnRootPropertyChanged;
        RefreshObservedChangeSources();

        fileProvider = new(directory);
        StartWatching();
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

    public TOptions Get(string? name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            throw new NotSupportedException("Named options are not supported.");
        }

        return CurrentValue;
    }

    public IDisposable OnChange(Action<TOptions, string?> listener)
    {
        return ChangeToken.OnChange(() => changeToken, state => state(CurrentValue, string.Empty), listener);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        lock (syncRoot)
        {
            watchRegistration?.Dispose();
            fileProvider.Dispose();
        }

        cachedValue.PropertyChanged -= OnRootPropertyChanged;
        ClearObservedChangeSources();
    }

    private static void OnFileChanged(object? state)
    {
        if (state is not JsonSettingsOptionsProvider<TOptions> self)
        {
            return;
        }

        if (self.disposed)
        {
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
                self.ApplyExternalChangeOnApplicationThread(newValue);
            }
        }
        finally
        {
            self.StartWatching();
        }
    }

    private void StartWatching()
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

    private bool TryLoadCore([NotNullWhen(true)] out TOptions? value)
    {
        if (!File.Exists(filePath))
        {
            value = new TOptions();
            return true;
        }

        for (int retry = 0; retry < 3; retry++)
        {
            try
            {
                using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    value = JsonSerializer.Deserialize<TOptions>(stream, JsonOptions) ?? new TOptions();
                    return true;
                }
            }
            catch (IOException)
            {
                Thread.Sleep(50);
            }
            catch (JsonException)
            {
                break;
            }
        }

        value = null;
        return false;
    }

    private void ApplyExternalChangeOnApplicationThread(TOptions value)
    {
        try
        {
            App.Current.Threading.SynchronizationContext.Post(static state =>
            {
                if (state is ExternalChangeState change)
                {
                    change.Provider.ApplyExternalChange(change.Value);
                }
            }, new ExternalChangeState(this, value));
        }
        catch (InvalidOperationException)
        {
            ApplyExternalChange(value);
        }
    }

    private void ApplyExternalChange(TOptions value)
    {
        if (disposed)
        {
            return;
        }

        suppressObservedChanges = true;
        try
        {
            cachedValue.CopyFrom(value);
            RefreshObservedChangeSources();
        }
        finally
        {
            suppressObservedChanges = false;
        }

        SignalChange();
    }

    private void OnRootPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (suppressObservedChanges)
        {
            return;
        }

        RefreshObservedChangeSources();

        PersistObservedChange();
    }

    private void OnObservedChangeSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (suppressObservedChanges)
        {
            return;
        }

        PersistObservedChange();
    }

    private void PersistObservedChange()
    {
        if (disposed || suppressObservedChanges)
        {
            return;
        }

        lock (syncRoot)
        {
            if (disposed || suppressObservedChanges)
            {
                return;
            }

            SaveCore(cachedValue);
        }

        SignalChange();
    }

    private void SignalChange()
    {
        ConfigurationReloadToken oldToken;

        lock (syncRoot)
        {
            if (disposed)
            {
                return;
            }

            oldToken = changeToken;
            changeToken = new();
        }

        oldToken.OnReload();
    }

    private void RefreshObservedChangeSources()
    {
        ClearObservedChangeSources();

        if (cachedValue is not IOptionsChangeSourceProvider sourceProvider)
        {
            return;
        }

        foreach (INotifyPropertyChanged source in sourceProvider.GetChangeSources())
        {
            source.PropertyChanged += OnObservedChangeSourcePropertyChanged;
            observedChangeSources.Add(source);
        }
    }

    private void ClearObservedChangeSources()
    {
        foreach (INotifyPropertyChanged source in observedChangeSources)
        {
            source.PropertyChanged -= OnObservedChangeSourcePropertyChanged;
        }

        observedChangeSources.Clear();
    }

    private void SaveCore(TOptions value)
    {
        string tempFile = $"{filePath}.tmp";

        using (FileStream stream = File.Open(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(stream, value, JsonOptions);
        }

        File.Move(tempFile, filePath, true);
    }

    private sealed record ExternalChangeState(JsonSettingsOptionsProvider<TOptions> Provider, TOptions Value);
}
