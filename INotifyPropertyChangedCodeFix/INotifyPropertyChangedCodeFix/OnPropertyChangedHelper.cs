using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INotifyPropertyChangedCodeFix
{
    static class OnPropertyChangedHelper
    {
        public static bool ImplementsOnPropertyChanged(ClassDeclarationSyntax cls)
        {
            return cls.Members.OfType<MethodDeclarationSyntax>().Where(x => x.Identifier.Text == "OnPropertyChanged").Count() > 0;
        }
        public static Task<Document> ImplementOnPropertyChanged(Document document, Diagnostic diagnostic, SyntaxNode root)
        {
            var compilationUnit = root as CompilationUnitSyntax;
            var node = compilationUnit.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<ClassDeclarationSyntax>();
            return Task.FromResult(document.WithSyntaxRoot(CreateOnPropertyChanged(root,node)));
        }

        public static SyntaxNode CreateOnPropertyChanged(SyntaxNode root,ClassDeclarationSyntax node)
        {
            var compilationUnit = root as CompilationUnitSyntax;
            var method = CreateNotifyProperty();
            var newRoot = compilationUnit.ReplaceNode(node, node.AddMembers(method));
            var names = newRoot.Usings
                               .Select(x => x.Name as QualifiedNameSyntax)
                               .Where(x => x?.Right != null);

            if (!names.Any(x => (x.Right as SimpleNameSyntax).Identifier.Text == "CompilerServices"))
                newRoot = newRoot.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System.Runtime.CompilerServices")));

            if (!names.Any(x => (x.Right as SimpleNameSyntax).Identifier.Text == "ComponentModel"))
                newRoot = newRoot.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System.ComponentModel")));


            return newRoot;
        }

        public static MethodDeclarationSyntax CreateNotifyProperty()
        {
            MethodDeclarationSyntax method = SyntaxFactory.MethodDeclaration(SyntaxFactory.IdentifierName("void"), "OnPropertyChanged");
            var attribute = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("CallerMemberName"));

            var attributes = SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList<AttributeSyntax>(new List<AttributeSyntax>() { attribute }));

            var equals = SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression));
            var expression = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(""));
            equals = equals.WithValue(expression);
            var param = SyntaxFactory.Parameter(new SyntaxList<AttributeListSyntax>(attributes),
                                                new SyntaxTokenList(),
                                                SyntaxFactory.IdentifierName("string"),
                                                SyntaxFactory.Identifier("name"),
                                                equals);

            method = method.AddParameterListParameters(param);
            method = method.WithBody(CreateBlock());
            return method;
        }

        public static BlockSyntax CreateBlock()
        {
            var property = SyntaxFactory.IdentifierName("PropertyChanged");
            var memberBind = SyntaxFactory.MemberBindingExpression(SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName("Invoke"));

            var thisArg = SyntaxFactory.Argument(SyntaxFactory.IdentifierName("this"));

            var propC = SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName("PropertyChangedEventArgs"));
            propC = propC.AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("name")));

            var propConstructor = SyntaxFactory.Argument(propC);

            var argList = SyntaxFactory.ArgumentList();
            argList = argList.AddArguments(thisArg, propConstructor);

            var invoc = SyntaxFactory.InvocationExpression(memberBind, argList);
            var exp = SyntaxFactory.ConditionalAccessExpression(property, SyntaxFactory.Token(SyntaxKind.QuestionToken), invoc);
            var statement = SyntaxFactory.ExpressionStatement(exp, SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            return SyntaxFactory.Block(statement);
        }

        public static bool ImplementsType(this ClassDeclarationSyntax cls,string s)
        {
            if (cls.BaseList.Types.Select(x => x.Type).OfType<SimpleNameSyntax>().Any(x => x.Identifier.Text == s))
                return true;

            return false;
        }
    }
}
