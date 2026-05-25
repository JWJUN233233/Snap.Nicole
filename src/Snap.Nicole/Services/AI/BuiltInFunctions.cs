using System.ComponentModel;

namespace Snap.Nicole.Services.AI;

internal static class BuiltInFunctions
{
    [Description("Get the current local time.")]
    public static string GetCurrentTime()
    {
        return DateTimeOffset.Now.ToString("O");
    }
}