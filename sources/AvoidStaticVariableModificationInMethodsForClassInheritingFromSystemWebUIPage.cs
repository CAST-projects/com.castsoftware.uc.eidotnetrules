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
       Id = "EI_AvoidStaticVariableModificationInMethodsForClassInheritingFromSystemWebUIPage",
       Title = "Avoid static variable modification in methods for class inheriting from  System.Web.UI.Page",
       MessageFormat = "Avoid static variable modification in methods for class inheriting from  System.Web.UI.Page",
       Category = "Secure Coding - Time and State",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.AvoidStaticVariableModificationInMethodsForClassInheritingFromSystemWebUIPage"
   )]
    public class AvoidStaticVariableModificationInMethodsForClassInheritingFromSystemWebUIPage : AbstractRuleChecker
    {
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
            //context.Compilation.GetSymbolsForClasses(_baseControllers, ref _controllerSymbols);
        }

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            try
            {

                //foreach (var dataModel in intersectDataModels)
                //{
                //    var dataSaveNode = dataSaved[dataModel];
                //    var pos = dataSaveNode.GetLocation().GetMappedLineSpan();
                //    AddViolation(methodSymbol, new[] { pos });
                //}

            }
            catch (Exception e)
            {
                Log.Warn(" Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath + ": " + context.Node.GetLocation().GetMappedLineSpan(), e);
            }
        }
    }
}
