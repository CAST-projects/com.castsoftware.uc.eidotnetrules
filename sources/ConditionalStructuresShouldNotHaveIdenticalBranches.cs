using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO;


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
   public class ConditionalStructuresShouldNotHaveIdenticalBranches : AbstractRuleChecker /*, IDisposable*/ {
      //private FileStream _log = null;
      public ConditionalStructuresShouldNotHaveIdenticalBranches() {
         //_log = new FileStream(@"C:\Temp\ConditionalStructuresShouldNotHaveIdenticalBranches.log", FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
      }

      //void IDisposable.Dispose()
      //{
      //   _log.Close();
      //}

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         context.RegisterSyntaxNodeAction(this.AnalyzeBranches, SyntaxKind.ConditionalExpression);
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

      private List<StatementSyntax> GetStatementsNoTriviaOrEmptyStatements(BlockSyntax block, ref List<SyntaxKind> syntaxKindsIn)
      {
         List<StatementSyntax> statements = GetStatementsNoEmptyStatements(block.WithoutTrivia().Statements, ref syntaxKindsIn);

         return statements;
      }

      private List<StatementSyntax> GetStatementsNoEmptyStatements(SyntaxList<StatementSyntax> statementsIn, ref List<SyntaxKind> syntaxKindsIn)
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

         return (areEquivalent ? statements : null);
      }


      private bool AreStatementsEquivalent(IfStatementSyntax ifStatement, ElseClauseSyntax elseClause, bool firstTime, ref List<StatementSyntax> currentStatements, ref List<SyntaxKind> syntaxKindCount)
      {
         BlockSyntax block = GetBlockSyntaxIf(ifStatement, elseClause);
         
         List<StatementSyntax> statements = GetStatementsNoTriviaOrEmptyStatements(block, ref syntaxKindCount);

         bool areEquivalent = (null != statements);
         if (!firstTime && areEquivalent) {
            areEquivalent = AreStatementsEquivalent(statements, currentStatements);
         }

         if (null == currentStatements) {
            currentStatements = statements;
         }
         
         return areEquivalent;
      }

      private bool AreStatementsEquivalent(List<StatementSyntax> currentStatements, List<StatementSyntax> previousStatements)
      {
         bool areEquivalent = (null != currentStatements && null != previousStatements 
            && currentStatements.Count == previousStatements.Count);
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
         var pos = ifStatement.SyntaxTree.GetMappedLineSpan(ifStatement.Span);
         if (null != ifStatement && !analyzedIfs.Contains(ifStatement)) {
            //var bytesEnter = new UTF8Encoding(true).GetBytes("AnalyzeIfBranches: " + iSymbol.OriginalDefinition.ToString() + ": " + pos + "\r\n");
            //_log.Write(bytesEnter, 0, bytesEnter.Length);
            //_log.Flush();

            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            ElseClauseSyntax elseClause = null;
            bool areEquivalent = true;
            List<StatementSyntax> currentStatements = null;
            List<StatementSyntax> previousStatements = null;
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
               } else {
                  previousStatements = currentStatements;
                  areEquivalent = AreStatementsEquivalent(ifStatement, elseClause, firstTime, ref currentStatements, ref syntaxKinds);
                  if (!areEquivalent) {
                     break;
                  }
               }

               if (null != ifStatement && null != ifStatement.Else) {
                  elseClause = ifStatement.Else as ElseClauseSyntax;
                  ifStatement = elseClause.Statement as IfStatementSyntax;
               } else if (null == ifStatement || null == ifStatement.Else) {
                  elseClause = null;
                  break;
               }

               firstTime = false;

            } while (null != ifStatement || null != elseClause);

            if (areEquivalent) {
               
               //Console.WriteLine(iSymbol.Name + ":" + pos);
               AddViolation(iSymbol, new FileLinePositionSpan [] {pos});
            }
            //watch.Stop();
            //var bytes = new UTF8Encoding(true).GetBytes("AnalyzeIfBranches: " + iSymbol.OriginalDefinition.ToString() + ": " + watch.ElapsedMilliseconds + ": " + ((null != syntaxKinds) ? syntaxKinds.Count : 0) + "\r\n");
            //_log.Write(bytes, 0, bytes.Length);
            //_log.Flush();

         }

      }

      private List<StatementSyntax> GetBlockStatements(SwitchSectionSyntax switchSectionSyntax, ref List<SyntaxKind> syntaxKindsIn) {
         List<StatementSyntax> statements = null;
         if (1 == switchSectionSyntax.Statements.Count) {
            var block = switchSectionSyntax.Statements.ElementAt(0) as BlockSyntax;
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
            //var bytesEnter = new UTF8Encoding(true).GetBytes("AnalyzeSwitchBranches: " + iSymbol.OriginalDefinition.ToString() + "\r\n");
            //_log.Write(bytesEnter, 0, bytesEnter.Length);
            //_log.Flush();

            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            var pos = switchStatement.SyntaxTree.GetMappedLineSpan(switchStatement.Span);

            List<StatementSyntax> currentStatements = null;
            List<StatementSyntax> previousStatements = null;
            bool areEquivalent = false;

            List<SyntaxKind> syntaxKinds = null;

            if (1 < switchStatement.Sections.Count && switchStatement.Sections.Any(section => section.Labels.Any(SyntaxKind.DefaultSwitchLabel))) {
               foreach (var section in switchStatement.Sections) {

                  previousStatements = currentStatements;
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

               //watch.Stop();
               //var bytes = new UTF8Encoding(true).GetBytes("AnalyzeSwitchBranches: " + iSymbol.OriginalDefinition.ToString() + ": " + watch.ElapsedMilliseconds + ": " + ((null != syntaxKinds) ? syntaxKinds.Count : 0) + "\r\n");
               //_log.Write(bytes, 0, bytes.Length);
               //_log.Flush();

               if (areEquivalent) {
                  
                  //Console.WriteLine(context.ContainingSymbol.Name + ":" + pos);
                  AddViolation(iSymbol, new FileLinePositionSpan [] {pos});
               }
            }

         }

      }

      private void AnalyzeSwitchBranches(SyntaxNodeAnalysisContext context, SwitchStatementSyntax switchStatement) {
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
               AnalyzeConditionalBranches(context, context.Node as ConditionalExpressionSyntax);
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
               HashSet<IfStatementSyntax> ifsAnalyzed = new HashSet<IfStatementSyntax>();
               IMethodSymbol iMethod = (context.Symbol as IMethodSymbol);
               if (null != iMethod && null != iMethod.DeclaringSyntaxReferences) {
                  foreach (var root in iMethod.DeclaringSyntaxReferences) {
                     var syntax = root.GetSyntax();
                     var descendentNodes = syntax.DescendantNodes();
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
            catch (System.Exception e) {
               System.Console.WriteLine(e.Message);
               System.Console.WriteLine(e.StackTrace);
            }
         }
      }
   }
}
 