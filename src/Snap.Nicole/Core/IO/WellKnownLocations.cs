using System.IO;

namespace Snap.Nicole.Core.IO;

internal static class WellKnownLocations
{
    public static string AppIcon { get; } = Path.Combine(AppContext.BaseDirectory, "Assets", "Logo.ico");

    public static string Settings { get; } = Path.Combine(AppContext.BaseDirectory, "Settings");

    public static string Cache { get; } = Path.Combine(AppContext.BaseDirectory, "Cache");
}
