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
       Id = "EI_ChildClassFieldsShouldNotShadowParentClassFields",
       Title = "Child Class Fields Should Not Shadow Parent Class Fields",
       MessageFormat = "Child Class Fields Should Not Shadow Parent Class Fields",
       Category = "Unexpected Behavior",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.ChildClassFieldsShouldNotShadowParentClassFields"
   )]
   public class ChildClassFieldsShouldNotShadowParentClassFields : AbstractRuleChecker {
      public ChildClassFieldsShouldNotShadowParentClassFields()
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
                  Dictionary<string, ISymbol> fields = new Dictionary<string, ISymbol>();
                  bool isTargetClass = true;
                  do {
                     foreach (var member in klazz.GetMembers()) {
                        var field = member as IFieldSymbol;
                        if (null != field) {
                           string fieldName = field.Name.ToLower();
                           //Log.WarnFormat("Field Name: {0}", fieldName);
                           if (isTargetClass) {
                              fields[fieldName] = field;
                           }
                           else if (fields.ContainsKey(fieldName)) {
                              var fieldSymbol = fields[fieldName];
                              var mainPos = fieldSymbol.Locations.FirstOrDefault().GetMappedLineSpan();
                              var additionalPos = field.Locations.FirstOrDefault().GetMappedLineSpan();
                              //Log.WarnFormat("Main Pos: {0} Additional Pos: ", mainPos.ToString(), additionalPos.ToString());
                              AddViolation(fieldSymbol, new List<FileLinePositionSpan>() { mainPos, additionalPos });
                           }
                        }
                     }
                     if (!fields.Any()) {
                        break;
                     }
                     isTargetClass = false;
                     klazz = klazz.BaseType;
                  } while (null != klazz && !klazz.ToString().Equals("object"));
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
