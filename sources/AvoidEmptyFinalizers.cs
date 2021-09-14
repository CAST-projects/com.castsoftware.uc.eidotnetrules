using System;
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
       Id = "EI_AvoidEmptyFinalizers",
       Title = "Avoid Empty Finalizers",
       MessageFormat = "Avoid Empty Finalizers",
       Category = "Efficiency - Performance",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.AvoidEmptyFinalizers"
   )]
   public class AvoidEmptyFinalizers : AbstractRuleChecker {

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.Block);
      }

      private readonly object _lock = new object();
      private void Analyze(SyntaxNodeAnalysisContext context) {
          Log.InfoFormat("Run registered callback for rule: {0}", GetRuleName());
         /*lock (_lock)*/ {
            try {

               var finalizer = context.ContainingSymbol as IMethodSymbol;
               if (null != finalizer && MethodKind.Destructor == finalizer.MethodKind) {
                  var body = context.Node as BlockSyntax;                  
                  if (null != body) {

                     var statements = body.WithoutTrivia().Statements.Where(statement => !statement.ToString().StartsWith("Debug.Fail"));
                     if (!statements.Any()) {
                        var pos = context.ContainingSymbol.Locations.FirstOrDefault().GetMappedLineSpan();
                        //Log.Warn(context.ContainingSymbol.ContainingSymbol.Name + ": " + pos);
                        AddViolation(context.ContainingSymbol, new[] {pos});
                     }
                  }
               }
            }
            catch (Exception e) {
               Log.Warn(" Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath + ": " + context.Node.GetLocation().GetMappedLineSpan(), e);
            }
         }
         Log.InfoFormat("END Run registered callback for rule: {0}", GetRuleName());
      }


   }
}
