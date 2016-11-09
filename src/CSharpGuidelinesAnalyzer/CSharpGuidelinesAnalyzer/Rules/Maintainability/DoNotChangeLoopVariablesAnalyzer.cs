using System.Collections.Immutable;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CSharpGuidelinesAnalyzer.Rules.Maintainability
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DoNotChangeLoopVariablesAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "AV1530";

        private const string Title = "Loop variable should not be written to in loop body";
        private const string MessageFormat = "Loop variable '{0}' should not be written to in loop body.";
        private const string Description = "Don't change a loop variable inside a for loop.";
        private const string Category = "Maintainability";

        [NotNull]
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat,
            Category, DiagnosticSeverity.Warning, true, Description, HelpLinkUris.GetForCategory(Category, DiagnosticId));

        [ItemNotNull]
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize([NotNull] AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(AnalyzeForStatement, SyntaxKind.ForStatement);
        }

        private void AnalyzeForStatement(SyntaxNodeAnalysisContext context)
        {
            var forStatementSyntax = (ForStatementSyntax) context.Node;
            foreach (VariableDeclaratorSyntax variableDeclaratorSyntax in forStatementSyntax.Declaration.Variables)
            {
                ISymbol variableSymbol = context.SemanticModel.GetDeclaredSymbol(variableDeclaratorSyntax);

                DataFlowAnalysis dataFlowAnalysis = context.SemanticModel.AnalyzeDataFlow(forStatementSyntax.Statement);
                if (dataFlowAnalysis.Succeeded)
                {
                    if (dataFlowAnalysis.WrittenInside.Contains(variableSymbol))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Rule,
                            variableDeclaratorSyntax.Identifier.GetLocation(), variableSymbol.Name));
                    }
                }
            }
        }
    }
}