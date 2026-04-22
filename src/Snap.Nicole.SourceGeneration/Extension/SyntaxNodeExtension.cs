using Microsoft.CodeAnalysis;

namespace Snap.Nicole.SourceGeneration.Extension;

internal static class SyntaxNodeExtension
{
    public static string ToFullStringWithHeader(this SyntaxNode node)
    {
        return $"""
            // Copyright (c) DGP Studio. All rights reserved.
            // Licensed under the MIT license.
            
            #pragma warning disable CS1591

            {node.ToFullString()}
            """;
    }
}
