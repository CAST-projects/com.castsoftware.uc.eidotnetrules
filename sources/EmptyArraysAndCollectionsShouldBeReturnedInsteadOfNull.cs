﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.DotNet.CastDotNetExtension;
using Roslyn.DotNet.Common;


namespace CastDotNetExtension {
   [CastRuleChecker]
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   [RuleDescription(
       Id = "EI_EmptyArraysAndCollectionsShouldBeReturnedInsteadOfNull",
       Title = "Empty arrays and collections should be returned instead of null",
       MessageFormat = "Empty arrays and collections should be returned instead of null",
       Category = "Programming Practices - Unexpected Behavior",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.EmptyArraysAndCollectionsShouldBeReturnedInsteadOfNull"
   )]
   public class EmptyArraysAndCollectionsShouldBeReturnedInsteadOfNull : AbstractRuleChecker {
      public EmptyArraysAndCollectionsShouldBeReturnedInsteadOfNull()
            : base(ViolationCreationMode.ViolationWithAdditionalBookmarks)
        {
        }

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         context.RegisterSymbolAction(Analyze, SymbolKind.Method, SymbolKind.Property);
      }

      private readonly object _lock = new object();
      private void Analyze(SymbolAnalysisContext context) {

         /*lock (_lock)*/ {
            try {
               var method = context.Symbol as IMethodSymbol;


               if (null != method) {
                  if (TypeKind.Array == method.ReturnType.TypeKind ||
                     method.ReturnType.OriginalDefinition.ToString().StartsWith("System.Collections.Generic.")) {
                     var syntaxRefs = method.DeclaringSyntaxReferences;
                     foreach (var syntaxRef in syntaxRefs) {
                        SyntaxNode syntaxNode = syntaxRef.GetSyntaxAsync(context.CancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();
                        var returnStatements = syntaxNode.DescendantNodes().OfType<ReturnStatementSyntax>();

                        List<FileLinePositionSpan> violations = new List<FileLinePositionSpan>();
                        foreach (var returnStatement in returnStatements) {
                           var retVal = returnStatement.Expression as LiteralExpressionSyntax;
                           if (null != retVal) {
                              if ("null" == retVal.ToString()) {
                                 //Log.Warn(returnStatement.GetLocation().GetMappedLineSpan().ToString());
                                 violations.Add(returnStatement.GetLocation().GetMappedLineSpan());
                              }
                           }
                        }
                        if (violations.Any()) {
                           AddViolation(method, violations);
                        }
                     }
                  }

               }
            }
            catch (Exception e) {
               HashSet<string> filePaths = new HashSet<string>();
               foreach (var synRef in context.Symbol.DeclaringSyntaxReferences) {
                  filePaths.Add(synRef.SyntaxTree.FilePath);
               }
               Log.Warn(" Exception while analyzing " + string.Join(",", filePaths), e);
            }
         }

      } 
   }
}
