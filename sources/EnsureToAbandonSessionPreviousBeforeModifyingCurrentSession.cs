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
       Id = "EI_EnsureToAbandonSessionPreviousBeforeModifyingCurrentSession",
       Title = "Ensure to abandon previous session before modifying current session",
       MessageFormat = "Ensure to abandon previous session before modifying current session",
       Category = "Secure Coding - Input Validation",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.EnsureToAbandonSessionPreviousBeforeModifyingCurrentSession"
   )]
    public class EnsureToAbandonSessionPreviousBeforeModifyingCurrentSession : AbstractRuleChecker
    {
        /// <summary>
        /// Initialize the QR with the given context and register all the syntax nodes
        /// to listen during the visit and provide a specific callback for each one
        /// </summary>
        /// <param name="context"></param>
        public override void Init(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.SimpleAssignmentExpression);
        }

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            try
            {
                var node = context.Node as AssignmentExpressionSyntax;
                if (node == null)
                    return;

                var leftNode = node.Left as ElementAccessExpressionSyntax;
                if (leftNode == null)
                    return;

                var sessionStateSymbol = context.Compilation.GetTypeByMetadataName("System.Web.SessionState.HttpSessionState") as INamedTypeSymbol;
                if (sessionStateSymbol == null)
                    return;

                HashSet<IMethodSymbol> methods =
                context.Compilation.GetMethodSymbolsForSystemClass(sessionStateSymbol, 
                    new HashSet<string>() { "Abandon" }, false, 1);
                IMethodSymbol abandonSymbol = methods.FirstOrDefault();

                var expressionSymbolType = context.SemanticModel.GetTypeInfo(leftNode.Expression).Type as INamedTypeSymbol;
                if (expressionSymbolType == null)
                    return;

                if (!SymbolEqualityComparer.Default.Equals(sessionStateSymbol, expressionSymbolType))
                    return;

                var containingSymbol = context.ContainingSymbol;
                var parent = containingSymbol.DeclaringSyntaxReferences.FirstOrDefault();
                if (parent == null)
                    return;

                var descendantNodes = parent.GetSyntax().DescendantNodes().OfType<InvocationExpressionSyntax>();
                bool hasAbandonned = false;
                foreach(var invocNode in descendantNodes)
                {
                    var expressionSymbol = context.SemanticModel.GetSymbolInfo(invocNode.Expression).Symbol;
                    if(expressionSymbol != null && SymbolEqualityComparer.Default.Equals(expressionSymbol, abandonSymbol))
                    {
                        hasAbandonned = true;
                        break;
                    }
                }

                if(!hasAbandonned)
                {
                    var pos = node.GetLocation().GetMappedLineSpan();
                    AddViolation(context.ContainingSymbol, new[] { pos });
                }

                //var objectCreation = context.Node as ObjectCreationExpressionSyntax;
                //if (objectCreation == null)
                //    return;

                //var identifierName = objectCreation.Type as IdentifierNameSyntax;
                //if (identifierName == null)
                //    return;

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
