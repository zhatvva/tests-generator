﻿using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TestsGenerator.Core.Extensions
{
    internal static class CompilationUnitSyntaxExtensions
    {
        public static List<ClassDeclarationSyntax> GetClasses(this CompilationUnitSyntax root) =>
            root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .ToList();
    }
}
