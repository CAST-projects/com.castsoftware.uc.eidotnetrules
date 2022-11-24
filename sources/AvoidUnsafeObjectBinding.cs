using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.DotNet.CastDotNetExtension;
using CastDotNetExtension.Utils;

namespace CastDotNetExtension
{
    [CastRuleChecker]
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [RuleDescription(
       Id = "EI_AvoidUnsafeObjectBinding",
       Title = "Avoid unsafe object binding",
       MessageFormat = "Avoid unsafe object binding",
       Category = "Secure Coding - Input Validation",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.AvoidUnsafeObjectBinding"
   )]
    public class AvoidUnsafeObjectBinding : AbstractRuleChecker
    {
        private static HashSet<string> _baseControllers = new HashSet<string>()
        {
            "System.Web.Mvc.Controller",
            "Microsoft.AspNetCore.Mvc.Controller",
            "System.Web.Mvc.AsyncController"
        };
        private static HashSet<string> _dbContexts = new HashSet<string>()
        { 
            "System.Data.Entity.DbContext", "Microsoft.EntityFrameworkCore.DbContext"
        };
        private static HashSet<string> _dbSets = new HashSet<string>()
        {
            "System.Data.Entity.DbSet`1", "Microsoft.EntityFrameworkCore.DbSet`1"
        };
        private static HashSet<INamedTypeSymbol> _controllerSymbols = new HashSet<INamedTypeSymbol>();
        private static HashSet<INamedTypeSymbol> _dbContextSymbols = new HashSet<INamedTypeSymbol>();
        private static HashSet<INamedTypeSymbol> _dbSetSymbols = new HashSet<INamedTypeSymbol>();

        private static HashSet<string> _dbSaveMethods = new HashSet<string>()
        {
            "SaveChanges", "SaveChangesAsync"
        };

        private static HashSet<string> _modelUpdateMethods = new HashSet<string>()
        {
            "TryUpdateModel", "UpdateModel", "TryUpdateModelAsync"
        };

        /// <summary>
        /// Initialize the QR with the given context and register all the syntax nodes
        /// to listen during the visit and provide a specific callback for each one
        /// </summary>
        /// <param name="context"></param>
        public override void Init(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ClassDeclaration);
        }

        private void InitializeSymbols(SyntaxNodeAnalysisContext context)
        {
            if (!_controllerSymbols.Any())
            {
                context.Compilation.GetSymbolsForClasses(_baseControllers, ref _controllerSymbols);
            }
            if (!_dbContextSymbols.Any())
            {
                context.Compilation.GetSymbolsForClasses(_dbContexts, ref _dbContextSymbols);
            }
            if (!_dbSetSymbols.Any())
            {
                context.Compilation.GetSymbolsForClasses(_dbSets, ref _dbSetSymbols);
            }
        }

        private bool HasBindAttribute(ParameterSyntax parameter)
        {
            foreach(var attributeList in parameter.AttributeLists)
            {
                foreach(var attribute in attributeList.Attributes)
                {
                    var identifierName = attribute.Name as IdentifierNameSyntax;
                    if (identifierName != null && identifierName.Identifier.ValueText == "Bind")
                        return true;
                }
            }
            return false;
        }

        private bool HasIncludeOrExcludeProperties(SyntaxNodeAnalysisContext context, ArgumentListSyntax argList)
        {
            foreach(var argument in argList.Arguments)
            {
                if (argument.NameColon != null &&
                    (argument.NameColon.Name.Identifier.ValueText == "includeProperties"
                    || argument.NameColon.Name.Identifier.ValueText == "excludeProperties"))
                    return true;

                if (argument.Expression != null)
                {
                    if( argument.Expression is ImplicitArrayCreationExpressionSyntax)
                        return true;
                    var argIdentifierName = argument.Expression as IdentifierNameSyntax;
                    if (argIdentifierName != null)
                    {
                        var argTypeSymbol = context.SemanticModel.GetTypeInfo(argIdentifierName).Type;
                        var argTypeSymbolName = argTypeSymbol.ToString();
                        if (argTypeSymbolName == "string[]")
                            return true;
                    }
                }

            }
            return false;
        }

