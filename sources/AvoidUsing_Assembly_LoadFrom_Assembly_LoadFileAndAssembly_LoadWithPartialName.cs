using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.DotNet.CastDotNetExtension;
using CastDotNetExtension.Utils;


namespace CastDotNetExtension {
   [CastRuleChecker]
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   [RuleDescription(
       Id = "EI_AvoidUsing_Assembly_LoadFrom_Assembly_LoadFileAndAssembly_LoadWithPartialName",
       Title = "Avoid using Assembly.LoadFrom, Assembly.LoadFile and Assembly.LoadWithPartialName",
       MessageFormat = "Avoid using Assembly.LoadFrom, Assembly.LoadFile and Assembly.LoadWithPartialName",
       Category = "Programming Practices - Unexpected Behavior",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.AvoidUsing_Assembly_LoadFrom_Assembly_LoadFileAndAssembly_LoadWithPartialName"
   )]
   public class AvoidUsing_Assembly_LoadFrom_Assembly_LoadFileAndAssembly_LoadWithPartialName : AbstractRuleChecker {

      private static readonly HashSet<string> MethodNames = new HashSet<string> { 
            "LoadFrom",
            "LoadFile",
            "LoadWithPartialName"
      };


      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         context.RegisterOperationAction(AnalyzeCall, OperationKind.Invocation);
      }

      private void AnalyzeCall(OperationAnalysisContext context)
      {
          Log.InfoFormat("Run registered callback for rule: {0}", GetRuleName());
         IInvocationOperation invocation = context.Operation as IInvocationOperation;
         System.Diagnostics.Debug.Assert(null != invocation && null != invocation.TargetMethod);
         HashSet<IMethodSymbol> methodSymbols =
            context.Compilation.GetMethodSymbolsForSystemClass(
               context.Compilation.GetTypeByMetadataName("System.Reflection.Assembly"), MethodNames, false, 8);

         if (methodSymbols.Contains(invocation.TargetMethod)) {
            AddViolation(context.ContainingSymbol, new[] { invocation.Syntax.GetLocation().GetMappedLineSpan() });
         }
      }
   }
}
