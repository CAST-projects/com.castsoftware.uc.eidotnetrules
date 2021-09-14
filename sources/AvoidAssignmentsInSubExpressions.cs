using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.DotNet.CastDotNetExtension;


namespace CastDotNetExtension
{
   [CastRuleChecker]
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   [RuleDescription(
       Id = "EI_AvoidAssignmentsInSubExpressions",
       Title = "Avoid assignments in sub-expressions",
       MessageFormat = "Avoid assignments in sub-expressions",
       Category = "Programming Practices - Modularity and OO Encapsulation Conformity",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.AvoidAssignmentsInSubExpressions"
   )]
   public class AvoidAssignmentsInSubExpressions : AbstractRuleChecker
   {

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.IfStatement, SyntaxKind.SwitchStatement, SyntaxKind.InvocationExpression, SyntaxKind.ObjectCreationExpression);
      }

      private static void AddIfAssignment(ExpressionSyntax expr, ref IEnumerable<SyntaxNode> expressions)
      {
         bool hasAssignment = !(expr is IdentifierNameSyntax || expr is LiteralExpressionSyntax);
         if (hasAssignment) {
            hasAssignment = expr is AssignmentExpressionSyntax;
            if (!hasAssignment) {
               var assignments = expr.DescendantNodes().Where(e => SyntaxKind.SimpleAssignmentExpression == e.Kind() &&
                                                                   SyntaxKind.ObjectInitializerExpression != e.Parent.Kind());
               expressions = expressions.Union(assignments);
            } else {
               expressions = expressions.Append(expr);
            }
         }
      }

      private static readonly HashSet<SyntaxKind> ExcludedArgumentTypes = new HashSet<SyntaxKind> {
         SyntaxKind.SimpleLambdaExpression,
         SyntaxKind.ParenthesizedLambdaExpression,
         SyntaxKind.AnonymousMethodExpression,
      };
      private static void AddIfHasAssignmentsInArguments(ArgumentListSyntax argumentList, ref IEnumerable<SyntaxNode> expressions) {
         if (null != argumentList && argumentList.Arguments.Any()) {
            foreach (var argument in argumentList.Arguments) {
               if (!ExcludedArgumentTypes.Contains(argument.Expression.Kind())) {
                  AddIfAssignment(argument.Expression, ref expressions);
               }
            }
         }
      }

      private readonly object _lock = new object();

      private void Analyze(SyntaxNodeAnalysisContext context) {
          Log.InfoFormat("Run registered callback for rule: {0}", GetRuleName());
         /*lock (_lock)*/ {
            try {
               IEnumerable<SyntaxNode> expressions = new List<ExpressionSyntax>();
               switch (context.Node.Kind())
               {
                  case SyntaxKind.IfStatement:
                  {
                     var exprToCheck = (context.Node as IfStatementSyntax).Condition;
                     AddIfAssignment(exprToCheck, ref expressions);
                     break;
                  }
                  case SyntaxKind.SwitchStatement:
                  {
                     var exprToCheck = (context.Node as SwitchStatementSyntax).Expression;
                     AddIfAssignment(exprToCheck, ref expressions);
                     break;
                  }
                  case SyntaxKind.InvocationExpression:
                  {
                     var invocation = context.Node as InvocationExpressionSyntax;
                     AddIfHasAssignmentsInArguments(invocation.ArgumentList, ref expressions);
                     break;
                  }
                  case SyntaxKind.ObjectCreationExpression:
                  {
                     var objCreation = context.Node as ObjectCreationExpressionSyntax;
                     AddIfHasAssignmentsInArguments(objCreation.ArgumentList, ref expressions);
                     break;
                  }
               }

               if (expressions.Any()) {
                  foreach (var expr in expressions) {
                     var pos = expr.GetLocation().GetMappedLineSpan();
                     //Console.WriteLine(context.ContainingSymbol + ": " + expr + ": " + pos);
                     AddViolation(context.ContainingSymbol, new[] { pos });
                  }
               }
            } catch (Exception e) {
               Log.Warn(" Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath + ": " + context.Node.GetLocation().GetMappedLineSpan(), e);
            }
         }
         Log.InfoFormat("END Run registered callback for rule: {0}", GetRuleName());
      }
   }
}
