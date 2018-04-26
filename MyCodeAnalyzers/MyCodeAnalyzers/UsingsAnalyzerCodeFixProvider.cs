using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using System;
using System.Linq;
using System.Threading;
using System.Composition;
using System.Threading.Tasks;
using System.Collections.Immutable;

namespace MyCodeAnalyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UsingsAnalyzerCodeFixProvider)), Shared]
    public class UsingsAnalyzerCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(UsingsAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics.Where(d => FixableDiagnosticIds.Contains(d.Id)))
            {
                context.RegisterCodeFix(CodeAction.Create("Arrange using statements",
                    token => GetTransformedDocumentAsync(context.Document, diagnostic, token)), diagnostic);
            }

            await Task.FromResult(Task.CompletedTask);
        }

        private static async Task<Document> GetTransformedDocumentAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            try
            {
                SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var usings = root.DescendantNodes().OfType<UsingDirectiveSyntax>().ToList();
                var orderedUsings = usings.OrderBy(o => new string(o.Name.ToString().TakeWhile(c => c != '.').ToArray()))
                    .ThenBy(o => o.Name.ToString().Length)
                    .ToList();

                var documentEditor = await DocumentEditor.CreateAsync(document, cancellationToken);
                for (var index = 0; index < usings.Count; index++)
                {
                    var usingStatement = usings[index];
                    var usingDirectiveSyntax = orderedUsings[index];
                    if (index > 0)
                    {
                        var currentNamespace = GetAssemblyNameFromNamespace(orderedUsings[index]);
                        var lastNamespace = GetAssemblyNameFromNamespace(orderedUsings[index - 1]);
                        if (currentNamespace != lastNamespace)
                        {
                            usingDirectiveSyntax =
                                usingDirectiveSyntax.WithLeadingTrivia(
                                    SyntaxTriviaList.Create(SyntaxFactory.CarriageReturnLineFeed));
                        }
                    }
                    documentEditor.ReplaceNode(usingStatement, usingDirectiveSyntax);
                }
                return documentEditor.GetChangedDocument();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return document;
            }
        }

        private static string GetAssemblyNameFromNamespace(UsingDirectiveSyntax usingDirective)
        {
            return new string(usingDirective.ToString().Split(' ')[1].TakeWhile(c => c != '.').ToArray()).Trim(';');
        }

    }
}
