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
       Id = "EI_MergeAdjacentTryBlocksWithIdenticalCatchFinallyStatements",
       Title = "Merge adjacent try blocks with identical catch/finally statements",
       MessageFormat = "Merge adjacent try blocks with identical catch/finally statements",
       Category = "Programming Practices - Error and Exception Handling",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.MergeAdjacentTryBlocksWithIdenticalCatchFinallyStatements"
   )]
   public class MergeAdjacentTryBlocksWithIdenticalCatchFinallyStatements : AbstractRuleChecker
   {
      public MergeAdjacentTryBlocksWithIdenticalCatchFinallyStatements() :
         base(ViolationCreationMode.ViolationWithAdditionalBookmarks) {
      }

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         context.RegisterSymbolAction(this.Analyze, SymbolKind.Method);
      }

      private class FinallyCatchBlocksMatcher : CSharpSyntaxWalker
      {
         private int _nestingLevel = 0;
         private Dictionary<int, SyntaxNode> _tryStatementsWithNestingLevel = new Dictionary<int, SyntaxNode>();
         private Dictionary<SyntaxNode, List<SyntaxNode>> _tryStatementsWithEquivalentCatchFinallyBlocks = new Dictionary<SyntaxNode, List<SyntaxNode>>();
         public Dictionary<SyntaxNode, List<SyntaxNode>> TryStatementsWithEquivalentCatchFinallyBlocks {
            get {
               return _tryStatementsWithEquivalentCatchFinallyBlocks;
            }
         }
         
         public override void Visit(SyntaxNode node) {
            if (SyntaxKind.TryStatement == node.Kind()) {
               _nestingLevel++;
               SyntaxNode tryStatementNode = null;
               if (_tryStatementsWithNestingLevel.TryGetValue(_nestingLevel, out tryStatementNode)) {
                  TryStatementSyntax currTryStatement = node as TryStatementSyntax;
                  TryStatementSyntax prevTryStatement = tryStatementNode as TryStatementSyntax;
                  if (null != currTryStatement && null != prevTryStatement) {
                     if (currTryStatement.Catches.Count == prevTryStatement.Catches.Count &&
                        (null != currTryStatement.Finally) == (null != prevTryStatement.Finally)) {
                        bool areEquivalent = true;
                        if (null != currTryStatement.Finally &&
                           !currTryStatement.Finally.Block.IsEquivalentTo(prevTryStatement.Finally.Block)) {
                           areEquivalent = false;
                        }

                        if (areEquivalent) {
                           Dictionary<CatchClauseSyntax, CatchClauseSyntax> curr2PrevCatchBlocks = new Dictionary<CatchClauseSyntax, CatchClauseSyntax>();
                           foreach (var currCatchBlock in currTryStatement.Catches) {
                              CatchClauseSyntax prevCatchClause = null;
                              foreach (var aPrevCatchClause in prevTryStatement.Catches) {
                                 if (aPrevCatchClause.Declaration.Type.ToString() == currCatchBlock.Declaration.Type.ToString()) {
                                    prevCatchClause = aPrevCatchClause;
                                    break;
                                 }
                              }
                              if (null == prevCatchClause) {
                                 curr2PrevCatchBlocks.Clear();
                                 areEquivalent = false;
                                 break;
                              }
                              curr2PrevCatchBlocks[currCatchBlock] = prevCatchClause;
                           }

                           if (currTryStatement.Catches.Any()) {
                              if (curr2PrevCatchBlocks.Any()) {
                                 foreach (var currCatch in curr2PrevCatchBlocks.Keys) {
                                    if (!currCatch.Block.IsEquivalentTo(curr2PrevCatchBlocks[currCatch].Block)) {
                                       areEquivalent = false;
                                       curr2PrevCatchBlocks.Clear();
                                       break;
                                    }
                                 }
                              } else {
                                 areEquivalent = false;
                              }
                           }
                        }
                        
                        if (areEquivalent) {
                           List<SyntaxNode> equivalentOnes = null;
                           if (!_tryStatementsWithEquivalentCatchFinallyBlocks.TryGetValue(prevTryStatement, out equivalentOnes)) {
                              equivalentOnes = new List<SyntaxNode>();
                              _tryStatementsWithEquivalentCatchFinallyBlocks[prevTryStatement] = equivalentOnes;
                           }
                           equivalentOnes.Add(node);
                        } else {
                           _tryStatementsWithNestingLevel[_nestingLevel] = node;
                        }
                     } else {
                        _tryStatementsWithNestingLevel[_nestingLevel] = node;
                     }
                  }
               } else {
                  _tryStatementsWithNestingLevel[_nestingLevel] = node;
               }
            }

            base.Visit(node);

            if (SyntaxKind.TryStatement == node.Kind()) {
               _nestingLevel--;
            }
         }
      }

      private object _lock = new object();
      private void Analyze(SymbolAnalysisContext context) {
         lock (_lock) {
            IMethodSymbol iMethod = context.Symbol as IMethodSymbol;
            if (null != iMethod) {
               foreach (var syntaxRef in iMethod.DeclaringSyntaxReferences) {
                  if (syntaxRef.GetSyntax().DescendantNodes().Any(s => SyntaxKind.TryStatement == s.Kind())) {
                     var walker = new FinallyCatchBlocksMatcher();
                     walker.Visit(syntaxRef.GetSyntax());

                     foreach (var firstTry in walker.TryStatementsWithEquivalentCatchFinallyBlocks.Keys) {
                        var lastTry = walker.TryStatementsWithEquivalentCatchFinallyBlocks[firstTry].Last() as TryStatementSyntax;
                        SyntaxNode endBlock = null != lastTry.Finally ? (SyntaxNode)lastTry.Finally : (SyntaxNode)lastTry.Catches.Last();
                        var pos = new FileLinePositionSpan(firstTry.SyntaxTree.FilePath,
                           firstTry.GetLocation().GetLineSpan().StartLinePosition,
                           endBlock.GetLocation().GetLineSpan().EndLinePosition);
                        //Console.WriteLine(context.Symbol + ": " + pos);
                        AddViolation(context.Symbol, new FileLinePositionSpan[] { pos });

                     }
                  }
               }
            }
         }
      }
   }
}
