using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.DotNet.CastDotNetExtension;


namespace CastDotNetExtension {
   [CastRuleChecker]
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   [RuleDescription(
       Id = "EI_UseLogicalORInsteadOfBitwiseORInBooleanContext",
       Title = "Use Logical OR and AND instead of Bitwise OR and AND in boolean context",
       MessageFormat = "Use Logical OR and AND instead of Bitwise OR and AND in boolean context",
       Category = "Programming Practices - Unexpected Behavior",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.UseLogicalORInsteadOfBitwiseORInBooleanContext"
   )]
   public class UseLogicalORandANDInsteadOfBitwiseORandANDInBooleanContext : AbstractRuleChecker {

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
          context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.BitwiseOrExpression);
          context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.BitwiseAndExpression);
      }

      private readonly object _lock = new object();
      private void Analyze(SyntaxNodeAnalysisContext context) {
          Log.InfoFormat("Run registered callback for rule: {0}", GetRuleName());
         /*lock (_lock)*/ {
            try {
               var expr = context.Node as BinaryExpressionSyntax;
               if (null != expr) {
                  var booleanType = context.SemanticModel.Compilation.GetTypeByMetadataName("System.Boolean");
                  if (null != expr.Left && context.SemanticModel.GetTypeInfo(expr.Left).Type.Equals(booleanType) ||
                  null != expr.Right && context.SemanticModel.GetTypeInfo(expr.Right).Type.Equals(booleanType)) {
                     var pos = expr.SyntaxTree.GetMappedLineSpan(expr.Span);
                     //Log.Warn(pos);
                     AddViolation(context.ContainingSymbol, new[] { pos });
                  }
               }
            }
            catch (System.Exception e) {
               Log.Warn(" Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath + ": " + context.Node.GetLocation().GetMappedLineSpan(), e);
            }
         }
      }

   }
}
