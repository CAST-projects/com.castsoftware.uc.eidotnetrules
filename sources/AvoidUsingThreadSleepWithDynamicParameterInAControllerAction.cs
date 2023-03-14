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

        private static HashSet<INamedTypeSymbol> _controllerSymbols = new HashSet<INamedTypeSymbol>();

        /// <summary>
        /// Initialize the QR with the given context and register all the syntax nodes
        /// to listen during the visit and provide a specific callback for each one
        /// </summary>
        /// <param name="context"></param>
        public override void Init(AnalysisContext context)
        {
            //context.RegisterCompilationStartAction(OnCompilationStart);
            //context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ClassDeclaration);
        }

        private void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            context.Compilation.GetSymbolsForClasses(_baseControllers, ref _controllerSymbols);
        }

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            try
            {
                //InitializeSymbols(context);
                var classNode = context.Node;
                // Get Class symbol
                var classSymbol = context.SemanticModel.GetDeclaredSymbol(classNode) as INamedTypeSymbol;
                if (classSymbol == null)
                {
                    Console.WriteLine("class declaration unresolved : " + classNode.ToString());
                    return;
                }

                // Check that the class is a Controller
                if (!_controllerSymbols.Where(_ => classSymbol.IsOrInheritsFrom(_)).Any())
                    return;

                var declarationMethodNodes = classNode.DescendantNodes().OfType<MethodDeclarationSyntax>();
                // Check each actions for unsafe binding
                foreach (var declationMethodNode in declarationMethodNodes)
                {
                    // Get Method Symbol
                    var methodSymbol = context.SemanticModel.GetDeclaredSymbol(declationMethodNode) as IMethodSymbol;
                    if (methodSymbol == null)
                        continue;
                    // Check if it's a POST action
                    if (!methodSymbol.GetAttributes().Select(_ => _.AttributeClass.Name).Contains("HttpPostAttribute"))
                        continue;

                    
                    //foreach (var dataModel in intersectDataModels)
                    //{
                    //    var dataSaveNode = dataSaved[dataModel];
                    //    var pos = dataSaveNode.GetLocation().GetMappedLineSpan();
                    //    AddViolation(methodSymbol, new[] { pos });
                    //}

                }
            }
            catch (Exception e)
            {
                Log.Warn(" Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath + ": " + context.Node.GetLocation().GetMappedLineSpan(), e);
            }
        }
    }
}
