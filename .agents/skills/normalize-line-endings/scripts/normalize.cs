using System.Text;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: dotnet run normalize.cs -- <path>");
    return 1;
}

var root = Path.GetFullPath(args[0]);
if (!Directory.Exists(root))
{
    Console.Error.WriteLine($"Directory not found: {root}");
    return 1;
}

var changed = 0;
var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    ".cs",
    ".csproj",
    ".config",
    ".cpp",
    ".editorconfig",
    ".filters",
    ".h",
    ".json",
    ".md",
    ".props",
    ".pubxml",
    ".rc",
    ".resx",
    ".sln",
    ".slnx",
    ".targets",
    ".toml",
    ".vcxproj",
    ".xml",
    ".xaml",
    ".yaml",
    ".yml",
};
var fileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    ".gitignore",
};
var excludedDirectoryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    ".git",
    ".vs",
    "bin",
    "obj",
    "Generated Files",
};

foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
{
    var relativePath = Path.GetRelativePath(root, file);
    var directoryParts = relativePath
        .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
        .SkipLast(1);

    if (directoryParts.Any(excludedDirectoryNames.Contains))
    {
        continue;
    }

    if (!extensions.Contains(Path.GetExtension(file)) &&
        !fileNames.Contains(Path.GetFileName(file)))
    {
        continue;
    }

    var text = File.ReadAllText(file, utf8NoBom);
    var normalized = text.ReplaceLineEndings();

    if (normalized == text)
    {
        continue;
    }

    File.WriteAllText(file, normalized, utf8NoBom);
    changed++;
    Console.WriteLine(file);
}

Console.WriteLine($"Normalized {changed} file(s).");
return 0;
