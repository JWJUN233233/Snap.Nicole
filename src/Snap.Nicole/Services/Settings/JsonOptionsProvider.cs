using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace Snap.Nicole.Services.Settings;

internal sealed class JsonOptionsProvider<TOptions> : IOptionsMonitor<TOptions>, IOptionsWriter<TOptions>, IDisposable
    where TOptions : class, new()
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly Lock syncRoot = new();

    private readonly string filePath;
    private readonly string fileName;

    private readonly PhysicalFileProvider fileProvider;
    private IDisposable? watchRegistration;

    private ConfigurationReloadToken changeToken = new();
    private TOptions cachedValue = new();

    private volatile bool disposed;

    public JsonOptionsProvider(string settingsKey)
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "Settings");
        Directory.CreateDirectory(directory);

        fileName = $"{settingsKey}.json";
        filePath = Path.Combine(directory, fileName);

        LoadCore(out cachedValue);

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
            throw new NotSupportedException();
        }

        return CurrentValue;
    }

    public IDisposable OnChange(Action<TOptions, string?> listener)
    {
        return ChangeToken.OnChange(() => changeToken, state => state(CurrentValue, string.Empty), listener);
    }

    public void Update(TOptions value)
    {
        ArgumentNullException.ThrowIfNull(value);

        lock (syncRoot)
        {
            cachedValue = value;
            SaveCore(value);
            RaiseChangeTokenCore();
        }
    }

    public void Reload()
    {
        lock (syncRoot)
        {
            if (TryLoadCore(out TOptions? newValue))
            {
                cachedValue = newValue;
                RaiseChangeTokenCore();
            }
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        watchRegistration?.Dispose();
        fileProvider.Dispose();
    }

    private static void OnFileChanged(object? state)
    {
        if (state is not JsonOptionsProvider<TOptions> self)
        {
            return;
        }

        if (self.disposed)
        {
            return;
        }

        self.Reload();
        self.StartWatching();
    }

    private void StartWatching()
    {
        watchRegistration = fileProvider.Watch(fileName).RegisterChangeCallback(OnFileChanged, this);
    }

    private void RaiseChangeTokenCore()
    {
        ConfigurationReloadToken oldToken = changeToken;
        changeToken = new();
        oldToken.OnReload();
    }

    private void LoadCore([NotNull] out TOptions? value)
    {
        if (!TryLoadCore(out value))
        {
            value = new TOptions();
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

    private void SaveCore(TOptions value)
    {
        string tempFile = $"{filePath}.tmp";

        using (FileStream stream = File.Open(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(stream, value, JsonOptions);
        }

        File.Move(tempFile, filePath, true);
    }
}
