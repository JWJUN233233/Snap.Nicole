using System.IO;

namespace Snap.Nicole.Core.IO;

internal static class WellKnownLocations
{
    public static string AppIcon = Path.Combine(AppContext.BaseDirectory, "Assets", "Logo.ico");

    public static string Settings = Path.Combine(AppContext.BaseDirectory, "Settings");
}
