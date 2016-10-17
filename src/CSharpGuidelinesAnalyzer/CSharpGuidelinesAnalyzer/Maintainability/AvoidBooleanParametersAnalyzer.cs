using System.Collections.Immutable;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CSharpGuidelinesAnalyzer.Maintainability
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AvoidBooleanParametersAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "AV1564";

        private const string Title = "Parameter is of type bool or bool?";
        private const string MessageFormat = "Parameter '{0}' is of type '{1}'.";
        private const string Description = "Avoid methods that take a bool flag.";
        private const string Category = "Maintainability";

        [NotNull]
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat,
            Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description,
            helpLinkUri: HelpLinkUris.GetForCategory(Category));

        [ItemNotNull]
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize([NotNull] AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(c => AnalyzeParameter(AnalysisUtilities.SyntaxToSymbolContext(c)),
                SyntaxKind.Parameter);
        }

        private void AnalyzeParameter(SymbolAnalysisContext context)
        {
            var parameter = (IParameterSymbol) context.Symbol;

            if (parameter.Type.SpecialType == SpecialType.System_Boolean ||
                AnalysisUtilities.IsNullableBoolean(parameter.Type))
            {
                AnalyzeBooleanParameter(parameter, context);
            }
        }

        private void AnalyzeBooleanParameter([NotNull] IParameterSymbol parameter, SymbolAnalysisContext context)
        {
            ISymbol containingMember = parameter.ContainingSymbol;
            if (containingMember.IsOverride)
            {
                return;
            }

            if (AnalysisUtilities.HidesBaseMember(containingMember, context.CancellationToken))
            {
                return;
            }

            if (IsInterfaceImplementation(parameter))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, parameter.Locations[0], parameter.Name, parameter.Type));
        }

        private bool IsInterfaceImplementation([NotNull] IParameterSymbol parameter)
        {
            ISymbol containingMember = parameter.ContainingSymbol;

            foreach (INamedTypeSymbol iface in parameter.ContainingType.AllInterfaces)
            {
                foreach (ISymbol ifaceMember in iface.GetMembers())
                {
                    ISymbol implementer = parameter.ContainingType.FindImplementationForInterfaceMember(ifaceMember);

                    if (containingMember.Equals(implementer))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}