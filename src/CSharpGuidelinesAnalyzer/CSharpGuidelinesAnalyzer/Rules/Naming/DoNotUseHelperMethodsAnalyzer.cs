using System.Collections.Immutable;
using CSharpGuidelinesAnalyzer.Extensions;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CSharpGuidelinesAnalyzer.Rules.Naming
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
#pragma warning disable AV1708 // Type name contains term that should be avoided
    public sealed class DoNotUseHelperMethodsAnalyzer : DiagnosticAnalyzer
#pragma warning restore AV1708 // Type name contains term that should be avoided
    {
        public const string DiagnosticId = "AV1708";

        private const string Title = "Type name contains term that should be avoided";
        private const string MessageFormat = "Name of type '{0}' contains the term '{1}'.";
        private const string Description = "Name types using nouns, noun phrases or adjective phrases.";
        private const string Category = "Naming";

        [NotNull]
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat,
            Category, DiagnosticSeverity.Warning, true, Description, HelpLinkUris.GetForCategory(Category, DiagnosticId));

        [ItemNotNull]
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        [ItemNotNull]
        private static readonly ImmutableArray<string> WordsBlacklist =
            new[] { "Utility", "Utilities", "Facility", "Facilities", "Helper", "Helpers", "Common", "Shared" }
                .ToImmutableArray();

        public override void Initialize([NotNull] AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        }

        private void AnalyzeNamedType(SymbolAnalysisContext context)
        {
            var type = (INamedTypeSymbol) context.Symbol;

            string word = type.Name.GetFirstWordInSetFromIdentifier(WordsBlacklist, TextMatchMode.RequireExactMatch);
            if (word != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, type.Locations[0], type.Name, word));
            }
        }
    }
}