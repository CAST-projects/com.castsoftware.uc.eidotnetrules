using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.DotNet.CastDotNetExtension;


namespace CastDotNetExtension {
   [CastRuleChecker]
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   [RuleDescription(
       Id = "EI_ConditionalStructuresShouldNotHaveIdenticalBranches",
       Title = "Conditional structures should not have identical branches",
       MessageFormat = "Conditional structures should not have identical branches",
       Category = "Programming Practices - Structuredness",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.ConditionalStructuresShouldNotHaveIdenticalBranches"
   )]
   public class ConditionalStructuresShouldNotHaveIdenticalBranches : AbstractRuleChecker {

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         context.RegisterSyntaxNodeAction(AnalyzeBranches, SyntaxKind.ConditionalExpression);
         context.RegisterSymbolAction(OnMethodEnd, SymbolKind.Method);
      }

      private static BlockSyntax GetBlock(dynamic blockOrExprs)
      {
         BlockSyntax block = null;
         if (blockOrExprs is ExpressionStatementSyntax) {
            var expr = blockOrExprs as ExpressionStatementSyntax;
            block = (BlockSyntax)SyntaxFactory.ParseStatement("{" + expr + "}");
         } else if (blockOrExprs is ReturnStatementSyntax) {
            var expr = blockOrExprs as ReturnStatementSyntax;
            block = (BlockSyntax)SyntaxFactory.ParseStatement("{" + expr + "}");
         }
         else {
            block = blockOrExprs as BlockSyntax;
         }
         return block;
      }

      private static BlockSyntax GetBlockSyntaxIf(IfStatementSyntax ifStatement, ElseClauseSyntax elseClause)
      {
         var blockOrExprs = null != ifStatement ? ifStatement.Statement : elseClause.Statement;
         return GetBlock(blockOrExprs);
      }

      private static List<StatementSyntax> GetStatementsNoTriviaOrEmptyStatements(BlockSyntax block, ref List<SyntaxKind> syntaxKindsIn)
      {
         if (null != block) {
            List<StatementSyntax> statements = GetStatementsNoEmptyStatements(block.WithoutTrivia().Statements, ref syntaxKindsIn);

            return statements;
         }

         return new List<StatementSyntax>();
      }

      private static List<StatementSyntax> GetStatementsNoEmptyStatements(SyntaxList<StatementSyntax> statementsIn, ref List<SyntaxKind> syntaxKindsIn)
      {
         List<StatementSyntax> statements = new List<StatementSyntax>();
         List<SyntaxKind> syntaxKinds = new List<SyntaxKind>();
         bool areEquivalent = true;
         int index = 0;
         foreach (var statement in statementsIn) {
            if (!(statement is EmptyStatementSyntax)) {
               statements.Add(statement);
               
               if (null != syntaxKindsIn && (syntaxKindsIn.Count == index || syntaxKindsIn[index] != statement.Kind())) {
                  areEquivalent = false;
                  break;
               }
               syntaxKinds.Add(statement.Kind());
               index++;
            }
         }


         if (null == syntaxKindsIn) {
            syntaxKindsIn = syntaxKinds;
         }

         return areEquivalent ? statements : null;
      }


      private bool AreStatementsEquivalent(IfStatementSyntax ifStatement, ElseClauseSyntax elseClause, bool firstTime, ref List<StatementSyntax> currentStatements, ref List<SyntaxKind> syntaxKindCount)
      {
         BlockSyntax block = GetBlockSyntaxIf(ifStatement, elseClause);
         
         List<StatementSyntax> statements = GetStatementsNoTriviaOrEmptyStatements(block, ref syntaxKindCount);

         bool areEquivalent = null != statements;
         if (!firstTime && areEquivalent) {
            areEquivalent = AreStatementsEquivalent(statements, currentStatements);
         }

         if (null == currentStatements) {
            currentStatements = statements;
         }
         
         return areEquivalent;
      }

      private static bool AreStatementsEquivalent(List<StatementSyntax> currentStatements, List<StatementSyntax> previousStatements)
      {
         bool areEquivalent = null != currentStatements && null != previousStatements 
                                                        && currentStatements.Count == previousStatements.Count;
         if (areEquivalent) {
            for (int i = 0; i < currentStatements.Count; ++i) {
               var current = currentStatements.ElementAt(i);
               var previous = previousStatements.ElementAt(i);
               if (current.Kind() != previous.Kind() || !current.IsEquivalentTo(previous, true)) {
                  areEquivalent = false;
                  break;
               }
            }
         }
         return areEquivalent;
      }

      
      private void AnalyzeIfBranches(IfStatementSyntax ifStatement, ISymbol iSymbol, ref HashSet<IfStatementSyntax> analyzedIfs) {
         
         if (null != ifStatement && !analyzedIfs.Contains(ifStatement)) {
            var pos = ifStatement.SyntaxTree.GetMappedLineSpan(ifStatement.Span);
            ElseClauseSyntax elseClause = null;
            bool areEquivalent = true;
            List<StatementSyntax> currentStatements = null;
            List<SyntaxKind> syntaxKinds = null;
            bool firstTime = true;
            do {
               if (null != ifStatement) {
                  analyzedIfs.Add(ifStatement);
               }
               bool noElse = null == elseClause && null == ifStatement.Else;
               bool ifWithNoElse = firstTime && noElse;

               if (ifWithNoElse) {
                  areEquivalent = false;
                  break;
               }

               areEquivalent = AreStatementsEquivalent(ifStatement, elseClause, firstTime, ref currentStatements, ref syntaxKinds);
               if (!areEquivalent) {
                  break;
               }

               if (null != ifStatement && null != ifStatement.Else) {
                  elseClause = ifStatement.Else as ElseClauseSyntax;
                  ifStatement = elseClause.Statement as IfStatementSyntax;
               } else if (null == ifStatement || null == ifStatement.Else) {
                  break;
               }

               firstTime = false;

            } while (null != ifStatement || null != elseClause);

            if (areEquivalent) {
               AddViolation(iSymbol, new FileLinePositionSpan [] {pos});
            }
         }
      }

