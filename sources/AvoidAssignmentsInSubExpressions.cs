using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.DotNet.CastDotNetExtension;
using Roslyn.DotNet.Common;


namespace CastDotNetExtension
{
   [CastRuleChecker]
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   [RuleDescription(
       Id = "TODO: Add prefix_AvoidAssignmentsInSubExpressions",
       Title = "TODO: Add Title",
       MessageFormat = "TODO: Add Message",
       Category = "TODO: Add Category",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.AvoidAssignmentsInSubExpressions"
   )]
   public class AvoidAssignmentsInSubExpressions : AbstractRuleChecker
   {
      public AvoidAssignmentsInSubExpressions() {
      }

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         context.RegisterSyntaxNodeAction(this.Analyze, SyntaxKind.IfStatement, SyntaxKind.SwitchStatement, SyntaxKind.InvocationExpression, SyntaxKind.ObjectCreationExpression);
      }

      private bool HasAssignment(ExpressionSyntax expr, ref IEnumerable<SyntaxNode> expressions) {
         bool hasAssignment = !(expr is IdentifierNameSyntax || expr is LiteralExpressionSyntax);
         if (hasAssignment) {
            hasAssignment = expr is AssignmentExpressionSyntax;
            if (!hasAssignment) {
               var assignments = expr.DescendantNodes().Where(e => SyntaxKind.SimpleAssignmentExpression == e.Kind());
               if (null != assignments && assignments.Any()) {
                  expressions = expressions.Union(assignments);
               }
            } else {
               expressions = expressions.Append(expr);
            }
         }
         return hasAssignment;
      }

      private bool HasAssignmentsInArguments(ArgumentListSyntax argumentList, ref IEnumerable<SyntaxNode> expressions) {
         bool hasAssignment = false;
         if (null != argumentList && argumentList.Arguments.Any()) {
            foreach (var argument in argumentList.Arguments) {
               hasAssignment = HasAssignment(argument.Expression, ref expressions);
            }
         }
         return hasAssignment;
      }

      private object _lock = new object();

      private void Analyze(SyntaxNodeAnalysisContext context) {
         lock (_lock) {
            try {
               IEnumerable<SyntaxNode> expressions = new List<ExpressionSyntax>();
               if (context.Node is IfStatementSyntax) {
                  var exprToCheck = (context.Node as IfStatementSyntax).Condition;
                  HasAssignment(exprToCheck, ref expressions);
               } else if (context.Node is SwitchStatementSyntax) {
                  var exprToCheck = (context.Node as SwitchStatementSyntax).Expression;
                  HasAssignment(exprToCheck, ref expressions);
               } else if (context.Node is InvocationExpressionSyntax) {
                  var invocation = context.Node as InvocationExpressionSyntax;
                  HasAssignmentsInArguments(invocation.ArgumentList, ref expressions);
               } else if (context.Node is ObjectCreationExpressionSyntax) {
                  var objCreation = context.Node as ObjectCreationExpressionSyntax;
                  HasAssignmentsInArguments(objCreation.ArgumentList, ref expressions);
               }
               if (expressions.Any()) {
                  foreach (var expr in expressions) {
                     var pos = expr.GetLocation().GetMappedLineSpan();
                     //Console.WriteLine(context.ContainingSymbol + ": " + expr + ": " + pos);
                     AddViolation(context.ContainingSymbol, new FileLinePositionSpan[] { pos });
                  }
               }
            } catch (Exception e) {
               Log.Warn("Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath + ": " + context.Node.GetLocation().GetMappedLineSpan(), e);
            }
         }
      }
   }
}
