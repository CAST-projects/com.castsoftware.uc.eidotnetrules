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
         //context.RegisterSyntaxNodeAction(Analyze, Microsoft.CodeAnalysis.CSharp.SyntaxKind.MethodDeclaration);
      }

      private void AnalyzeMethodName(SymbolAnalysisContext context) {
         try {
            if (SymbolKind.Method == context.Symbol.Kind) {
               var method = context.Symbol as IMethodSymbol;
               if (!method.ReturnsVoid && method.ReturnType is ITypeSymbol) {
                  var typeSymbol = method.ReturnType as ITypeSymbol;
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
            Log.Warn("Exception while analyzing " + String.Join(",", filePaths), e);
         }
      }

      private readonly object _lock = new object();
      protected void Analyze(SyntaxNodeAnalysisContext context) {
         lock (_lock) {
            try {
               var method = context.ContainingSymbol as IMethodSymbol;
               if (null != method) {
                  var typeSymbol = method.ReturnType as ITypeSymbol;

                  var name = method.Name;
                  var typeFullName = typeSymbol.ToString();
                  bool returnsAsync = typeFullName.StartsWith("System.Threading.Tasks.Task");

                  //if (method.IsAsync != name.EndsWith("Async")) {   <=== IsAsync is always false
                  if (returnsAsync != name.EndsWith("Async")) {
                     var span = context.Node.Span;
                     var pos = context.Node.SyntaxTree.GetMappedLineSpan(span);
                     //Log.Warn(pos.ToString());
                     AddViolation(context.ContainingSymbol, new FileLinePositionSpan[] { pos });
                  }
               }
            }
            catch (Exception e) {
               Log.Warn("Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath + ": " + context.Node.GetLocation().GetMappedLineSpan(), e);
            }
         }
      }
   }
}
