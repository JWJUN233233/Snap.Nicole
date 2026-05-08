using Microsoft.CodeAnalysis;

namespace Snap.Nicole.SourceGeneration.Extension;

internal static class SyntaxNodeExtensions
{
    public static string ToFullStringWithHeader(this SyntaxNode node)
    {
        return $"""
            #pragma warning disable CS1591

            {node.ToFullString()}
            """;
    }
}
