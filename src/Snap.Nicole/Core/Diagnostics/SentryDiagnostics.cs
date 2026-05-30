using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sentry;
using Sentry.Extensions.Logging;
using Snap.Nicole.Core.IO;
using Snap.Nicole.Native;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Snap.Nicole.Core.Diagnostics;

internal static class SentryDiagnostics
{
    private const string ApplicationName = "Snap.Nicole";
    private const string DefaultDsn = "https://46fe08b9947aee4a457bc20fe15fdb6a@sentry.gentle.house/14";
    private const string DsnEnvironmentVariable = "SENTRY_DSN";
    private const string EnvironmentEnvironmentVariable = "SENTRY_ENVIRONMENT";
    private const string ReleaseEnvironmentVariable = "SENTRY_RELEASE";
    private const int FlushTimeoutSeconds = 2;

    public static IDisposable Initialize()
    {
        IDisposable sentry = SentrySdk.Init(ConfigureOptions);

        if (SentrySdk.IsEnabled)
        {
            ConfigureUser();
        }

        return sentry;
    }

    public static void ConfigureLogging(SentryLoggingOptions options)
    {
        ConfigureOptions(options);
        options.InitializeSdk = false;
        options.MinimumBreadcrumbLevel = LogLevel.Information;
        options.MinimumEventLevel = LogLevel.Error;
    }

    public static void CaptureUnhandledException(Exception exception, bool isTerminating)
    {
        SentrySdk.CaptureException(exception);

        if (isTerminating)
        {
            SentrySdk.Flush(TimeSpan.FromSeconds(FlushTimeoutSeconds));
        }
    }

    private static void ConfigureOptions(SentryOptions options)
    {
        options.Dsn = GetDsn();
        options.Environment = GetEnvironmentName();
        options.Release = GetReleaseName();
        options.CacheDirectoryPath = GetCacheDirectory();
        options.IsGlobalModeEnabled = true;
        options.AutoSessionTracking = true;
        options.SendDefaultPii = false;
        options.AttachStacktrace = true;
        options.EnableLogs = true;
        options.ShutdownTimeout = TimeSpan.FromSeconds(FlushTimeoutSeconds);
        options.FlushTimeout = TimeSpan.FromSeconds(FlushTimeoutSeconds);

        options.AddInAppInclude(ApplicationName);
        options.AddExceptionFilterForType<OperationCanceledException>();
        options.SetBeforeSend(static @event =>
        {
            @event.ServerName = null;
            return @event;
        });
        options.SetBeforeSendLog(static log =>
        {
            return log.Level < SentryLogLevel.Warning ? null : log;
        });
        options.DisableWinUiUnhandledExceptionIntegration();
    }

    private static string GetDsn()
    {
        string? dsn = Environment.GetEnvironmentVariable(DsnEnvironmentVariable);
        if (dsn is null)
        {
            return DefaultDsn;
        }

        return dsn.Trim();
    }

    private static string GetEnvironmentName()
    {
        if (GetEnvironmentValue(EnvironmentEnvironmentVariable) is { } environment)
        {
            return environment;
        }

#if DEBUG
        return Environments.Development;
#else
        return Environments.Production;
#endif
    }

    private static string? GetEnvironmentValue(string name)
    {
        string? value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static void ConfigureUser()
    {
        try
        {
            NicoleNativeFirmwareUuidReader firmwareUuidReader = NicoleNative.Default.MakeFirmwareUuidReader();
            if (!firmwareUuidReader.TryGetFirmwareUuid(out Guid firmwareUuid))
            {
                return;
            }

            string userId = Convert.ToHexString(CryptographicOperations.HashData(HashAlgorithmName.SHA256, MemoryMarshal.AsBytes(new ReadOnlySpan<Guid>(ref firmwareUuid)))).ToUpperInvariant();

            SentrySdk.ConfigureScope(static (scope, userId) =>
            {
                scope.User.Id = userId;
            }, userId);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex, static scope =>
            {
                scope.SetTag("diagnostics.operation", "sentry.configure_user");
            });
        }
    }

    private static string GetReleaseName()
    {
        if (GetEnvironmentValue(ReleaseEnvironmentVariable) is { } release)
        {
            return release;
        }

        Assembly thisAssembly = typeof(SentryDiagnostics).Assembly;
        string? informationalVersion = thisAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            return $"{ApplicationName}@{informationalVersion}";
        }

        if (thisAssembly.GetName().Version is { } version)
        {
            return $"{ApplicationName}@{version}";
        }

        return ApplicationName;
    }

    private static string GetCacheDirectory()
    {
        string cacheDirectory = Path.Combine(WellKnownLocations.Cache, "Sentry");
        Directory.CreateDirectory(cacheDirectory);
        return cacheDirectory;
    }
}
