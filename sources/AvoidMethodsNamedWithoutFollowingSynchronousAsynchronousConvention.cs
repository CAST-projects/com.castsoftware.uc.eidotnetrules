using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.DotNet.CastDotNetExtension;


namespace CastDotNetExtension {
   [CastRuleChecker]
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   [RuleDescription(
       Id = "EI_AvoidMethodsNamedWithoutFollowingSynchronousAsynchronousConvention",
       Title = "Avoid methods named without following synchronous/asynchronous convention",
       MessageFormat = "Avoid methods named without following synchronous/asynchronous convention",
       Category = "Documentation - Style Conformity",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.AvoidMethodsNamedWithoutFollowingSynchronousAsynchronousConvention"
   )]
   public class AvoidMethodsNamedWithoutFollowingSynchronousAsynchronousConvention : AbstractRuleChecker {

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         context.RegisterSymbolAction(AnalyzeMethodName, SymbolKind.Method);
      }

      private readonly object _lock = new object();
      private void AnalyzeMethodName(SymbolAnalysisContext context) {
         /*lock (_lock)*/ {
            try {
               if (SymbolKind.Method == context.Symbol.Kind) {
                  var method = context.Symbol as IMethodSymbol;
                  if (!method.ReturnsVoid) {
                     var typeSymbol = method.ReturnType;
                     var typeFullName = typeSymbol.ToString();
                     bool isAsync = typeFullName.StartsWith("System.Threading.Tasks.Task"); //method.IsAsync <=== is always false
                     if (isAsync != method.Name.EndsWith("Async")) {
                        var pos = method.Locations.FirstOrDefault().GetMappedLineSpan();
                        AddViolation(method, new FileLinePositionSpan[] { pos });
                     }
                  }
               }
            } catch (Exception e) {
               HashSet<string> filePaths = new HashSet<string>();
               foreach (var synRef in context.Symbol.DeclaringSyntaxReferences) {
                  filePaths.Add(synRef.SyntaxTree.FilePath);
               }
               Log.Warn("[com.castsoftware.eidotnetrules] Exception while analyzing " + string.Join(",", filePaths), e);
            }
         }
      }
   }
}
