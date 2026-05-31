using System.ComponentModel;
using Snap.Nicole.Core.Diagnostics;

namespace Snap.Nicole.Services.AI;

internal static class BuiltInFunctions
{
    [Description("Get the current local time.")]
    public static string GetCurrentTime()
    {
        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan("ai.tool.current_time", "Get current local time");

        try
        {
            string result = DateTimeOffset.Now.ToString("O");
            return result;
        }
        catch (Exception ex)
        {
            SentryDiagnostics.CaptureException(ex, span, "ai.tool.current_time");
            throw;
        }
    }
}
