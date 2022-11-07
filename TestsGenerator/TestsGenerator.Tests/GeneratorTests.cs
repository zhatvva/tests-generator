using Xunit;
using TestsGenerator.Core.Servises;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TestsGenerator.Tests
{
    public class GeneratorTests
    {
        [Theory]
        [InlineData("SetUp")]
        [InlineData("Method1Test")]
        [InlineData("Method2Test")]
        public void Generate_IfClassContainsOverridedMethods(string methodName)
        {
            var generator = new TestsGenerationService();
            
            var source = generator.Generate(SourceCode.ClassWithOverridedMethod).First().Content;
            var actual = TestHelper.GetClass(source);
            
            var methods = TestHelper.GetMethods(actual);
            Assert.Contains(methods, m => m.Identifier.ValueText == methodName);
        }

        [Fact]
        public void Generate_IfClassContainsConstructorWithArguments()
        {
            var generator = new TestsGenerationService();

            var source = generator.Generate(SourceCode.ClassWithOverridedMethod).First().Content;
            var actual = TestHelper.GetClass(source);

            var method = TestHelper.GetMethods(actual).First(m => m.Identifier.ValueText == "SetUp");
            method.Body.ChildNodes().First(m => m.Ide
        }
    }
}