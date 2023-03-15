using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
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
       Id = "EI_AvoidUsingThreadSleepWithDynamicParameterInAControllerAction",
       Title = "Avoid using Thread.Sleep with dynamic parameter in a controller action",
       MessageFormat = "Avoid using Thread.Sleep with dynamic parameter in a controller action",
       Category = "Secure Coding - Time and State",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.AvoidUsingThreadSleepWithDynamicParameterInAControllerAction"
   )]
    public class AvoidUsingThreadSleepWithDynamicParameterInAControllerAction : AbstractRuleChecker
    {
        private static HashSet<string> _baseControllers = new HashSet<string>()
        {
            "System.Web.Mvc.Controller",
            "Microsoft.AspNetCore.Mvc.Controller",
            "System.Web.Mvc.AsyncController"
        };

        private static HashSet<string> _modelUpdateMethods = new HashSet<string>()
        {
            "TryUpdateModel", "UpdateModel", "TryUpdateModelAsync"
        };

        private static HashSet<INamedTypeSymbol> _controllerSymbols = new HashSet<INamedTypeSymbol>();

        /// <summary>
        /// Initialize the QR with the given context and register all the syntax nodes
        /// to listen during the visit and provide a specific callback for each one
        /// </summary>
        /// <param name="context"></param>
        public override void Init(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(OnCompilationStart);
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ClassDeclaration);
        }

        private void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            context.Compilation.GetSymbolsForClasses(_baseControllers, ref _controllerSymbols);
        }

        /// <summary>
        /// Check invocation expression nodes for a call to Thread.Sleep
        /// </summary>
        /// <param name="invocationNode"></param>
        /// <param name="sleepCall"></param>
        /// <returns></returns>
        private bool CheckForCallToThreadSleep(IEnumerable<InvocationExpressionSyntax> invocationExpressionNodes, ref InvocationExpressionSyntax sleepCall)
        {
            foreach (var invocationNode in invocationExpressionNodes)
            {
                var accessMemberNode = invocationNode.Expression as MemberAccessExpressionSyntax;
                if (accessMemberNode != null)
                {
                    var expression = accessMemberNode.Expression as IdentifierNameSyntax;
                    var name = accessMemberNode.Name as IdentifierNameSyntax;
                    if(expression != null && name != null)
                    {
                        if(expression.Identifier.ValueText == "Thread" && name.Identifier.ValueText == "Sleep")
                        {
                            sleepCall = invocationNode;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Verify if classNode is the declaration of a controller class
        /// </summary>
        /// <param name="context"></param>
        /// <param name="classNode"></param>
        /// <returns></returns>
        private bool IsClassAController(SyntaxNodeAnalysisContext context, SyntaxNode classNode)
        {
            // Get Class symbol
            var classSymbol = context.SemanticModel.GetDeclaredSymbol(classNode) as INamedTypeSymbol;
            if (classSymbol == null)
            {
                Console.WriteLine("class declaration unresolved : " + classNode.ToString());
                return false;
            }

            // Check that the class is a Controller
            if (!_controllerSymbols.Where(_ => classSymbol.IsOrInheritsFrom(_)).Any())
                return false;
            return true;
        }

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            try
            {
                //InitializeSymbols(context);
                var classNode = context.Node;

                // Check if class is a Controller
                if (!IsClassAController(context,classNode))
                    return;

                var declarationMethodNodes = classNode.DescendantNodes().OfType<MethodDeclarationSyntax>();
                // Check each actions Thread.Sleep call
                foreach (var declationMethodNode in declarationMethodNodes)
                {
                    // An action is a public method of a controller
                    // Get Method Symbol
                    var methodSymbol = context.SemanticModel.GetDeclaredSymbol(declationMethodNode) as IMethodSymbol;
                    if (methodSymbol == null)
                        continue;
                    // Check if the method is public
                    if (methodSymbol.DeclaredAccessibility != Accessibility.Public)
                        continue;

                    var invocationExpressionNodes = declationMethodNode.DescendantNodes().OfType<InvocationExpressionSyntax>();
                    // Check for a call to Thread.Sleep
                    InvocationExpressionSyntax sleepCall = null;
                    if (!CheckForCallToThreadSleep(invocationExpressionNodes, ref sleepCall))
                        continue;
                    ArgumentSyntax sleepParam = sleepCall.ArgumentList.Arguments.First();
                    if (sleepParam == null)
                        continue;

                    var sleepParamNames = sleepParam.Expression
                                            .DescendantNodesAndSelf()
                                            .OfType<IdentifierNameSyntax>()
                                            .Select(_=>_.Identifier.ValueText);


                    var parameters = declationMethodNode.ParameterList.Parameters;
                    var parametersNames = parameters.Select(_ => _.Identifier.ValueText);
                    
                    if(sleepParamNames.Intersect(parametersNames).Count()>0)
                    {
                        var pos = sleepCall.GetLocation().GetMappedLineSpan();
                        AddViolation(methodSymbol, new[] { pos });
                        continue;
                    }


                    // Check for a call to an update model method
                    var updateModels = 
                        invocationExpressionNodes
                            .Where(_ =>
                            {
                                var identifierName = _.Expression as IdentifierNameSyntax;
                                if (identifierName == null)
                                    return false;
                                return _modelUpdateMethods.Contains(identifierName.Identifier.ValueText);
                            }); // check update model methods

                    if (!updateModels.Any())
                        continue;
                    ArgumentSyntax updateModelParam = updateModels.First().ArgumentList.Arguments.First();
                    if (updateModelParam == null)
                        continue;

                    var updateModelParamNames = updateModelParam.Expression
                                            .DescendantNodesAndSelf()
                                            .OfType<IdentifierNameSyntax>()
                                            .Select(_ => _.Identifier.ValueText);

                    if (sleepParamNames.Intersect(updateModelParamNames).Count() > 0)
                    {
                        var pos = sleepCall.GetLocation().GetMappedLineSpan();
                        AddViolation(methodSymbol, new[] { pos });
                        continue;
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
