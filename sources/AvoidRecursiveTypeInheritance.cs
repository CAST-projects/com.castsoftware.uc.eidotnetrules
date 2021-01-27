using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.DotNet.CastDotNetExtension;
using Roslyn.DotNet.Common;


namespace CastDotNetExtension {
   [CastRuleChecker]
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   [RuleDescription(
       Id = "EI_AvoidRecursiveTypeInheritance",
       Title = "Avoid Recursive Type Inheritance",
       MessageFormat = "Avoid Recursive Type Inheritance",
       Category = "Complexity - Technical Complexity",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.AvoidRecursiveTypeInheritance"
   )]
   public class AvoidRecursiveTypeInheritance : AbstractRuleChecker {
      public AvoidRecursiveTypeInheritance()
            : base(ViolationCreationMode.ViolationWithAdditionalBookmarks)
        {
        }

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         context.RegisterSymbolAction(this.AnalyzeClass, SymbolKind.NamedType);
      }

      private object _lock = new object();
      private void AnalyzeClass(SymbolAnalysisContext context) {
         lock (_lock) {
            try {
               var klazz = context.Symbol as INamedTypeSymbol;
               if (null != klazz && TypeKind.Class == klazz.TypeKind) {
                  if (klazz.IsGenericType && 1 == klazz.TypeParameters.Length
                     && klazz.BaseType.IsGenericType && 1 == klazz.BaseType.TypeParameters.Length) {
                     var fullName = klazz.ToString();
                     int idx = fullName.IndexOf('<');
                     if (-1 != idx) {
                        fullName = fullName.Substring(0, idx);
                        var baseFullName = klazz.BaseType.ToString();
                        var parts = baseFullName.Split('<');
                        if (3 < parts.Length) {
                           var baseLastTypeParam = parts.Last().Trim('>');
                           var typeParamName = klazz.TypeParameters.Last().ToString();
                           if (typeParamName == baseLastTypeParam) {
                              for (int i = 1; i < parts.Length - 1; ++i) {
                                 if (parts.ElementAt(i) == fullName) {
                                    var pos = context.Symbol.Locations.FirstOrDefault().GetMappedLineSpan();
                                    //Log.WarnFormat("{0}: {1}", context.Symbol.Name, pos);
                                    AddViolation(context.Symbol, new FileLinePositionSpan[] { pos });
                                    break;
                                 }
                              }
                           }
                        }
                     }
                  }
               }
            }
            catch (System.Exception e) {
               HashSet<string> filePaths = new HashSet<string>();
               foreach (var synRef in context.Symbol.DeclaringSyntaxReferences) {
                  filePaths.Add(synRef.SyntaxTree.FilePath);
               }
               Log.Warn("Exception while analyzing " + String.Join(",", filePaths) + ": " + context.Symbol.Locations.FirstOrDefault().GetMappedLineSpan(), e);
            }
         }
      }



   }
}
