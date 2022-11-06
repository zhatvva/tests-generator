using Microsoft.CodeAnalysis.CSharp.Syntax;
using TestsGenerator.Core.Models;

namespace TestsGenerator.Core.Servises
{
    internal class TestMethodNameGenerator
    {
        private readonly Dictionary<string, TestMethodNameGenerationInfo> _methods;

        public TestMethodNameGenerator(List<MethodDeclarationSyntax> methods)
        {
            _methods = new();
            var totalCounts = new Dictionary<string, int>(); 
            foreach (var method in methods)
            {
                var methodName = method.Identifier.ValueText;
                if (totalCounts.ContainsKey(methodName))
                {
                    ++totalCounts[methodName];
                }
                else
                {
                    totalCounts[methodName] = 1;
                }
            }

            foreach (var kv in totalCounts)
            {
                _methods.Add(kv.Key, new(kv.Value));
            }
        }

        public string GetTestMethodName(string methodName)
        {
            var method = _methods[methodName];
            if (method.MethodsWithTheSameNameCount <= 1)
            {
                return $"{methodName}Test";
            }
            else
            {
                return $"{methodName}{++method.LastGenerationNumber}Test";
            }
        }
    }
}
