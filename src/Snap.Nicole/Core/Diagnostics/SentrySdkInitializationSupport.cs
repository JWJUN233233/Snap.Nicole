using Microsoft.Extensions.Logging;
using Sentry;
using Sentry.Extensions.Logging;
using Snap.Nicole.Core.IO;
using Snap.Nicole.Native;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Snap.Nicole.Core.Diagnostics;

internal static class SentrySdkInitializationSupport
{
    private const string ApplicationName = "Snap.Nicole";

    internal static TimeSpan FlushTimeout { get; } = TimeSpan.FromSeconds(2);

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

    private static void ConfigureOptions(SentryOptions options)
    {
        options.Dsn = SentryEnvironmentVariables.GetDsn();
        options.Environment = SentryEnvironmentVariables.GetEnvironmentName();
        options.Release = SentryEnvironmentVariables.GetReleaseName(ApplicationName, typeof(SentrySdkInitializationSupport).Assembly);
        options.CacheDirectoryPath = GetCacheDirectory();
        options.IsGlobalModeEnabled = true;
        options.AutoSessionTracking = true;
        options.SendDefaultPii = false;
        options.AttachStacktrace = true;
        options.TracesSampleRate = SentryEnvironmentVariables.GetTracesSampleRate();
        options.EnableLogs = true;
        options.ShutdownTimeout = FlushTimeout;
        options.FlushTimeout = FlushTimeout;

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
                scope.SetTag(SentryTags.DiagnosticsOperation, SentryOperations.SentryConfigureUser);
            });
        }
    }

    private static string GetCacheDirectory()
    {
        string cacheDirectory = Path.Combine(WellKnownLocations.Cache, "Sentry");
        Directory.CreateDirectory(cacheDirectory);
        return cacheDirectory;
    }
}