        // Get symbol type of variable declared assuming this variable is initializer by getting a value from a DbSet of a DbContext
        private void GetVariablDeclarationSymbolType(SyntaxNodeAnalysisContext context, VariableDeclaratorSyntax declaratorSyntax, ref INamedTypeSymbol namedTypeSymbol)
        {
            var initializer = declaratorSyntax.Initializer as EqualsValueClauseSyntax;
            if(initializer != null)
            {
                var initializationValue = initializer.Value as InvocationExpressionSyntax;
                if(initializationValue != null)
                {
                    SyntaxNode node = initializationValue.Expression;
                    MemberAccessExpressionSyntax previousNode = null;
                    while(node is MemberAccessExpressionSyntax || node is InvocationExpressionSyntax)
                    {
                        previousNode = node as MemberAccessExpressionSyntax;
                        var memberAccessNode = node as MemberAccessExpressionSyntax;
                        if(memberAccessNode!= null)
                        {
                            node = memberAccessNode.Expression;
                        }
                        else
                        {
                            var invocNode = node as InvocationExpressionSyntax;
                            if (invocNode != null)
                            {
                                node = invocNode.Expression;
                            }
                            else
                                node = null;
                        }
                        
                    }
                    if(previousNode is MemberAccessExpressionSyntax && node is IdentifierNameSyntax)
                    {
                        ITypeSymbol expressionTypeSymbol = context.SemanticModel.GetTypeInfo(node).Type;
                        // Check that the Expression is a DbContext
                        if (expressionTypeSymbol != null || _dbContextSymbols.Where(_ => expressionTypeSymbol.IsOrInheritsFrom(_)).Any())
                        {
                            var dbSetName = previousNode.Name;
                            INamedTypeSymbol dbSetType = context.SemanticModel.GetTypeInfo(dbSetName).Type as INamedTypeSymbol;
                            if (dbSetType != null)
                            {
                                var typeArgument = dbSetType.TypeArguments.FirstOrDefault() as INamedTypeSymbol;
                                if (typeArgument != null)
                                {
                                    namedTypeSymbol = typeArgument;
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }


        private void GetModelBindedForAction(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax declationMethodNode, 
            ref HashSet<INamedTypeSymbol> modelsBinded)
        {
            // Get data models in the the action parameters
            var parameters = declationMethodNode.ParameterList.Parameters;
            foreach(var parameter in parameters)
            {
                var typeSymb = context.SemanticModel.GetTypeInfo(parameter.Type).Type;
                INamedTypeSymbol parameterTypeSymbol = typeSymb as INamedTypeSymbol;
                if(parameterTypeSymbol != null && !HasBindAttribute(parameter))
                {
                    modelsBinded.Add(parameterTypeSymbol);
                }
            }

            // Get data models explicitly saved in DB
            var invocationExpressionNodes = declationMethodNode
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>();
            var updateModels = invocationExpressionNodes
                .Where(_ => 
                        {
                            var identifierName = _.Expression as IdentifierNameSyntax;
                            if (identifierName == null)
                                return false;
                            return _modelUpdateMethods.Contains(identifierName.Identifier.ValueText);
                        }); // check update model methods
            foreach(var updateModel in updateModels)
            {
                if (HasIncludeOrExcludeProperties(context, updateModel.ArgumentList))
                    continue;
                var parameter = updateModel.ArgumentList.Arguments.First().Expression;
                if (parameter == null)
                    continue;
                INamedTypeSymbol parameterTypeSymbol = context.SemanticModel.GetTypeInfo(parameter).Type as INamedTypeSymbol;
                if (parameterTypeSymbol is IErrorTypeSymbol)
                {
                    var parameterSymbolInfo = context.SemanticModel.GetSymbolInfo(parameter);
                    var parameterSymbol = parameterSymbolInfo.Symbol;
                    var declaringSyntaxRefs = parameterSymbol.DeclaringSyntaxReferences;
                    foreach (var syntaxRef in declaringSyntaxRefs)
                    {
                        var declaringNode = syntaxRef.GetSyntax();
                        if (declaringNode is VariableDeclaratorSyntax)
                            GetVariablDeclarationSymbolType(context, declaringNode as VariableDeclaratorSyntax, ref parameterTypeSymbol);
                    }
                }
                if (parameterTypeSymbol != null)
                {
                    modelsBinded.Add(parameterTypeSymbol);
                }
            }
        }

        private bool GetDatabaseSavingModel(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax declationMethodNode,
            ref HashSet<INamedTypeSymbol> dataSaved)
        {
            // check for saving model values in DB
            var invocationExpressionNodes = declationMethodNode
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>();
            var dbSaveNodes = invocationExpressionNodes
                .Select(_ => _.Expression as MemberAccessExpressionSyntax)
                .Where(_ => _ != null)
                .Where(_ => _dbSaveMethods.Contains(_.Name.Identifier.ValueText)); // check DB save methods
            if (!dbSaveNodes.Any())
                return false;

            foreach (var dbSaveNode in dbSaveNodes)
            {
                ITypeSymbol expressionTypeSymbol = context.SemanticModel.GetTypeInfo(dbSaveNode.Expression).Type;
                // Check that the Expression is a DbContext
                if (expressionTypeSymbol != null || _dbContextSymbols.Where(_ => expressionTypeSymbol.IsOrInheritsFrom(_)).Any())
                {
                    var propertyMembers = expressionTypeSymbol.GetMembers().OfType<IPropertySymbol>();
                    foreach (var property in propertyMembers)
                    {
                        var propertyType = property.Type as INamedTypeSymbol;
                        if (propertyType != null && _dbSetSymbols.Contains(propertyType.ConstructedFrom))
                        {
                            var typeArgument = propertyType.TypeArguments.First() as INamedTypeSymbol;
                            if (typeArgument != null)
                            {
                                dataSaved.Add(typeArgument);
                                return true;
                            }
                            
                        }
                    }
                }
            }
            return false;
        }

        private void ExcludeBindNeverAndReadOnlyAttribute(SyntaxNodeAnalysisContext context, ref HashSet<INamedTypeSymbol> dataModels)
        {
            var dataModelsToExclude = new HashSet<INamedTypeSymbol>();
            foreach (var dataModel in dataModels)
            {
                bool IsDataModelExcluded = false;
                foreach(var member in dataModel.GetMembers())
                {

                    var memberAttributes = member.GetAttributes();
                    foreach(var attribute in memberAttributes)
                    {
                        var attributeString = attribute.ToString();
                        if (attributeString == "BindNever" || attributeString == "ReadOnly")
                        {
                            dataModelsToExclude.Add(dataModel);
                            IsDataModelExcluded = true;
                            break;
                        }
                    }
                    if (IsDataModelExcluded)
                        break;
                }
            }
            foreach(var dataModel in dataModelsToExclude)
            {
                dataModels.Remove(dataModel);
            }
        }

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            try
            {
                InitializeSymbols(context);
                var classNode = context.Node;
                // Get Class symbol
                var classSymbol = context.SemanticModel.GetDeclaredSymbol(classNode) as INamedTypeSymbol;
                if(classSymbol == null)
                {
                    Console.WriteLine("class declaration unresolved : " + classNode.ToString());
                    return;
                }

                // Check that the class is a Controller
                if (!_controllerSymbols.Where(_ => classSymbol.IsOrInheritsFrom(_)).Any())
                    return;

                var declarationMethodNodes = classNode.DescendantNodes().OfType<MethodDeclarationSyntax>();
                // Check each actions for unsafe binding
                foreach(var declationMethodNode in declarationMethodNodes)
                {
                    // Get Method Symbol
                    var methodSymbol = context.SemanticModel.GetDeclaredSymbol(declationMethodNode) as IMethodSymbol;
                    if (methodSymbol == null)
                        continue;
                    // Check if it's a POST action
                    if (!methodSymbol.GetAttributes().Select(_ => _.AttributeClass.Name).Contains("HttpPostAttribute"))
                        continue;

                    HashSet<INamedTypeSymbol> modelsBinded = new HashSet<INamedTypeSymbol>();
                    HashSet<INamedTypeSymbol> dataSaved = new HashSet<INamedTypeSymbol>();
                    GetModelBindedForAction(context, declationMethodNode, ref modelsBinded);
                    GetDatabaseSavingModel(context, declationMethodNode, ref dataSaved);
                    var intersectDataModels = modelsBinded.Intersect(dataSaved).ToHashSet();
                    ExcludeBindNeverAndReadOnlyAttribute(context, ref intersectDataModels);
                    foreach (var dataModel in intersectDataModels)
                    {
                        var pos = declationMethodNode.GetLocation().GetMappedLineSpan();
                        AddViolation(methodSymbol, new[] { pos });
                    }
                   
                }
            }
            catch (Exception e)
            {
                Log.Warn(" Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath + ": " + context.Node.GetLocation().GetMappedLineSpan(), e);
            }
        }
    }
}
