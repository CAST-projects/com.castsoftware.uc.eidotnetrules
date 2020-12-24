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
       Id = "EI_EmptyArraysAndCollectionsShouldBeReturnedInsteadOfNull",
       Title = "Empty arrays and collections should be returned instead of null",
       MessageFormat = "Empty arrays and collections should be returned instead of null",
       Category = "Programming Practices",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "DotNetQualityRules.EmptyArraysAndCollectionsShouldBeReturnedInsteadOfNull"
   )]
   public class EmptyArraysAndCollectionsShouldBeReturnedInsteadOfNull : AbstractRuleChecker {
      public EmptyArraysAndCollectionsShouldBeReturnedInsteadOfNull() {
      }

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         context.RegisterSymbolAction(this.Analyze, SymbolKind.Method, SymbolKind.Property);
      }

      private Object _lock = new Object();
      private void Analyze(SymbolAnalysisContext context) {
         lock (_lock) {
            var method = context.Symbol as IMethodSymbol;
            
            
            if (null != method) {
               if (TypeKind.Array == method.ReturnType.TypeKind ||
                  method.ReturnType.OriginalDefinition.ToString().StartsWith("System.Collections.Generic.")) {
                  var syntaxRefs = method.DeclaringSyntaxReferences;
                  foreach (var syntaxRef in syntaxRefs) {
                     SyntaxNode syntaxNode = syntaxRef.GetSyntaxAsync(context.CancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();
                     var returnStatements = syntaxNode.DescendantNodes().OfType<ReturnStatementSyntax>();

                     if (null != returnStatements) {
                        List<FileLinePositionSpan> violations = new List<FileLinePositionSpan>();
                        foreach (var returnStatement in returnStatements) {
                           var retVal = returnStatement.Expression as LiteralExpressionSyntax;
                           if (null != retVal) {
                              var strRetVal = retVal.ToString();
                              if ("null" == retVal.ToString()) {
                                 //Console.WriteLine(returnStatement.GetLocation().GetMappedLineSpan().ToString());
                                 violations.Add(returnStatement.GetLocation().GetMappedLineSpan());
                              }
                           }
                        }
                        if (violations.Any()) {
                           AddViolation(method, violations);
                        }
                     }
                  }
               }
               
            }
            //else {
            //   var property = context.Symbol as IPropertySymbol;
            //   if (null != property) {
            //      property.GetMethod
            //      //property.DeclaringSyntaxReferences
            //   }
            //}
         }
         
         
         //var property = context.
      } 


   }
}
