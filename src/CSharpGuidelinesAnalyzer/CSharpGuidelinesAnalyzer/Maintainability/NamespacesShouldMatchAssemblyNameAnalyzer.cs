using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CSharpGuidelinesAnalyzer.Maintainability
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NamespacesShouldMatchAssemblyNameAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "AV1505";

        private const string Title = "Namespaces should match with assembly name";
        private const string NamespaceMessageFormat = "Namespace '{0}' does not match with assembly name '{1}'.";

        private const string TypeInNamespaceMessageFormat =
            "Type '{0}' is declared in namespace '{1}', which does not match with assembly name '{2}'.";

        private const string GlobalTypeMessageFormat =
            "Type '{0}' is declared in global namespace, which does not match with assembly name '{1}'.";

        private const string Description = "Name assemblies after their contained namespace.";
        private const string Category = "Maintainability";

        [NotNull]
        private static readonly DiagnosticDescriptor NamespaceRule = new DiagnosticDescriptor(DiagnosticId, Title,
            NamespaceMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true,
            description: Description, helpLinkUri: HelpLinkUris.GetForCategory(Category, DiagnosticId));

        [NotNull]
        private static readonly DiagnosticDescriptor TypeInNamespaceRule = new DiagnosticDescriptor(DiagnosticId, Title,
            TypeInNamespaceMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true,
            description: Description, helpLinkUri: HelpLinkUris.GetForCategory(Category, DiagnosticId));

        [NotNull]
        private static readonly DiagnosticDescriptor GlobalTypeRule = new DiagnosticDescriptor(DiagnosticId, Title,
            GlobalTypeMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true,
            description: Description, helpLinkUri: HelpLinkUris.GetForCategory(Category, DiagnosticId));

        [ItemNotNull]
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(NamespaceRule, TypeInNamespaceRule, GlobalTypeRule);

        public override void Initialize([NotNull] AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSymbolAction(AnalyzeNamespace, SymbolKind.Namespace);
            context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        }

        private void AnalyzeNamespace(SymbolAnalysisContext context)
        {
            var namespaceSymbol = (INamespaceSymbol) context.Symbol;

            string assemblyName = namespaceSymbol.ContainingAssembly.Name;
            if (IsCoreAssembly(assemblyName))
            {
                return;
            }

            if (!IsTopLevelNamespace(namespaceSymbol))
            {
                return;
            }

            var visitor = new TypesInNamespaceVisitor(assemblyName, context.ReportDiagnostic);
            visitor.Visit(namespaceSymbol);
        }

        private static bool IsCoreAssembly([NotNull] string assemblyName)
        {
            return assemblyName == "Core" || assemblyName.EndsWith(".Core", StringComparison.Ordinal);
        }

        private static bool IsTopLevelNamespace([NotNull] INamespaceSymbol namespaceSymbol)
        {
            return namespaceSymbol.ContainingNamespace.IsGlobalNamespace;
        }

        private void AnalyzeNamedType(SymbolAnalysisContext context)
        {
            var type = (INamedTypeSymbol) context.Symbol;

            string assemblyName = type.ContainingAssembly.Name;
            if (type.ContainingNamespace.IsGlobalNamespace && !IsCoreAssembly(assemblyName))
            {
                context.ReportDiagnostic(Diagnostic.Create(GlobalTypeRule, type.Locations[0], type.Name, assemblyName));
            }
        }

        private sealed class TypesInNamespaceVisitor : SymbolVisitor
        {
            [NotNull]
            private readonly string assemblyName;

            [ItemNotNull]
            private readonly ImmutableArray<string> assemblyNameParts;

            [NotNull]
            private readonly Action<Diagnostic> reportDiagnostic;

            [NotNull]
            [ItemNotNull]
            private readonly Stack<string> namespaceNames = new Stack<string>();

            [NotNull]
            private string CurrentNamespaceName => string.Join(".", namespaceNames.Reverse());

            public TypesInNamespaceVisitor([NotNull] string assemblyName, [NotNull] Action<Diagnostic> reportDiagnostic)
            {
                Guard.NotNull(assemblyName, nameof(assemblyName));

                this.assemblyName = assemblyName;
                assemblyNameParts = assemblyName.Split('.').ToImmutableArray();

                this.reportDiagnostic = reportDiagnostic;
            }

            public override void VisitNamespace([NotNull] INamespaceSymbol symbol)
            {
                namespaceNames.Push(symbol.Name);

                if (!IsCurrentNamespaceValid(false))
                {
                    reportDiagnostic(Diagnostic.Create(NamespaceRule, symbol.Locations[0], CurrentNamespaceName,
                        assemblyName));
                }

                foreach (INamedTypeSymbol typeMember in symbol.GetTypeMembers())
                {
                    VisitNamedType(typeMember);
                }

                foreach (INamespaceSymbol namespaceMember in symbol.GetNamespaceMembers())
                {
                    VisitNamespace(namespaceMember);
                }

                namespaceNames.Pop();
            }

            private bool IsCurrentNamespaceValid(bool requireCompleteMatchWithAssemblyName)
            {
                string[] currentNamespaceParts = namespaceNames.Reverse().ToArray();

                if (requireCompleteMatchWithAssemblyName && assemblyNameParts.Length > currentNamespaceParts.Length)
                {
                    return false;
                }

                int commonLength = Math.Min(currentNamespaceParts.Length, assemblyNameParts.Length);
                for (int index = 0; index < commonLength; index++)
                {
                    if (currentNamespaceParts[index] != assemblyNameParts[index])
                    {
                        return false;
                    }
                }

                return true;
            }

            public override void VisitNamedType([NotNull] INamedTypeSymbol symbol)
            {
                if (!IsCurrentNamespaceValid(true))
                {
                    reportDiagnostic(Diagnostic.Create(TypeInNamespaceRule, symbol.Locations[0], symbol.Name,
                        CurrentNamespaceName, assemblyName));
                }
            }
        }
    }
}