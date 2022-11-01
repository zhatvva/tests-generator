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

        private static (ArgumentSyntax ConstructorArgument, StatementSyntax InitializationExpression) GetParameterInitializationSection(
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

        private static (string TestClassObjectName, List<MemberDeclarationSyntax> SetupSection) GetSetupSection(
            string className, ConstructorDeclarationSyntax constructor)
        {
            var testClassObjectName = $"_test{className}Object";
            var initializatons = new List<StatementSyntax>();
            var constructorArguments = new SeparatedSyntaxList<ArgumentSyntax>();

            foreach (var parameter in constructor.ParameterList.Parameters)
            {
                var section = GetParameterInitializationSection(parameter);
                initializatons.Add(section.InitializationExpression);
                constructorArguments.Add(section.ConstructorArgument);
            }

            initializatons.Add(
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(testClassObjectName),
                        SyntaxFactory.ObjectCreationExpression(
                            SyntaxFactory.IdentifierName(className))
                        .WithArgumentList(SyntaxFactory.ArgumentList(constructorArguments)))));

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

        private static List<MemberDeclarationSyntax> GetMethodSection(string testClassObjectName, List<MethodDeclarationSyntax> methods)
        {
            throw new NotImplementedException();
        }
        
        private static GenerationResult GenerateTestsForSingleClass(ClassDeclarationSyntax syntax)
        {
            var className = syntax.Identifier.Text;
            var constructor = syntax.GetConstructorWithMaxArgumentsCount();
            var methods = syntax.GetPublicMethods();

            var setupSection = GetSetupSection(className, constructor);
            //var methodSecton = GetMethodSection(setupSection.TestClassObjectName, methods);
            var methodSecton = new List<MemberDeclarationSyntax>();

            var members = new List<MemberDeclarationSyntax>(setupSection.SetupSection);
            members.AddRange(methodSecton);

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
