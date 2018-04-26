using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using System.Linq;
using System.Collections.Immutable;

namespace MyCodeAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UsingsAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "MyCodeAnalyzers";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSyntaxTreeAction(AnalyzeTree);
        }

        private void AnalyzeTree(SyntaxTreeAnalysisContext obj)
        {
            var descendantNodes = obj.Tree.GetRoot().DescendantNodes();
            var usings = descendantNodes.OfType<UsingDirectiveSyntax>().ToList();
            if (usings.Count == 0)
                return;
            var sortedUsings = usings.OrderBy(o => new string(o.Name.ToString().TakeWhile(c => c != '.').ToArray()))
                                        .ThenBy(o => o.Name.ToString().Length)
                                        .ToList();
            for (var index = 0; index < usings.Count; index++)
            {
                var usingDirectiveSyntax = usings[index];
                if (usingDirectiveSyntax.ToString() != sortedUsings[index].ToString())
                {
                    var diagnostic = Diagnostic.Create(Rule, sortedUsings[index].GetLocation(), sortedUsings);
                    obj.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
