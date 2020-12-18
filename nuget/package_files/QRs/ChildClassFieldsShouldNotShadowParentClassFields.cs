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
       Id = "TODO: Add prefix_ChildClassFieldsShouldNotShadowParentClassFields",
       Title = "Child Class Fields Should Not Shadow Parent Class Fields",
       MessageFormat = "Child Class Fields Should Not Shadow Parent Class Fields",
       Category = "Unexpected Behavior",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "DotNetQualityRules.ChildClassFieldsShouldNotShadowParentClassFields"
   )]
   public class ChildClassFieldsShouldNotShadowParentClassFields : AbstractRuleChecker {
      public ChildClassFieldsShouldNotShadowParentClassFields() {
      }

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         //TODO: register for events
         context.RegisterSymbolAction(this.AnalyzeClass, SymbolKind.NamedType);
      }

      private void AnalyzeClass(SymbolAnalysisContext context) {
         var klazz = context.Symbol as INamedTypeSymbol;
         if (null != klazz) {
            Dictionary<string, ISymbol> fields = new Dictionary<string, ISymbol>();
            bool isTargetClass = true;
            do {
               foreach (var member in klazz.GetMembers()) {
                  var field = member as IFieldSymbol;
                  if (null != field) {
                     string fieldName = field.Name.ToLower();
                     //Console.WriteLine("Field Name: " + fieldName);
                     if (isTargetClass) {
                        fields[fieldName] = field;
                     }
                     else if (fields.ContainsKey(fieldName)) {
                        var fieldSymbol = fields[fieldName];
                        var mainPos = fieldSymbol.Locations.FirstOrDefault().GetMappedLineSpan();
                        var additionalPos = field.Locations.FirstOrDefault().GetMappedLineSpan();
                        //Console.WriteLine("Main Pos: " + mainPos.ToString() + " Additional Pos: " + additionalPos.ToString());
                        AddViolation(context.Symbol, new List<FileLinePositionSpan>() { mainPos, additionalPos});
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
   }
}
