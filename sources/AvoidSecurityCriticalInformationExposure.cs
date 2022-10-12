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

namespace CastDotNetExtension
{
    [CastRuleChecker]
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [RuleDescription(
       Id = "EI_AvoidSecurityCriticalInformationExposure",
       Title = "Avoid security-critical information exposure",
       MessageFormat = "Avoid security-critical information exposure",
       Category = "Secure Coding - Weak Security Features",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.AvoidSecurityCriticalInformationExposure"
   )]
    public class AvoidSecurityCriticalInformationExposure : AbstractRuleChecker
    {
        private static readonly HashSet<string> loggingNames = new HashSet<string> 
        {
         "System.Console::Writeline(string)",
         "System.Console::Write(string)",
        };

        private HashSet<IMethodSymbol> _loggingSymbols = new HashSet<IMethodSymbol>();
        /// <summary>
        /// Initialize the QR with the given context and register all the syntax nodes
        /// to listen during the visit and provide a specific callback for each one
        /// </summary>
        /// <param name="context"></param>
        public override void Init(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
        }

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            try
            {
                var invocation = context.Node as InvocationExpressionSyntax;
                if (invocation == null)
                    return;

                var expression = invocation.Expression as MemberAccessExpressionSyntax;
                if (expression == null)
                    return;

                //if (identifierName.Identifier.ValueText == "HtmlInputHidden")
                //{
                //    var pos = objectCreation.GetLocation().GetMappedLineSpan();
                //    AddViolation(context.ContainingSymbol, new[] { pos });
                //}
            }
            catch (Exception e)
            {
                Log.Warn(" Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath + ": " + context.Node.GetLocation().GetMappedLineSpan(), e);
            }
        }
    }
}
