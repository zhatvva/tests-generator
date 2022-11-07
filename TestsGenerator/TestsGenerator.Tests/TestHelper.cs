using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace TestsGenerator.Tests
{
    public static class TestHelper
    {
        public static ClassDeclarationSyntax GetClass(string source)
        {
            var root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();
            return root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .ToList()
                .First();
        }

        public static List<MethodDeclarationSyntax> GetMethods(ClassDeclarationSyntax classSyntax)
        {
            return classSyntax.Members.OfType<MethodDeclarationSyntax>().ToList();
        }
    }
}
