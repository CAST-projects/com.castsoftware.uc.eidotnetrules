using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


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
      public ConditionalStructuresShouldNotHaveIdenticalBranches() {
      }

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         context.RegisterSyntaxNodeAction(this.AnalyzeBranches, SyntaxKind.IfStatement, SyntaxKind.SwitchStatement, SyntaxKind.ConditionalExpression);
         context.RegisterSymbolAction(this.OnMethodEnd, SymbolKind.Method);
      }

      private BlockSyntax GetBlock(dynamic blockOrExprs) {
         BlockSyntax block = null;
         if (blockOrExprs is ExpressionStatementSyntax) {
            var expr = blockOrExprs as ExpressionStatementSyntax;
            block = (BlockSyntax)SyntaxFactory.ParseStatement((null != expr ? "{" + expr + "}" : "{}"));
         }
         else {
            block = blockOrExprs as BlockSyntax;
         }
         return block;
      }

      private BlockSyntax GetBlockSyntaxIf(IfStatementSyntax ifStatement, ElseClauseSyntax elseClause) {
         var blockOrExprs = (null != ifStatement) ? ifStatement.Statement : elseClause.Statement;
         return GetBlock(blockOrExprs);
      }

      private IEnumerable<StatementSyntax> GetBlockStatements(IfStatementSyntax ifStatement, ElseClauseSyntax elseClause) {
         BlockSyntax block = GetBlockSyntaxIf(ifStatement, elseClause);
         var statements = block.WithoutTrivia().Statements.Where(statement => !(statement is EmptyStatementSyntax));
         return statements;
      }

      private bool AreStatementsEquivalent(IEnumerable<StatementSyntax> currentStatements, IEnumerable<StatementSyntax> previousStatements) {
         bool areEquivalent = true;
         int noOfcurrentStatements = currentStatements.Count();
         int noOfPreviousStatements = previousStatements.Count();

         if (noOfcurrentStatements != noOfPreviousStatements) {
            areEquivalent = false;
         }
         else {
            for (int i = 0; i < noOfcurrentStatements; ++i) {
               var currentStatement = currentStatements.ElementAt(i);
               var previousStatement = previousStatements.ElementAt(i);
               string curr = currentStatement.ToString();
               string prev = previousStatement.ToString();
               if (!currentStatement.IsEquivalentTo(previousStatement, true)) {
                  areEquivalent = false;
                  break;
               }
            }
         }
         return areEquivalent;
      }


      private Dictionary<ISymbol, HashSet<IfStatementSyntax>> _ifsAnalyzed = new Dictionary<ISymbol, HashSet<IfStatementSyntax>>();
      private void AnalyzeIfBranches(SyntaxNodeAnalysisContext context, IfStatementSyntax ifStatement) {
         HashSet<IfStatementSyntax> analyzedIfs = null;
         if (null != ifStatement && (!_ifsAnalyzed.TryGetValue(context.ContainingSymbol, out analyzedIfs) || !analyzedIfs.Contains(ifStatement))) {
            ElseClauseSyntax elseClause = null;
            bool areEquivalent = true;
            IEnumerable<StatementSyntax> currentStatements = null;
            IEnumerable<StatementSyntax> previousStatements = null;

            bool firstTime = true;
            do {
               if (null != ifStatement) {
                  if (null == analyzedIfs) {
                     analyzedIfs = new HashSet<IfStatementSyntax>();
                     _ifsAnalyzed[context.ContainingSymbol] = analyzedIfs;
                  }
                  analyzedIfs.Add(ifStatement);
               }
               bool noElse = null == elseClause && null == ifStatement.Else;
               bool ifWithNoElse = firstTime && noElse;

               if (ifWithNoElse) {
                  areEquivalent = false;
                  break;
               }
               else {
                  previousStatements = currentStatements;
                  currentStatements = GetBlockStatements(ifStatement, elseClause);
               }

               if (!firstTime) {
                  if (null == currentStatements || null == previousStatements) {
                     break;
                  }
                  areEquivalent = AreStatementsEquivalent(currentStatements, previousStatements);
                  if (!areEquivalent) {
                     break;
                  }
               }

               if (null != ifStatement && null != ifStatement.Else) {
                  elseClause = ifStatement.Else as ElseClauseSyntax;
                  ifStatement = elseClause.Statement as IfStatementSyntax;
               }
               else if (null == ifStatement) {
                  elseClause = null;
                  break;
               }

               firstTime = false;

            } while (null != ifStatement || null != elseClause);

            if (areEquivalent) {
               var pos = context.Node.SyntaxTree.GetMappedLineSpan(context.Node.Span);
               //Console.WriteLine(context.ContainingSymbol.Name + ":" + pos);
               AddViolation(context);
            }
         }
      }

      private IEnumerable<StatementSyntax> GetBlockStatements(SwitchSectionSyntax switchSectionSyntax) {
         IEnumerable<StatementSyntax> statements = null;
         if (1 == switchSectionSyntax.Statements.Count) {
            var block = switchSectionSyntax.Statements.ElementAt(0) as BlockSyntax;
            if (null != block) {
               statements = block.WithoutTrivia().Statements.Where(statement => !(statement is EmptyStatementSyntax));
            }
         }
         else {
            statements = switchSectionSyntax.Statements.Where(statement => !(statement is EmptyStatementSyntax));
         }
         
         return statements;
      }

      private void AnalyzeSwitchBranches(SyntaxNodeAnalysisContext context, SwitchStatementSyntax switchStatement) {
         if (null != switchStatement) {
            IEnumerable<StatementSyntax> currentStatements = null;
            IEnumerable<StatementSyntax> previousStatements = null;
            bool areEquivalent = false;

            
            
            var hasDefault = switchStatement.Sections.Any(section => section.Labels.Any(SyntaxKind.DefaultSwitchLabel));

            if (hasDefault) {

               foreach (var section in switchStatement.Sections) {
                  
                  previousStatements = currentStatements;
                  currentStatements = GetBlockStatements(section);
                  if (null != previousStatements) {
                     if (AreStatementsEquivalent(currentStatements, previousStatements)) {
                        areEquivalent = true;
                     }
                     else {
                        areEquivalent = false;
                        break;
                     }
                  }
               }
               if (areEquivalent) {
                  var pos = context.Node.SyntaxTree.GetMappedLineSpan(context.Node.Span);
                  //Console.WriteLine(context.ContainingSymbol.Name + ":" + pos);
                  AddViolation(context);
               }
            }
         
         }
      }

      private void AnalyzeConditionalBranches(SyntaxNodeAnalysisContext context, ConditionalExpressionSyntax conditionalExpr) {
         if (null != conditionalExpr && null != conditionalExpr.WhenFalse && null != conditionalExpr.WhenTrue) {
            if (conditionalExpr.WhenFalse.IsEquivalentTo(conditionalExpr.WhenTrue, true)) {
               var pos = context.Node.SyntaxTree.GetMappedLineSpan(context.Node.Span);
               //Console.WriteLine(context.ContainingSymbol.Name + ":" + pos);
               AddViolation(context);
            }
         }
      }

      private object _lock = new object();
      private void AnalyzeBranches(SyntaxNodeAnalysisContext context) {
         lock (_lock) {
            try {
               AnalyzeSwitchBranches(context, context.Node as SwitchStatementSyntax);
               AnalyzeConditionalBranches(context, context.Node as ConditionalExpressionSyntax);
               AnalyzeIfBranches(context, context.Node as IfStatementSyntax);
               
            }
            catch (Exception e) {
               Console.WriteLine(e.Message);
               Console.WriteLine(e.StackTrace);
            }
         }
      }

      private void OnMethodEnd(SymbolAnalysisContext context) {
         lock (_lock) {
            try {
               if (_ifsAnalyzed.Keys.Contains(context.Symbol)) {
                  _ifsAnalyzed.Remove(context.Symbol);
               }
            }
            catch (System.Exception e) {
               System.Console.WriteLine(e.Message);
               System.Console.WriteLine(e.StackTrace);
            }
         }
      }
   }
}
