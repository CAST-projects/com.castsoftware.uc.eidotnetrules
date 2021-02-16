using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.DotNet.CastDotNetExtension;


namespace CastDotNetExtension {
   [CastRuleChecker]
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   [RuleDescription(
       Id = "EI_MutableStaticFieldsOfTypeCollectionOrArrayShouldNotBePublicStatic",
       Title = "Mutable static fields of type System.Collections.Generic.ICollection<T> or System.Array should not be public static",
       MessageFormat = "Mutable static fields of type System.Collections.Generic.ICollection<T> or System.Array should not be public static",
       Category = "Programming Practices - Unexpected Behavior",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.MutableStaticFieldsOfTypeCollectionOrArrayShouldNotBePublicStatic"
   )]
   public class MutableStaticFieldsOfTypeCollectionOrArrayShouldNotBePublicStatic : AbstractRuleChecker {
      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);
      }

      private readonly object _lock = new object();
      private void Analyze(SymbolAnalysisContext context) {
         lock (_lock) {
            try {
               var type = context.Symbol as INamedTypeSymbol;
               if (null != type) {
                  var SystemArray = context.Compilation.GetTypeByMetadataName("System.Array");
                  var SystemGenericICollection = context.Compilation.GetTypeByMetadataName("System.Collections.Generic.ICollection`1");
                  var SystemCollectionsICollection = context.Compilation.GetTypeByMetadataName("System.Collections.ICollection");
                  if (TypeKind.Class == type.TypeKind || TypeKind.Struct == type.TypeKind) {
                     var targetFields = type.GetMembers().
                        OfType<IFieldSymbol>().
                        Where(field => !field.IsReadOnly &&
                           Accessibility.Public == field.DeclaredAccessibility &&
                           field.IsStatic &&
                           (
                              field.Type.BaseType == SystemArray ||
                              (
                                 (
                                    (null != SystemGenericICollection && field.Type.AllInterfaces.Contains(SystemGenericICollection)) ||
                                    (null != SystemCollectionsICollection && field.Type.AllInterfaces.Contains(SystemCollectionsICollection))
                                 )
                                 &&
                                 !field.Type.OriginalDefinition.ToString().StartsWith("System.Collections.ObjectModel.ReadOnly")
                              )
                           )
                           );

                     foreach (IFieldSymbol field in targetFields) {
                        var pos = field.Locations.FirstOrDefault().GetMappedLineSpan();
                        //Log.WarnFormat("{0}: {1}", field.Name, pos);
                        AddViolation(context.Symbol, new FileLinePositionSpan[] { pos });
                     }
                  }
               }
            }
            catch (Exception e) {
               HashSet<string> filePaths = new HashSet<string>();
               foreach (var synRef in context.Symbol.DeclaringSyntaxReferences) {
                  filePaths.Add(synRef.SyntaxTree.FilePath);
               }
               Log.Warn("Exception while analyzing " + string.Join(",", filePaths), e);

            }
         }
      }
   }
}
