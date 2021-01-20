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
       Id = "EI_UseLogicalORInsteadOfBitwiseORInBooleanContext",
       Title = "Use Logical OR instead of Bitwise OR in boolean context",
       MessageFormat = "Use Logical OR instead of Bitwise OR in boolean context",
       Category = "Programming Practices - Unexpected Behavior",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.UseLogicalORInsteadOfBitwiseORInBooleanContext"
   )]
   public class UseLogicalORInsteadOfBitwiseORInBooleanContext : AbstractRuleChecker {
      public UseLogicalORInsteadOfBitwiseORInBooleanContext() {
      }

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         context.RegisterSyntaxNodeAction(this.Analyze, SyntaxKind.BitwiseOrExpression);
      }

      private object _lock = new object();
      private void Analyze(SyntaxNodeAnalysisContext context) {
         lock (_lock) {
            var expr = context.Node as BinaryExpressionSyntax;
            if (null != expr) {
               var booleanType = context.SemanticModel.Compilation.GetTypeByMetadataName("System.Boolean");
               if ((null != expr.Left && context.SemanticModel.GetTypeInfo(expr.Left).Type.Equals(booleanType)) ||
               (null != expr.Right && context.SemanticModel.GetTypeInfo(expr.Right).Type.Equals(booleanType))) {
                  var pos = expr.SyntaxTree.GetMappedLineSpan(expr.Span);
                  //Console.WriteLine(pos);
                  AddViolation(context.ContainingSymbol, new FileLinePositionSpan[] { pos });
               }
            }
         }
      }

   }
}
