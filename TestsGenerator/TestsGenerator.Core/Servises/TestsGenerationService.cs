using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TestsGenerator.Core.Interfaces;
using TestsGenerator.Core.Models;
using TestsGenerator.Core.Extensions;

namespace TestsGenerator.Core.Servises
{
    public class TestsGenerationService : ITestsGenerator
    {
        public Task<List<GenerationResult>> Generate(string file)
        {
            var tree = CSharpSyntaxTree.ParseText(file);
            var root = tree.GetCompilationUnitRoot();
            var classes = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .ToList();
            var result = new List<GenerationResult>(classes.Count);
            
            foreach (var c in classes)
            {
                var tests = GenerateTestsForSingleClass(c);
                result.Add(tests);
            }

            return Task.FromResult(result);
        }

        private static GenerationResult GenerateTestsForSingleClass(ClassDeclarationSyntax syntax)
        {
            var className = syntax.Identifier.Text;
            var constructor = syntax.GetConstructorWithMaxArgumentsCount();
            var methods = syntax.GetPublicMethods();

            return new GenerationResult(className, className);
        }
    }
}
