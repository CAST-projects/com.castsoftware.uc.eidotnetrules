using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.DotNet.CastDotNetExtension;
using Microsoft.CodeAnalysis.Operations;
using System;

namespace CastDotNetExtension {
   [CastRuleChecker]
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   [RuleDescription(
       Id = "EI_EnsureProperArgumentsToEvents",
       Title = "Ensure Proper Arguments To Events",
       MessageFormat = "Ensure Proper Arguments To Events",
       Category = "Programming Practices - Error and Exception Handling",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.EnsureProperArgumentsToEvents"
   )]
   public class EnsureProperArgumentsToEvents : AbstractRuleChecker {

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         context.RegisterOperationAction(AnalyzeEventInvoke, OperationKind.EventReference);
      }

      private void AnalyzeEventInvoke(OperationAnalysisContext context)
      {
         try {
            IEventReferenceOperation eventReference = context.Operation as IEventReferenceOperation;
            System.Diagnostics.Debug.Assert(null != eventReference);
            IInvocationOperation invocation = OperationKind.Invocation ==
               eventReference.Parent.Kind ? eventReference.Parent as IInvocationOperation : null;
            if (null == invocation && OperationKind.ConditionalAccess == eventReference.Parent.Kind &&
               2 == eventReference.Parent.Children.Count() && OperationKind.Invocation == eventReference.Parent.Children.ElementAt(1).Kind) {
               invocation = eventReference.Parent.Children.ElementAt(1) as IInvocationOperation;
            }

            if (null != invocation && "Invoke" == invocation.TargetMethod.Name && 2 == invocation.Arguments.Count()) {
               var arg0null = invocation.Arguments.ElementAt(0).Value.ConstantValue.HasValue &&
                  null == invocation.Arguments.ElementAt(0).Value.ConstantValue.Value;
               var arg1null = invocation.Arguments.ElementAt(1).Value.ConstantValue.HasValue &&
                  null == invocation.Arguments.ElementAt(1).Value.ConstantValue.Value;

               if ((!eventReference.Member.IsStatic && arg0null) || arg1null) {
                  AddViolation(context.ContainingSymbol, new FileLinePositionSpan[] { invocation.Syntax.GetLocation().GetMappedLineSpan() });
               }
            }
         } catch (Exception e) {
            Log.Warn(" Exception while analyzing " + context.Operation.Syntax.SyntaxTree.FilePath + 
               " Pos: " + context.Operation.Syntax.GetLocation().GetMappedLineSpan(), e);
         }
      }

   }
}
