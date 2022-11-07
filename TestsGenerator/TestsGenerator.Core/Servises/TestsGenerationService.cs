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
            var classes = root.GetClasses();
            var result = new List<GenerationResult>(classes.Count);
            
            foreach (var c in classes)
            {
                var tests = GenerateTestsForSingleClass(c);
                result.Add(tests);
            }

            return Task.FromResult(result);
        }

        private static (ArgumentSyntax Argument, StatementSyntax InitializationExpression) GetParameterCreationSection(
            ParameterSyntax parameter)
        {
            StatementSyntax initializationExpression;
            ArgumentSyntax constructorArgument;
            var objectName = $"{parameter.Identifier.Text}Fake";
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
                                                    SyntaxFactory.IdentifierName(type)))))
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
                initializationExpression = GetLocalDeclarationStatement(type, objectName);
                constructorArgument = SyntaxFactory.Argument(SyntaxFactory.IdentifierName(objectName));
            }

            return (constructorArgument, initializationExpression);
        }

        private static LocalDeclarationStatementSyntax GetLocalDeclarationStatement(string type, string variableName)
        {
            var initializationExpression = SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.IdentifierName(type))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                            SyntaxFactory.Identifier(variableName))
                        .WithInitializer(
                            SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.LiteralExpression(
                                    SyntaxKind.DefaultLiteralExpression,
                                    SyntaxFactory.Token(SyntaxKind.DefaultKeyword)))))));

            return initializationExpression;
        }

        private static (string TestClassVariableName, List<MemberDeclarationSyntax> SetupSection) GetSetupSection(
            string className, ConstructorDeclarationSyntax constructor)
        {
            var testClassObjectName = $"_test{className}";
            var initializatons = new List<StatementSyntax>();
            var constructorArguments = new List<SyntaxNodeOrToken>();
            
            if (constructor != null && constructor.ParameterList.Parameters.Count > 0)
            {
                foreach (var parameter in constructor.ParameterList.Parameters)
                {
                    var section = GetParameterCreationSection(parameter);
                    initializatons.Add(section.InitializationExpression);

                    constructorArguments.Add(section.Argument);
                    constructorArguments.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
                }

                constructorArguments.RemoveAt(constructorArguments.Count - 1);
            }
            
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

        private static MemberDeclarationSyntax GetMethodSection(string testMethodName, string testClassVariableName,
            MethodDeclarationSyntax method)
        {
            var statements = new List<StatementSyntax>();
            var methodArguments = new List<SyntaxNodeOrToken>();
            var actResultVariableName = "actual";
            var expectedVariableName = "expected";

            if (method.ParameterList.Parameters.Count > 0)
            {
                foreach (var parameter in method.ParameterList.Parameters)
                {
                    var section = GetParameterCreationSection(parameter);
                    statements.Add(section.InitializationExpression);

                    methodArguments.Add(section.Argument);
                    methodArguments.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
                }

                methodArguments.RemoveAt(methodArguments.Count - 1);
            }

            if (method.ReturnType is PredefinedTypeSyntax predifinedReturnType && predifinedReturnType.Keyword.ValueText == "void")  
            {
                var actStatement = SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(testClassVariableName),
                            SyntaxFactory.IdentifierName(method.Identifier.ValueText)))
                    .WithArgumentList(
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                new SyntaxNodeOrTokenList(methodArguments)))));

                statements.Add(actStatement);
            }
            else
            {
                string returnTypeName;
                if (method.ReturnType is PredefinedTypeSyntax predefinedTypeSyntax)
                {
                    returnTypeName = predefinedTypeSyntax.Keyword.ValueText;
                }
                else if (method.ReturnType is IdentifierNameSyntax identifierNameSyntax)
                {
                    returnTypeName = identifierNameSyntax.Identifier.ValueText;
                }
                else
                {
                    returnTypeName = "object";
                }

                var actStatement = SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.IdentifierName(returnTypeName))
                    .WithVariables(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier(actResultVariableName))
                            .WithInitializer(
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.InvocationExpression(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.IdentifierName(testClassVariableName),
                                            SyntaxFactory.IdentifierName(method.Identifier.ValueText)))
                                    .WithArgumentList(
                                        SyntaxFactory.ArgumentList(
                                            SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                                new SyntaxNodeOrTokenList(methodArguments)))))))));
                statements.Add(actStatement);

                var expectedVariableDeclarationStatement = GetLocalDeclarationStatement(returnTypeName, expectedVariableName);
                statements.Add(expectedVariableDeclarationStatement);

                var resultAssertionStatement = SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("Assert"),
                            SyntaxFactory.IdentifierName("That")))
                    .WithArgumentList(
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                new SyntaxNodeOrToken[]{
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.IdentifierName(actResultVariableName)),
                                    SyntaxFactory.Token(SyntaxKind.CommaToken),
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.InvocationExpression(
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.IdentifierName("Is"),
                                                SyntaxFactory.IdentifierName("EqualTo")))
                                        .WithArgumentList(
                                            SyntaxFactory.ArgumentList(
                                                SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                                                    SyntaxFactory.Argument(
                                                        SyntaxFactory.IdentifierName(expectedVariableName))))))}))));

                statements.Add(resultAssertionStatement);
            }

            var assertFailStatement = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("Assert"),
                        SyntaxFactory.IdentifierName("Fail")))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                SyntaxFactory.LiteralExpression(
                                    SyntaxKind.StringLiteralExpression,
                                    SyntaxFactory.Literal("autogenerated")))))));

            statements.Add(assertFailStatement);

            var methodDeclaration = SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(
                    SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                SyntaxFactory.Identifier(testMethodName))
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
            var generator = new TestMethodNameGenerator(methods);

            foreach (var method in methods)
            {
                var methodName = generator.GetTestMethodName(method.Identifier.ValueText);
                var methodSection = GetMethodSection(methodName, testClassVariableName, method);
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
