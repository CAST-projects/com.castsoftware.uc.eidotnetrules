using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.DotNet.CastDotNetExtension;


namespace CastDotNetExtension {
   [CastRuleChecker]
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   [RuleDescription(
       Id = "EI_InterfaceInstancesShouldNotBeCastToConcreteTypes",
       Title = "Interface Instances Should Not Be Cast To Concrete Types",
       MessageFormat = "Interface Instances Should Not Be Cast To Concrete Types",
       Category = "OO - Abstraction",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.InterfaceInstancesShouldNotBeCastToConcreteTypes"
   )]
   public class InterfaceInstancesShouldNotBeCastToConcreteTypes : AbstractRuleChecker {
      

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         //TODO: register for events
         context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.CastExpression, SyntaxKind.AsExpression);
      }

      private readonly object _lock = new object();
      private void Analyze(SyntaxNodeAnalysisContext context) {
         /*lock (_lock)*/ {
            try {
               var castNode = context.Node as CastExpressionSyntax;
               INamedTypeSymbol fromType = null, toType = null;
               var pos = context.Node.GetLocation().GetMappedLineSpan();
               if (null != castNode) {
                  if (castNode.Expression is IdentifierNameSyntax) {
                     var identifier = castNode.Expression as IdentifierNameSyntax;
                     if (null != identifier && context.SemanticModel.GetTypeInfo(identifier).Type is INamedTypeSymbol) {
                        fromType = (INamedTypeSymbol)context.SemanticModel.GetTypeInfo(identifier).Type;
                        if (null != fromType && context.SemanticModel.GetTypeInfo(castNode.Type).Type is INamedTypeSymbol) {
                           toType = (INamedTypeSymbol)context.SemanticModel.GetTypeInfo(castNode.Type).Type;
                        }
                     }
                  }
               }
               else {
                  var binary = context.Node as BinaryExpressionSyntax;
                  if (null != binary) {
                     var left = binary.Left as IdentifierNameSyntax;
                     var right = binary.Right as IdentifierNameSyntax;
                     if (null != left && null != right) {
                        var typeFrom = context.SemanticModel.GetTypeInfo(left).Type;
                        if (typeFrom is INamedTypeSymbol) {
                           fromType = (INamedTypeSymbol)typeFrom;
                           var typeTo = context.SemanticModel.GetTypeInfo(right).Type;
                           if (typeTo is INamedTypeSymbol) {
                              toType = (INamedTypeSymbol)typeTo;
                           }
                        }
                     }
                  }
               }

               if (null != fromType && null != toType) {
                  if (TypeKind.Interface == fromType.TypeKind) {
                     if (TypeKind.Class == toType.TypeKind && !toType.IsAbstract || TypeKind.Struct == toType.TypeKind) {
                        //Log.Warn(pos.ToString());
                        AddViolation(context, new[] { pos });
                     }
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
