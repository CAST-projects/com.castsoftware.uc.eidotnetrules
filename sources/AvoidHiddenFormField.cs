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
       Id = "EI_AvoidHiddenFormField",
       Title = "Avoid hidden form field",
       MessageFormat = "Avoid hidden form field",
       Category = "Secure Coding - Weak Security Features",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.AvoidHiddenFormField"
   )]
    public class AvoidHiddenFormField : AbstractRuleChecker
    {
        /// <summary>
        /// Initialize the QR with the given context and register all the syntax nodes
        /// to listen during the visit and provide a specific callback for each one
        /// </summary>
        /// <param name="context"></param>
        public override void Init(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ObjectCreationExpression);
        }

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            try
            {
                var objectCreation = context.Node as ObjectCreationExpressionSyntax;
                if (objectCreation == null)
                    return;

                var identifierName = objectCreation.Type as IdentifierNameSyntax;
                if (identifierName == null)
                    return;

                if(identifierName.Identifier.ValueText == "HtmlInputHidden")
                {
                    var pos = objectCreation.GetLocation().GetMappedLineSpan();
                    AddViolation(context.ContainingSymbol, new[] { pos });
                }
            }
            catch (Exception e)
            {
                Log.Warn(" Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath + ": " + context.Node.GetLocation().GetMappedLineSpan(), e);
            }
        }
    }
}
