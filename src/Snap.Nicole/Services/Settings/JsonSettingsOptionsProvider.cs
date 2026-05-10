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

internal sealed class JsonSettingsOptionsProvider<TOptions> : IOptionsProvider<TOptions>, IDisposable
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

    public void Update()
    {
        lock (syncRoot)
        {
            SaveCore(cachedValue);
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
        if (state is not JsonSettingsOptionsProvider<TOptions> self)
        {
            return;
        }

        if (self.disposed)
        {
            return;
        }

        lock (self.syncRoot)
        {
            if (self.TryLoadCore(out TOptions? newValue))
            {
                self.cachedValue = newValue;

                ConfigurationReloadToken oldToken = self.changeToken;
                self.changeToken = new();
                oldToken.OnReload();
            }
        }

        self.StartWatching();
    }

    private void StartWatching()
    {
        watchRegistration = fileProvider.Watch(fileName).RegisterChangeCallback(OnFileChanged, this);
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