      private List<StatementSyntax> GetBlockStatements(SwitchSectionSyntax switchSectionSyntax, ref List<SyntaxKind> syntaxKindsIn) {
         List<StatementSyntax> statements = null;
         if (1 == switchSectionSyntax.Statements.Count) {
            var block = switchSectionSyntax.Statements.ElementAt(0) as BlockSyntax;
            if (null == block) {
               if (switchSectionSyntax.Statements.ElementAt(0) is ReturnStatementSyntax) {
                  var statement = switchSectionSyntax.Statements.ElementAt(0);
                  block = (BlockSyntax)SyntaxFactory.ParseStatement(null != statement ? "{" + statement + "}" : "{}");
               }
            }

            if (null != block) {
               statements = GetStatementsNoTriviaOrEmptyStatements(block, ref syntaxKindsIn);
            }
         }
         else {
            statements = GetStatementsNoEmptyStatements(switchSectionSyntax.Statements, ref syntaxKindsIn);
         }
         
         return statements;
      }

      private void AnalyzeSwitchBranches(SwitchStatementSyntax switchStatement, ISymbol iSymbol) {
         if (null != switchStatement) {
            
            var pos = switchStatement.SyntaxTree.GetMappedLineSpan(switchStatement.Span);

            bool areEquivalent = false;

            List<SyntaxKind> syntaxKinds = null;

            if (1 < switchStatement.Sections.Count && switchStatement.Sections.Any(section => section.Labels.Any(SyntaxKind.DefaultSwitchLabel))) {
               List<StatementSyntax> currentStatements = null;

               foreach (var section in switchStatement.Sections) {

                  var previousStatements = currentStatements;
                  currentStatements = GetBlockStatements(section, ref syntaxKinds);
                  if (null != previousStatements) {
                     if (null != currentStatements && AreStatementsEquivalent(currentStatements, previousStatements)) {
                        areEquivalent = true;
                     } else {
                        areEquivalent = false;
                        break;
                     }
                  }
               }

               if (areEquivalent) {
                  //Console.WriteLine(context.ContainingSymbol.Name + ":" + pos);
                  AddViolation(iSymbol, new FileLinePositionSpan [] {pos});
               }
            }

         }

      }

      private void AnalyzeConditionalBranches(SyntaxNodeAnalysisContext context, ConditionalExpressionSyntax conditionalExpr) {
         if (null != conditionalExpr && null != conditionalExpr.WhenFalse && null != conditionalExpr.WhenTrue) {
            if (conditionalExpr.WhenFalse.IsEquivalentTo(conditionalExpr.WhenTrue, true)) {
               //var pos = context.Node.SyntaxTree.GetMappedLineSpan(context.Node.Span);
               //Log.WarnFormat("{0}: {1}", context.ContainingSymbol.Name, pos);
               AddViolation(context);
            }
         }
      }

      private readonly object _lock = new object();
      private void AnalyzeBranches(SyntaxNodeAnalysisContext context) {
         lock (_lock) {
            try {
               AnalyzeConditionalBranches(context, context.Node as ConditionalExpressionSyntax);
            }
            catch (Exception e) {
               Log.Warn("Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath + ": " + context.Node.GetLocation().GetMappedLineSpan(), e);
            }
         }
      }

      private void OnMethodEnd(SymbolAnalysisContext context) {
         lock (_lock) {
            try {
               HashSet<IfStatementSyntax> ifsAnalyzed = new HashSet<IfStatementSyntax>();
               IMethodSymbol iMethod = context.Symbol as IMethodSymbol;
               if (null != iMethod && null != iMethod.DeclaringSyntaxReferences) {
                  foreach (var root in iMethod.DeclaringSyntaxReferences) {
                     var syntax = root.GetSyntax();
                     var statements = syntax.DescendantNodes().Where(s => s is IfStatementSyntax || s is SwitchStatementSyntax);
                     
                     foreach (var statement in statements) {
                        if (statement is IfStatementSyntax) {
                           AnalyzeIfBranches(statement as IfStatementSyntax, context.Symbol, ref ifsAnalyzed);
                        } else {
                           AnalyzeSwitchBranches(statement as SwitchStatementSyntax, context.Symbol);
                        }
                     
                     }
                  }
               }
            }
            catch (Exception e) {
               HashSet<string> filePaths = new HashSet<string>();
               foreach (var synRef in context.Symbol.DeclaringSyntaxReferences) {
                  filePaths.Add(synRef.SyntaxTree.FilePath);
               }
               Log.Warn("Exception while analyzing " + string.Join(",", filePaths) + ": " + context.Symbol.Locations.FirstOrDefault().GetMappedLineSpan(), e);
            }
         }
      }
   }
}
 