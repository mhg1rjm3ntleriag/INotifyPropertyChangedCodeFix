using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace INotifyPropertyChangedCodeFix
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CallOnPropertyChangedNotImplementedAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CallOnPropNotImpl";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.CallOnPropertyChangedAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.CallOnPropertyChangedAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.CallOnPropertyChangedAnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(SyntaxNodeAction, SyntaxKind.PropertyDeclaration);
        }

        private void SyntaxNodeAction(SyntaxNodeAnalysisContext tree)
        {
            var prop = tree.Node as PropertyDeclarationSyntax;
            if (!prop.FirstAncestorOrSelf<ClassDeclarationSyntax>().ImplementsType("INotifyPropertyChanged"))
                return;

            var accessors = prop.AccessorList.Accessors;
            if (accessors.Count < 2)
                return;

            if (OnPropertyChangedCallHelper.CallsOnPropertyChanged(accessors[1]))
                return;

            var diag = Diagnostic.Create(Rule,prop.Identifier.GetLocation());
            tree.ReportDiagnostic(diag);
        }
    }
}
