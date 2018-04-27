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

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxTreeAction(AnalyzeTree);
        }

        private void AnalyzeTree(SyntaxTreeAnalysisContext obj)
        {
            var fileName = obj.Tree.FilePath.Split('\\').Last();
            var syntaxNode = obj.Tree.GetRoot();
            var descendantNodes = syntaxNode.DescendantNodes();
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
                    var diagnostic = Diagnostic.Create(Rule, sortedUsings[index].GetLocation(), fileName);
                    obj.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
