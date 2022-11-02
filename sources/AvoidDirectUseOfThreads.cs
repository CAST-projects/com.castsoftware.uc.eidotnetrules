using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
      Id = "EI_AvoidDirectUseOfThreads",
      Title = "Avoid direct use of threads",
      MessageFormat = "Avoid direct use of threads",
      Category = "Secure Coding - Time and State",
      DefaultSeverity = DiagnosticSeverity.Warning,
      CastProperty = "EIDotNetQualityRules.AvoidDirectUseOfThreads"
    )]
    public class AvoidDirectUseOfThreads : AbstractRuleChecker
    {
        /// <summary>
        /// Initialize the QR with the given context and register all the syntax nodes
        /// to listen during the visit and provide a specific callback for each one
        /// </summary>
        /// <param name="context"></param>
        public override void Init(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
        }
        private void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            try
            {
                var objectCreation = context.Node as ObjectCreationExpressionSyntax;
                if (objectCreation == null)
                    return;

                var identifierName = objectCreation.Type as IdentifierNameSyntax;
                if (identifierName == null)
                    return;

                if (identifierName.Identifier.ValueText == "Thread")
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

        private void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            try
            {
                var invocationExpression = context.Node as InvocationExpressionSyntax;
                if (invocationExpression == null)
                    return;

                var memberAccessExpression = invocationExpression.Expression as MemberAccessExpressionSyntax;
                if (memberAccessExpression == null)
                    return;

                var identifierName = memberAccessExpression.Name as IdentifierNameSyntax;
                if (identifierName == null)
                    return;

                if (identifierName.Identifier.ValueText == "QueueUserWorkItem")
                {
                    var pos = invocationExpression.GetLocation().GetMappedLineSpan();
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
