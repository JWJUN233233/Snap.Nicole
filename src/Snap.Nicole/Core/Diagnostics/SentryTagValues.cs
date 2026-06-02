namespace Snap.Nicole.Core.Diagnostics;

internal static class SentryTagValues
{
    public const string False = "false";
    public const string True = "true";

    public static string FromBoolean(bool value)
    {
        return value ? True : False;
    }
}
