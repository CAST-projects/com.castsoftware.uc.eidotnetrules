using System;
using System.Collections.Generic;
using System.Linq;
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
       Category = "Programming Practices - OO Inheritance and Polymorphism",
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
         context.RegisterSymbolAction(AnalyzeClass, SymbolKind.NamedType);
      }

      private readonly object _lock = new object();

      private void ProcessField(ISymbol field, Dictionary<string, ISymbol> fields, bool isTargetClass) {
         string fieldName = field.Name.ToLower();
         //Log.WarnFormat("Field Name: {0}", fieldName);
         if (isTargetClass) {
            fields[fieldName] = field;
         } else if (fields.ContainsKey(fieldName)) {
            var fieldSymbol = fields[fieldName];
            var mainPos = fieldSymbol.Locations.FirstOrDefault().GetMappedLineSpan();
            var additionalPos = field.Locations.FirstOrDefault().GetMappedLineSpan();
            //Log.InfoFormat("Field: {0} Main Pos: {1} Additional Pos: {2}", fieldSymbol.Name, mainPos.ToString(), additionalPos.ToString());
            AddViolation(fieldSymbol, new List<FileLinePositionSpan> { mainPos, additionalPos });
         }

      }

      private bool ProcessAssociatedProperty(IFieldSymbol fieldMaybeProp, Dictionary<string, ISymbol> fields, bool isTargetClass) {
         if (null != fieldMaybeProp.AssociatedSymbol && SymbolKind.Property == fieldMaybeProp.AssociatedSymbol.Kind) {
            var prop = fieldMaybeProp.AssociatedSymbol as IPropertySymbol;
            if (prop.IsAbstract || prop.IsVirtual || prop.IsOverride) {
               return true;
            }
            var propDeclSyntax = prop.DeclaringSyntaxReferences.FirstOrDefault().GetSyntax();
            if (propDeclSyntax is PropertyDeclarationSyntax) {
               bool isNewed = (propDeclSyntax as PropertyDeclarationSyntax).Modifiers.
                  FirstOrDefault(t => t.IsKind(SyntaxKind.NewKeyword)).
                  //just silly. First will throw if not found. FirstOrDefault will return SyntaxKind.None.
                  IsKind(SyntaxKind.NewKeyword);
               if (isNewed) {
                  return true;
               }
            }
            ProcessField(fieldMaybeProp.AssociatedSymbol, fields, isTargetClass);
            return true;
         }
         return false;
      }

      private void AnalyzeClass(SymbolAnalysisContext context) {
         lock (_lock) {
            try {
               var klazz = context.Symbol as INamedTypeSymbol;
               if (null != klazz && TypeKind.Class == klazz.TypeKind) {
                  Dictionary<string, ISymbol> fields = new Dictionary<string, ISymbol>();
                  bool isTargetClass = true;
                  do {
                     foreach (var field in klazz.GetMembers().OfType<IFieldSymbol>()) {
                        if (!ProcessAssociatedProperty(field, fields, isTargetClass) ||
                           !field.IsImplicitlyDeclared) {
                           ProcessField(field, fields, isTargetClass);
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
               Log.Warn("Exception while analyzing " + string.Join(",", filePaths) + ": " + context.Symbol.Locations.FirstOrDefault().GetMappedLineSpan(), e);
            }
         }
      }
   }
}
