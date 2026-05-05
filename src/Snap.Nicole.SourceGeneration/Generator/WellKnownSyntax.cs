using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Snap.Nicole.SourceGeneration.Primitive.FastSyntaxFactory;

namespace Snap.Nicole.SourceGeneration.Generator;

internal static class WellKnownSyntax
{
    public static readonly NameSyntax NameOfMicrosoftUIXaml = ParseName("global::Microsoft.UI.Xaml");
    public static readonly NameSyntax NameOfSystemComponentModelEditorBrowsable = ParseName("global::System.ComponentModel.EditorBrowsable");
    public static readonly NameSyntax NameOfSystemComponentModelEditorBrowsableState = ParseName("global::System.ComponentModel.EditorBrowsableState");
    public static readonly NameSyntax NameOfSystemDiagnosticsCodeAnalysisMaybeNull = ParseName("global::System.Diagnostics.CodeAnalysis.MaybeNull");
    public static readonly NameSyntax NameOfSystemDiagnosticsCodeAnalysisNotNullIfNotNull = ParseName("global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull");

    public static readonly TypeSyntax TypeOfSystemArgumentNullException = ParseTypeName("global::System.ArgumentNullException");
    public static readonly TypeSyntax TypeOfSystemGlobalizationCultureInfo = ParseTypeName("global::System.Globalization.CultureInfo");
    public static readonly TypeSyntax TypeOfSystemIOStream = ParseTypeName("global::System.IO.Stream");
    public static readonly TypeSyntax TypeOfSystemResourcesResourceManager = ParseTypeName("global::System.Resources.ResourceManager");

    // ArgumentNullException.ThrowIfNull(%argumentExpression%)
    public static InvocationExpressionSyntax ArgumentNullExceptionThrowIfNull(ExpressionSyntax argumentExpression)
    {
        return InvocationExpression(
                SimpleMemberAccessExpression(
                    TypeOfSystemArgumentNullException,
                    IdentifierName("ThrowIfNull")))
            .WithArgumentList(ArgumentList(SingletonSeparatedList(
                Argument(argumentExpression))));
    }
}