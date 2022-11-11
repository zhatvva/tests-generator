using Xunit;
using TestsGenerator.Core.Servises;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TestsGenerator.Core.Interfaces;
using System.Reflection;

namespace TestsGenerator.Tests
{
    public class GeneratorTests
    {
        private readonly ITestsGenerator _generator = new TestsGenerationService();

        [Fact]
        public void Generate_Returns3TestClasses()
        {
            var source = _generator.Generate(SourceCode.ClassWithConstructor + "\n" 
                + SourceCode.ClassWithOverridedMethod + "\n"
                + SourceCode.ClassWithInterfacePassedInConstructor);

            Assert.Equal(3, source.Count);
        }

        [Fact]
        public void Generate_ReturnsTestClassWithSetUpMethod()
        {
            var source = _generator.Generate(SourceCode.ClassWithConstructor).First().Content;
            var actual = TestHelper.GetClass(source);

            var methods = TestHelper.GetMethods(actual);
            Assert.Contains(methods, m => m.Identifier.ValueText == "SetUp");
            var method = methods.First(m => m.Identifier.ValueText == "SetUp");
            Assert.Single(method.AttributeLists);
            Assert.Single(method.AttributeLists[0].Attributes);
            Assert.Equal("SetUp", method.AttributeLists[0].Attributes[0].Name.ToString());

            var fields = TestHelper.GetFields(actual);
            Assert.Single(fields);
            Assert.Equal("private MyClass _testMyClass;", fields.First().ToString());
            Assert.NotNull(method.Body);
            Assert.Equal(3, method.Body!.Statements.Count);
            Assert.Equal("string nameFake = default;", method.Body!.Statements[0].ToString());
            Assert.Equal("int ageFake = default;", method.Body!.Statements[1].ToString());
            Assert.Equal("_testMyClass = new MyClass(nameFake, ageFake);", method.Body!.Statements[2].ToString());
        }

        [Theory]
        [InlineData("Method1Test")]
        [InlineData("Method2Test")]
        public void Generate_IfClassContainsOverridedMethods(string methodName)
        {
            var source = _generator.Generate(SourceCode.ClassWithOverridedMethod).First().Content;
            var actual = TestHelper.GetClass(source);
            
            var methods = TestHelper.GetMethods(actual);
            Assert.Contains(methods, m => m.Identifier.ValueText == methodName);
        }

        //[Fact]
        //public void Generate_IfClassContainsConstructorWithArguments()
        //{
        //    var source = _generator.Generate(SourceCode.ClassWithOverridedMethod).First().Content;
        //    var actual = TestHelper.GetClass(source);

        //    var method = TestHelper.GetMethods(actual).First(m => m.Identifier.ValueText == "SetUp");
        //    method.Body.ChildNodes().First(m => m.Ide
        //}
    }
}