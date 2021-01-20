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
       Id = "EI_AvoidMethodsNamedWithoutFollowingSynchronousAsynchronousConvention",
       Title = "Avoid methods named without following synchronous/asynchronous convention",
       MessageFormat = "Avoid methods named without following synchronous/asynchronous convention",
       Category = "Naming Convention",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.AvoidMethodsNamedWithoutFollowingSynchronousAsynchronousConvention"
   )]
   public class AvoidMethodsNamedWithoutFollowingSynchronousAsynchronousConvention : AbstractRuleChecker {
      public AvoidMethodsNamedWithoutFollowingSynchronousAsynchronousConvention() {
      }

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         //TODO: register for events
         context.RegisterSyntaxNodeAction(Analyze, Microsoft.CodeAnalysis.CSharp.SyntaxKind.MethodDeclaration);
      }

      private object _lock = new object();
      protected void Analyze(SyntaxNodeAnalysisContext context) {
         lock (_lock) {
            try {
               var method = context.ContainingSymbol as IMethodSymbol;
               if (null != method) {
                  var typeSymbol = method.ReturnType as ITypeSymbol;

                  var name = method.Name;
                  var typeFullName = typeSymbol.ToString();
                  Boolean returnsAsync = typeFullName.StartsWith("System.Threading.Tasks.Task");

                  if (returnsAsync != name.EndsWith("Async")) {
                     var span = context.Node.Span;
                     var pos = context.Node.SyntaxTree.GetMappedLineSpan(span);
                     //Console.WriteLine(pos.ToString());
                     AddViolation(context.ContainingSymbol, new FileLinePositionSpan[] { pos });
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
