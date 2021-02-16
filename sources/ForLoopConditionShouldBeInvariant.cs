using System;
using System.Collections.Generic;
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
       Id = "EI_ForLoopConditionShouldBeInvariant",
       Title = "For Loop Condition Should Be Invariant",
       MessageFormat = "For Loop Condition Should Be Invariant",
       Category = "Programming Practices - Unexpected Behavior",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.ForLoopConditionShouldBeInvariant"
   )]
   public class ForLoopConditionShouldBeInvariant : AbstractRuleChecker
   {


      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         //TODO: register for events
         context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ForStatement);
      }

      private IList<SyntaxNode> GetViolatingSyntaxNodes(SyntaxNodeAnalysisContext context, StatementSyntax forLoopBlock, IdentifierNameSyntax left, IdentifierNameSyntax right) {
         IList<SyntaxNode> violatingExprs = new List<SyntaxNode>();
         if (null != left && null != forLoopBlock) {
            ISymbol iSymbolLeft = null, iSymbolRight = null;
            bool triedLeft = false, triedRight = false;
            var descendentNodes = forLoopBlock.DescendantNodes();
            foreach (var node in descendentNodes) {
               if (node is IdentifierNameSyntax) {
                  var identifierNode = node as IdentifierNameSyntax;
                  if ((null != left && identifierNode.Identifier.Value == left.Identifier.Value) ||
                     (null != right && identifierNode.Identifier.Value == right.Identifier.Value)) {

                     if (identifierNode.Identifier.Value == left.Identifier.Value && null == iSymbolLeft && null != left) {
                        iSymbolLeft = context.SemanticModel.GetSymbolInfo(left).Symbol;
                        if (null == iSymbolLeft) {
                           triedLeft = true;
                        }
                     } else if (null == iSymbolRight && null != right) {
                        iSymbolRight = context.SemanticModel.GetSymbolInfo(right).Symbol;
                        if (null == iSymbolRight) {
                           triedRight = true;
                        }
                     }

                     if (triedLeft && triedRight && null == iSymbolLeft && null == iSymbolRight) {
                        return violatingExprs;
                     }

                     var varSymbol = context.SemanticModel.GetSymbolInfo(identifierNode).Symbol;
                     if (null != varSymbol && (varSymbol == iSymbolLeft || varSymbol == iSymbolRight)) {
                        bool addViolation = false;
                        if (identifierNode.Parent is PostfixUnaryExpressionSyntax) {
                           addViolation = true;
                        } else if (identifierNode.Parent is PrefixUnaryExpressionSyntax) {
                           addViolation = true;
                        } else if (identifierNode.Parent is AssignmentExpressionSyntax) {
                           var assignmentExpr = identifierNode.Parent as AssignmentExpressionSyntax;
                           if (assignmentExpr.Left == identifierNode) {
                              addViolation = true;
                           }
                        } else if (identifierNode.Parent is ArgumentSyntax) {
                           var argumentExpr = identifierNode.Parent as ArgumentSyntax;
                           if (!argumentExpr.RefOrOutKeyword.IsKind(SyntaxKind.None)) {
                              addViolation = true;
                           }
                        }
                        if (addViolation) {
                           violatingExprs.Add(identifierNode.Parent);
                        }
                     }
                  }
               }
            }
         }

         return violatingExprs;
      }

      private void Analyze(SyntaxNodeAnalysisContext context) {
         try {
            var forLoop = context.Node as ForStatementSyntax;
            if (forLoop.Condition is BinaryExpressionSyntax) {
               var binaryExpr = forLoop.Condition as BinaryExpressionSyntax;
               IdentifierNameSyntax left = null, right = null;
               if (binaryExpr.Left is IdentifierNameSyntax) {
                  left = binaryExpr.Left as IdentifierNameSyntax;
               }
               if (binaryExpr.Right is IdentifierNameSyntax) {
                  right = binaryExpr.Right as IdentifierNameSyntax;
               }
               if (null != left || right != null) {
                  var violatingSyntaxNodes = GetViolatingSyntaxNodes(context, forLoop.Statement, left, right);
                  foreach (var violatingSyntaxNode in violatingSyntaxNodes) {
                     var pos = violatingSyntaxNode.GetLocation().GetMappedLineSpan();
                     //Console.WriteLine(context.ContainingSymbol + ": " + pos);
                     AddViolation(context.ContainingSymbol, new FileLinePositionSpan[] { pos });
                  }
               }

            }
         } catch (Exception e) {
            Log.Warn("Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath + ": " + context.Node.GetLocation().GetMappedLineSpan(), e);
         }
      }
   }
}
