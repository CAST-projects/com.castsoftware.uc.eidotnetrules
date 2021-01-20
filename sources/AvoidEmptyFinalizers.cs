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
       Id = "EI_AvoidEmptyFinalizers",
       Title = "Avoid Empty Finalizers",
       MessageFormat = "Avoid Empty Finalizers",
       Category = "Efficiency - Performance",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.AvoidEmptyFinalizers"
   )]
   public class AvoidEmptyFinalizers : AbstractRuleChecker {
      public AvoidEmptyFinalizers() {
      }

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         context.RegisterSyntaxNodeAction(this.Analyze, SyntaxKind.Block);
      }

      private object _lock = new object();
      private void Analyze(SyntaxNodeAnalysisContext context) {
         lock (_lock) {
            try {

               var finalizer = context.ContainingSymbol as IMethodSymbol;
               if (null != finalizer && MethodKind.Destructor == finalizer.MethodKind) {
                     var body = context.Node as BlockSyntax;                  
                     if (null != body) {

                        var statements = body.WithoutTrivia().Statements.Where(statement => !statement.ToString().StartsWith("Debug.Fail")); ;
                        if (!statements.Any()) {
                           var pos = context.ContainingSymbol.Locations.FirstOrDefault().GetMappedLineSpan();
                           //Console.WriteLine(context.ContainingSymbol.ContainingSymbol.Name + ": " + pos);
                           AddViolation(context.ContainingSymbol, new FileLinePositionSpan [] {pos});
                        }
                     }
                  }
            }
            catch (Exception e) {
               Console.WriteLine(e.Message);
               Console.WriteLine(e.StackTrace);
            }
         }
      }


   }
}
