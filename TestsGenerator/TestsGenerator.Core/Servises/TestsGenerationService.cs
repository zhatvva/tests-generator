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
        private readonly List<string> _methodNames = new();
        
        public Task<List<GenerationResult>> Generate(string file)
        {
            var tree = CSharpSyntaxTree.ParseText(file);
            var root = tree.GetCompilationUnitRoot();
            var classes = root.GetClasses();
            var result = new List<GenerationResult>(classes.Count);
            
            foreach (var c in classes)
            {
                var tests = GenerateTestsForSingleClass(c);
                result.Add(tests);
            }

            return Task.FromResult(result);
        }

        private static (ArgumentSyntax Argument, StatementSyntax InitializationExpression) GetParameterInitializationSection(
            ParameterSyntax parameter)
        {
            StatementSyntax initializationExpression;
            ArgumentSyntax constructorArgument;
            var objectName = $"{parameter.Identifier.Text}Object";
            var type = parameter.Type.ToString();
            
            if (type.StartsWith('I'))
            {
                initializationExpression = SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.IdentifierName(type))
                    .WithVariables(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier(objectName))
                            .WithInitializer(
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.ObjectCreationExpression(
                                        SyntaxFactory.GenericName(
                                            SyntaxFactory.Identifier("Mock"))
                                        .WithTypeArgumentList(
                                            SyntaxFactory.TypeArgumentList(
                                                SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                                    SyntaxFactory.IdentifierName(parameter.Identifier.Text)))))
                                    .WithArgumentList(
                                        SyntaxFactory.ArgumentList()))))));

                constructorArgument = SyntaxFactory.Argument(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(objectName),
                            SyntaxFactory.IdentifierName("Object")));
            }
            else
            {
                initializationExpression = SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.IdentifierName(type))
                    .WithVariables(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier(objectName))
                            .WithInitializer(
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.LiteralExpression(
                                        SyntaxKind.DefaultLiteralExpression,
                                        SyntaxFactory.Token(SyntaxKind.DefaultKeyword)))))));

                constructorArgument = SyntaxFactory.Argument(SyntaxFactory.IdentifierName(objectName));
            }

            return (constructorArgument, initializationExpression);
        }

        private static (string TestClassVariableName, List<MemberDeclarationSyntax> SetupSection) GetSetupSection(
            string className, ConstructorDeclarationSyntax constructor)
        {
            var testClassObjectName = $"_test{className}Object";
            var initializatons = new List<StatementSyntax>();
            var constructorArguments = new List<SyntaxNodeOrToken>();

            foreach (var parameter in constructor.ParameterList.Parameters)
            {
                var section = GetParameterInitializationSection(parameter);
                initializatons.Add(section.InitializationExpression);

                constructorArguments.Add(section.Argument);
                constructorArguments.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
            }

            constructorArguments.RemoveAt(constructorArguments.Count - 1);

            initializatons.Add(
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(testClassObjectName),
                        SyntaxFactory.ObjectCreationExpression(
                            SyntaxFactory.IdentifierName(className))
                        .WithArgumentList(
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                    new SyntaxNodeOrTokenList(constructorArguments)))))));

            var setup = new List<MemberDeclarationSyntax>()
            {
                SyntaxFactory.FieldDeclaration(
                    SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.IdentifierName(className))
                    .WithVariables(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier(testClassObjectName)))))
                .WithModifiers(
                    SyntaxFactory.TokenList(
                        SyntaxFactory.Token(SyntaxKind.PrivateKeyword))),

                SyntaxFactory.MethodDeclaration(
                    SyntaxFactory.PredefinedType(
                        SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                    SyntaxFactory.Identifier("SetUp"))
                .WithAttributeLists(
                    SyntaxFactory.SingletonList(
                        SyntaxFactory.AttributeList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Attribute(
                                    SyntaxFactory.IdentifierName("SetUp"))))))
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithBody(SyntaxFactory.Block(initializatons))
            };

            return (testClassObjectName, setup);
        }

        private static MemberDeclarationSyntax GetMethodSection(string variableName, MethodDeclarationSyntax method)
        {
            var statements = new List<StatementSyntax>();
            var methodArguments = new List<SyntaxNodeOrToken>();

            foreach (var parameter in method.ParameterList.Parameters)
            {
                var section = GetParameterInitializationSection(parameter);
                statements.Add(section.InitializationExpression);

                methodArguments.Add(section.Argument);
                methodArguments.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
            }

            methodArguments.RemoveAt(methodArguments.Count - 1);

            var methodDeclaration = SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(
                    SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                SyntaxFactory.Identifier($"{method.Identifier.ValueText}Test"))
            .WithAttributeLists(
                SyntaxFactory.SingletonList(
                    SyntaxFactory.AttributeList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Attribute(
                                SyntaxFactory.IdentifierName("Test"))))))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithBody(SyntaxFactory.Block(statements));

            return methodDeclaration;
        }

        private static List<MemberDeclarationSyntax> GetMethodsSection(string testClassVariableName, List<MethodDeclarationSyntax> methods)
        {
            var members = new List<MemberDeclarationSyntax>(methods.Count);

            foreach (var method in methods)
            {
                var methodSection = GetMethodSection(testClassVariableName, method);
                members.Add(methodSection);
            }

            return members;
        }
        
        private static GenerationResult GenerateTestsForSingleClass(ClassDeclarationSyntax syntax)
        {
            var className = syntax.Identifier.Text;
            var constructor = syntax.GetConstructorWithMaxArgumentsCount();
            var methods = syntax.GetPublicMethods();

            var setupSection = GetSetupSection(className, constructor);
            var methodsSecton = GetMethodsSection(setupSection.TestClassVariableName, methods);

            var members = new List<MemberDeclarationSyntax>(setupSection.SetupSection);
            members.AddRange(methodsSecton);

            var tree = CSharpSyntaxTree.Create(
                SyntaxFactory.CompilationUnit()
                    .WithUsings(
                        SyntaxFactory.List(
                            new UsingDirectiveSyntax[]{
                                SyntaxFactory.UsingDirective(
                                    SyntaxFactory.IdentifierName("NUnit.Framework")),
                                SyntaxFactory.UsingDirective(
                                    SyntaxFactory.IdentifierName("Moq"))}))
                    .WithMembers(
                        SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                            SyntaxFactory.NamespaceDeclaration(
                                SyntaxFactory.IdentifierName("Tests"))
                            .WithMembers(
                                SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                                    SyntaxFactory.ClassDeclaration($"{className}Tests")
                                    .WithAttributeLists(
                                        SyntaxFactory.SingletonList(
                                            SyntaxFactory.AttributeList(
                                                SyntaxFactory.SingletonSeparatedList(
                                                    SyntaxFactory.Attribute(
                                                        SyntaxFactory.IdentifierName("TestFixture"))))))
                                    .WithModifiers(
                                        SyntaxFactory.TokenList(
                                            SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                                    .WithMembers(
                                        SyntaxFactory.List(members))))))
                    .NormalizeWhitespace()
                );

            return new GenerationResult(className, tree.ToString());
        }
    }
}
