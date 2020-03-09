using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace INotifyPropertyChangedCodeFix
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class OnPropertyChangedNotImplementedAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "OnPropChangNotImpl";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.OnPropertyChangedAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.OnPropertyChangedAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.OnPropertyChangedAnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = "Naming";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(SyntaxNodeAction,SyntaxKind.ClassDeclaration);
        }

        private void SyntaxNodeAction(SyntaxNodeAnalysisContext node)
        {
            var classNode = node.Node as ClassDeclarationSyntax;
            if (classNode.BaseList == null || classNode.BaseList.Types == null)
                return;

            if (OnPropertyChangedHelper.ImplementsOnPropertyChanged(classNode))
                return;

            foreach (var tp in classNode.BaseList.Types.Select(x => x.Type).OfType<SimpleNameSyntax>())
            {
                if (tp.Identifier.Text == "INotifyPropertyChanged")
                {
                    var diag = Diagnostic.Create(Rule, classNode.Identifier.GetLocation());
                    node.ReportDiagnostic(diag);
                }

            }
        }
    }
}
