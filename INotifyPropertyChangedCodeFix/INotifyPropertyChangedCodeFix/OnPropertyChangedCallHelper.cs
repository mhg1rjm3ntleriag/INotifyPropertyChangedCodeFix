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
    public static class OnPropertyChangedCallHelper
    {
        public static bool CallsOnPropertyChanged(AccessorDeclarationSyntax decl)
        {
            if (decl.Body == null)
                return false;
            var block = decl?.Body as BlockSyntax;
            if (block == null)
                return false;


            foreach (ExpressionStatementSyntax item in block.Statements)
            {
                var invoc = item.Expression as InvocationExpressionSyntax;
                if (invoc == null)
                    continue;
                var identifier = invoc.Expression as SimpleNameSyntax;

                if (identifier == null)
                    continue;

                if (identifier.Identifier.Text == "OnPropertyChanged")
                    return true;
            }

            return false;
        }

        public static Task<Document> AddCallToOnPropertyChangedInProp(Document document, Diagnostic diagnostic, SyntaxNode root)
        {
            var compilationUnit = root as CompilationUnitSyntax;
            var node = compilationUnit.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<PropertyDeclarationSyntax>();

            var newRoot = root;


            if (node.AccessorList.Accessors[1].Body == null)
                (newRoot, node) = ConvertToFullProperty(newRoot, node);


            (newRoot,node) = AddCallToOnPropertyChanged(newRoot, node.AccessorList.Accessors[1]);

            var cls = node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (!OnPropertyChangedHelper.ImplementsOnPropertyChanged(cls))
                newRoot = OnPropertyChangedHelper.CreateOnPropertyChanged(newRoot, cls);

            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        private static (SyntaxNode newRoot, PropertyDeclarationSyntax newNode) AddCallToOnPropertyChanged(SyntaxNode root, AccessorDeclarationSyntax node)
        {
            InvocationExpressionSyntax invoc = SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("OnPropertyChanged"));
            ExpressionStatementSyntax expr = SyntaxFactory.ExpressionStatement(invoc, SyntaxFactory.Token(SyntaxKind.SemicolonToken));

            var annotation = new SyntaxAnnotation("new_call_node", "");
            var newNode = node.AddBodyStatements(expr).WithAdditionalAnnotations(annotation);
            var newRoot = root.ReplaceNode(node, newNode);

            newNode = newRoot.GetAnnotatedNodes(annotation).First() as AccessorDeclarationSyntax;
            return (newRoot,newNode.Parent.Parent as PropertyDeclarationSyntax);
        }

        private static (SyntaxNode newRoot, PropertyDeclarationSyntax newNode) ConvertToFullProperty(SyntaxNode root, PropertyDeclarationSyntax node)
        {
            string nodeType = (node.Type as TypeSyntax).GetText().ToString();
            string fieldName =  node.Identifier.Text;
            fieldName = fieldName.First().ToString().ToLower() + new String(fieldName.Skip(1).ToArray());
            if (fieldName == node.Identifier.Text)
                fieldName = "_" + fieldName;







            var getArrow = SyntaxFactory.ArrowExpressionClause(SyntaxFactory.IdentifierName(fieldName));
            var newGetter = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration);
            newGetter = newGetter.WithExpressionBody(getArrow);
            newGetter = newGetter.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

            var assignment = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                                                SyntaxFactory.IdentifierName(fieldName),
                                                                SyntaxFactory.IdentifierName("value"));

            var newSetter = SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration,
                                                              SyntaxFactory.Block(SyntaxFactory.ExpressionStatement(assignment)));

            var annotation = new SyntaxAnnotation("prop_with_new_setter", "");

            var accessorList = SyntaxFactory.AccessorList();
            var newaccessorList = accessorList.AddAccessors(newGetter,newSetter);
            var newNode = node.WithAccessorList(newaccessorList)
                              .WithAdditionalAnnotations(annotation);

            var cls = node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            var newCls = cls.ReplaceNode(node,newNode);

            newCls = newCls.AddField(nodeType, fieldName);

            var newRoot = root.ReplaceNode(cls, newCls);

            newNode = newRoot.GetAnnotatedNodes(annotation).First() as PropertyDeclarationSyntax;
            return (newRoot, newNode);
        }
    }
}
