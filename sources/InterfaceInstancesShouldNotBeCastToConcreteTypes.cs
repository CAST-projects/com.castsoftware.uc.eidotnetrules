using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using log4net;
using Roslyn.DotNet.CastDotNetExtension;
using Roslyn.DotNet.Common;


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
      
      public InterfaceInstancesShouldNotBeCastToConcreteTypes()
            : base(ViolationCreationMode.ViolationWithAdditionalBookmarks)
        {
        }

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         //TODO: register for events
         context.RegisterSyntaxNodeAction(this.Analyze, SyntaxKind.CastExpression, SyntaxKind.AsExpression);
      }

      private object _lock = new object();
      private void Analyze(SyntaxNodeAnalysisContext context) {
         lock (_lock) {
            try {
               var castNode = context.Node as CastExpressionSyntax;
               var pos = context.Node.GetLocation().GetMappedLineSpan();
               INamedTypeSymbol fromType = null, toType = null;
               if (null != castNode) {
                  var identifier = castNode.Expression as IdentifierNameSyntax;
                  if (null != identifier) {
                     fromType = (INamedTypeSymbol)context.SemanticModel.GetTypeInfo(identifier).Type;
                     if (null != fromType) {
                        toType = (INamedTypeSymbol)context.SemanticModel.GetTypeInfo(castNode.Type).Type;
                     }
                  }
               }
               else {
                  var binary = context.Node as BinaryExpressionSyntax;
                  if (null != binary) {
                     var left = binary.Left as IdentifierNameSyntax;
                     var right = binary.Right as IdentifierNameSyntax;
                     if (null != left && null != right) {
                        fromType = (INamedTypeSymbol)context.SemanticModel.GetTypeInfo(left).Type;
                        if (null != fromType) {
                           toType = (INamedTypeSymbol)context.SemanticModel.GetTypeInfo(right).Type;
                        }
                     }
                  }
               }

               if (null != fromType && null != toType) {
                  if (TypeKind.Interface == fromType.TypeKind) {
                     if ((TypeKind.Class == toType.TypeKind && !toType.IsAbstract) || TypeKind.Struct == toType.TypeKind) {
                        //Log.Warn(pos.ToString());
                        AddViolation(context, new FileLinePositionSpan[] { pos });
                     }
                  }
               }
            }
            catch (System.Exception e) {
               Log.Warn("Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath, e);
            }
         }
      }
   }
}
