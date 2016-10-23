using CSharpGuidelinesAnalyzer.Maintainability;
using CSharpGuidelinesAnalyzer.Test.TestDataBuilders;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace CSharpGuidelinesAnalyzer.Test.Specs.Maintainability
{
    public class NamespacesShouldMatchAssemblyNameSpecs : CSharpGuidelinesAnalysisTestFixture
    {
        protected override string DiagnosticId => NamespacesShouldMatchAssemblyNameAnalyzer.DiagnosticId;

        [Fact]
        public void When_assembly_name_matches_with_namespace_name_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InAssemblyNamed("Some.Scope.Example")
                .InGlobalScope(@"
                    namespace Some
                    {
                        namespace Scope
                        {
                            namespace Example
                            {
                                namespace Deeper
                                {
                                }
                            }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source);
        }

        [Fact]
        public void When_assembly_name_does_not_match_with_namespace_name_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InAssemblyNamed("Some.Scope.Example")
                .InGlobalScope(@"
                    namespace Some
                    {
                        namespace [|WrongScope|]
                        {
                            namespace [|Example|]
                            {
                                namespace [|Deeper|]
                                {
                                }
                            }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Namespace 'Some.WrongScope' does not match with assembly name 'Some.Scope.Example'.",
                "Namespace 'Some.WrongScope.Example' does not match with assembly name 'Some.Scope.Example'.",
                "Namespace 'Some.WrongScope.Example.Deeper' does not match with assembly name 'Some.Scope.Example'.");
        }

        [Fact]
        public void When_assembly_name_starts_with_namespace_name_but_does_not_match_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InAssemblyNamed("Some.Scope.Example")
                .InGlobalScope(@"
                    namespace Some
                    {
                        namespace [|Scope2|]
                        {
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Namespace 'Some.Scope2' does not match with assembly name 'Some.Scope.Example'.");
        }

        [Fact]
        public void When_namespace_name_starts_with_assembly_name_but_does_not_match_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InAssemblyNamed("Some.Scope2.Example")
                .InGlobalScope(@"
                    namespace Some
                    {
                        namespace [|Scope|]
                        {
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Namespace 'Some.Scope' does not match with assembly name 'Some.Scope2.Example'.");
        }

        [Fact]
        public void When_assembly_name_matches_with_namespace_of_type_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InAssemblyNamed("Some.Scope.Example")
                .InGlobalScope(@"
                    namespace Some
                    {
                        namespace Scope
                        {
                            namespace Example
                            {
                                public class Good
                                {
                                }

                                namespace Deeper
                                {
                                    public class AlsoGood
                                    {
                                    }
                                }
                            }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source);
        }

        [Fact]
        public void When_assembly_name_does_not_match_with_namespace_of_type_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InAssemblyNamed("Some.Scope.Example")
                .InGlobalScope(@"
                    namespace Some
                    {
                        namespace Scope
                        {
                            public class [|TooHigh|]
                            {
                            }

                            namespace Example
                            {
                                namespace Deeper
                                {
                                }
                            }

                            public class [|AlsoTooHigh|]
                            {
                            }

                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Type 'TooHigh' is declared in namespace 'Some.Scope', which does not match with assembly name 'Some.Scope.Example'.",
                "Type 'AlsoTooHigh' is declared in namespace 'Some.Scope', which does not match with assembly name 'Some.Scope.Example'.");
        }

        [Fact]
        public void When_assembly_name_starts_with_namespace_of_type_but_does_not_match_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InAssemblyNamed("Some.Scope2")
                .InGlobalScope(@"
                    namespace Some
                    {
                        namespace [|Scope|]
                        {
                            public class [|WrongSpace|]
                            {
                            }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Namespace 'Some.Scope' does not match with assembly name 'Some.Scope2'.",
                "Type 'WrongSpace' is declared in namespace 'Some.Scope', which does not match with assembly name 'Some.Scope2'.");
        }

        [Fact]
        public void When_namespace_of_type_starts_with_assembly_name_but_does_not_match_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InAssemblyNamed("Some.Scope")
                .InGlobalScope(@"
                    namespace Some
                    {
                        namespace [|Scope2|]
                        {
                            public class [|WrongSpace|]
                            {
                            }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Namespace 'Some.Scope2' does not match with assembly name 'Some.Scope'.",
                "Type 'WrongSpace' is declared in namespace 'Some.Scope2', which does not match with assembly name 'Some.Scope'.");
        }

        [Fact]
        public void When_type_is_defined_in_global_namespace_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InAssemblyNamed("Some.Scope.Example")
                .InGlobalScope(@"
                    public class [|C|]
                    {
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Type 'C' is declared in global namespace, which does not match with assembly name 'Some.Scope.Example'.");
        }

        [Fact]
        public void When_assembly_name_ends_with_Core_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InAssemblyNamed("Company.Core")
                .InGlobalScope(@"
                    class Top
                    {
                    }

                    namespace Different
                    {
                        namespace Nested
                        {
                            class Deep
                            {
                            }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source);
        }

        [Fact]
        public void When_assembly_name_equals_Core_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InAssemblyNamed("Core")
                .InGlobalScope(@"
                    class Top
                    {
                    }

                    namespace Different
                    {
                        namespace Nested
                        {
                            class Deep
                            {
                            }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source);
        }

        protected override DiagnosticAnalyzer CreateAnalyzer()
        {
            return new NamespacesShouldMatchAssemblyNameAnalyzer();
        }
    }
}