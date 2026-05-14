using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snap.Nicole.SourceGeneration.Extension;
using Snap.Nicole.SourceGeneration.Model;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Snap.Nicole.SourceGeneration.Generator.WellKnownSyntax;
using static Snap.Nicole.SourceGeneration.Primitive.FastSyntaxFactory;

namespace Snap.Nicole.SourceGeneration.Generator.Core;

[Generator(LanguageNames.CSharp)]
internal sealed class CopyFromGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<CopyFromGeneratorContext> provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                WellKnownMetadataNames.GeneratedCopyFromAttribute,
                static (node, token) => node is TypeDeclarationSyntax,
                CopyFromGeneratorContext.Create)
            .Where(static context => context is not null)
            .Select(static (context, token) => context!);

        context.RegisterSourceOutput(provider, GenerateWrapper);
    }

    private static void GenerateWrapper(SourceProductionContext production, CopyFromGeneratorContext context)
    {
        try
        {
            Generate(production, context);
        }
        catch (Exception e)
        {
            production.AddSource($"Error-{Guid.NewGuid()}.g.cs", e.ToString());
        }
    }

    private static void Generate(SourceProductionContext production, CopyFromGeneratorContext context)
    {
        CompilationUnitSyntax syntax = context.Hierarchy
            .GetCompilationUnit([GenerateCopyFromMethod(context)])
            .NormalizeWhitespace();

        production.AddSource(context.Hierarchy.FileNameHint, syntax.ToFullStringWithHeader());
    }

    private static MethodDeclarationSyntax GenerateCopyFromMethod(CopyFromGeneratorContext context)
    {
        SyntaxToken sourceParameter = Identifier("source");
        IdentifierNameSyntax sourceParameterName = IdentifierName(sourceParameter);

        ImmutableArray<StatementSyntax>.Builder statements = ImmutableArray.CreateBuilder<StatementSyntax>();
        statements.Add(ExpressionStatement(ArgumentNullExceptionThrowIfNull(sourceParameterName)));

        foreach (CopyFromPropertyInfo property in context.Properties)
        {
            statements.Add(ExpressionStatement(SimpleAssignmentExpression(
                SimpleMemberAccessExpression(ThisExpression(), IdentifierName(property.TargetName)),
                SimpleMemberAccessExpression(sourceParameterName, IdentifierName(property.SourceName)))));
        }

        return MethodDeclaration(VoidType, Identifier("CopyFrom"))
            .WithModifiers(PublicTokenList)
            .WithParameterList(ParameterList(SingletonSeparatedList(
                Parameter(ParseTypeName(context.SourceTypeFullyQualifiedNameWithNullabilityAnnotations), sourceParameter))))
            .WithBody(Block(List(statements)));
    }

    private sealed record CopyFromGeneratorContext
    {
        public required HierarchyInfo Hierarchy { get; init; }

        public required string SourceTypeFullyQualifiedNameWithNullabilityAnnotations { get; init; }

        public required EquatableArray<CopyFromPropertyInfo> Properties { get; init; }

        public static CopyFromGeneratorContext? Create(GeneratorAttributeSyntaxContext context, CancellationToken token)
        {
            if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
            {
                return null;
            }

            if (typeSymbol.TypeKind is not TypeKind.Class and not TypeKind.Struct)
            {
                return null;
            }

            INamedTypeSymbol? implementedInterface = GetImplementedCopyFromInterface(typeSymbol, token);
            if (implementedInterface is null)
            {
                return null;
            }

            ITypeSymbol sourceType = implementedInterface.TypeArguments[0];

            return new()
            {
                Hierarchy = HierarchyInfo.Create(typeSymbol),
                SourceTypeFullyQualifiedNameWithNullabilityAnnotations = sourceType.GetFullyQualifiedNameWithNullabilityAnnotations(),
                Properties = GetProperties(typeSymbol, sourceType, context.SemanticModel.Compilation, token),
            };
        }

        private static INamedTypeSymbol? GetImplementedCopyFromInterface(INamedTypeSymbol typeSymbol, CancellationToken token)
        {
            foreach (INamedTypeSymbol interfaceType in typeSymbol.AllInterfaces)
            {
                token.ThrowIfCancellationRequested();

                if (interfaceType.OriginalDefinition.HasFullyQualifiedMetadataName(WellKnownMetadataNames.CopyFromInterfaceT))
                {
                    return interfaceType;
                }
            }

            return null;
        }

        private static EquatableArray<CopyFromPropertyInfo> GetProperties(
            INamedTypeSymbol targetType,
            ITypeSymbol sourceType,
            Compilation compilation,
            CancellationToken token)
        {
            bool isSelfCopy = SymbolEqualityComparer.Default.Equals(targetType, sourceType);
            Dictionary<string, IPropertySymbol> sourceProperties = GetSourceProperties(sourceType, targetType.ContainingAssembly, isSelfCopy, token);
            ImmutableArray<CopyFromPropertyInfo>.Builder builder = ImmutableArray.CreateBuilder<CopyFromPropertyInfo>();

            foreach (IPropertySymbol targetProperty in targetType.GetMembers().OfType<IPropertySymbol>())
            {
                token.ThrowIfCancellationRequested();

                if (!CanWriteTargetProperty(targetProperty))
                {
                    continue;
                }

                if (!sourceProperties.TryGetValue(targetProperty.Name, out IPropertySymbol? sourceProperty))
                {
                    continue;
                }

                if (!compilation.ClassifyConversion(sourceProperty.Type, targetProperty.Type).IsImplicit)
                {
                    continue;
                }

                builder.Add(new()
                {
                    TargetName = targetProperty.Name,
                    SourceName = sourceProperty.Name,
                });
            }

            return builder.ToImmutable();
        }

        private static Dictionary<string, IPropertySymbol> GetSourceProperties(
            ITypeSymbol sourceType,
            IAssemblySymbol targetAssembly,
            bool isSelfCopy,
            CancellationToken token)
        {
            Dictionary<string, IPropertySymbol> result = new(StringComparer.Ordinal);

            foreach (IPropertySymbol property in sourceType.GetMembers().OfType<IPropertySymbol>())
            {
                token.ThrowIfCancellationRequested();

                if (!CanReadSourceProperty(property, targetAssembly, isSelfCopy))
                {
                    continue;
                }

                if (!result.ContainsKey(property.Name))
                {
                    result.Add(property.Name, property);
                }
            }

            return result;
        }

        private static bool CanWriteTargetProperty(IPropertySymbol property)
        {
            return !property.IsStatic
                && !property.IsIndexer
                && property.SetMethod is { IsInitOnly: false };
        }

        private static bool CanReadSourceProperty(IPropertySymbol property, IAssemblySymbol targetAssembly, bool isSelfCopy)
        {
            return !property.IsStatic
                && !property.IsIndexer
                && property.GetMethod is not null
                && (isSelfCopy || property.GetMethod.CanBeAccessedFrom(targetAssembly));
        }
    }

    private sealed record CopyFromPropertyInfo
    {
        public required string TargetName { get; init; }

        public required string SourceName { get; init; }
    }
}
