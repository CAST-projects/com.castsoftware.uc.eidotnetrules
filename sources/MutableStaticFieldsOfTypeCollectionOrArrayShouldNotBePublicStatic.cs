﻿using System;
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
       Id = "EI_MutableStaticFieldsOfTypeCollectionOrArrayShouldNotBePublicStatic",
       Title = "Mutable static fields of type System.Collections.Generic.ICollection<T> or System.Array should not be public static",
       MessageFormat = "Mutable static fields of type System.Collections.Generic.ICollection<T> or System.Array should not be public static",
       Category = "Programming Practices - Unexpected Behavior",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.MutableStaticFieldsOfTypeCollectionOrArrayShouldNotBePublicStatic"
   )]
   public class MutableStaticFieldsOfTypeCollectionOrArrayShouldNotBePublicStatic : AbstractRuleChecker {
      public MutableStaticFieldsOfTypeCollectionOrArrayShouldNotBePublicStatic() {
      }

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         context.RegisterSymbolAction(this.Analyze, SymbolKind.NamedType);
      }

      private object _lock = new object();
      private void Analyze(SymbolAnalysisContext context) {
         lock (_lock) {
            var type = context.Symbol as INamedTypeSymbol;
            if (null != type) {
               System.Collections.Generic.ICollection<int> x = null;
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
                     //Console.WriteLine(field.Name + ":" + pos);
                     AddViolation(context.Symbol, new FileLinePositionSpan[] { pos });
                  }
               }
            }
         }
      }
   }
}