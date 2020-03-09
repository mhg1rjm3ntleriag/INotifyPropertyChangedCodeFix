using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace INotifyPropertyChangedCodeFix
{
    public static class ExtensionMethods
    {
        public static ClassDeclarationSyntax AddField(this ClassDeclarationSyntax cls,string fieldType,string fieldName)
        {
            var declarator = SyntaxFactory.VariableDeclarator(fieldName);
            var variable = SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName(fieldType), declarator.ToSeparated());
            
            var field = SyntaxFactory.FieldDeclaration(new SyntaxList<AttributeListSyntax>(),SyntaxFactory.TokenList(SyntaxFactory.Token( SyntaxKind.PrivateKeyword)), variable);
            return cls.AddMembers(field);
        }

        public static SeparatedSyntaxList<T> ToSeparated<T>(this T node) where T : SyntaxNode
        {
            return SyntaxFactory.SeparatedList<T>(new List<T>() { node});
        }
    }
}
