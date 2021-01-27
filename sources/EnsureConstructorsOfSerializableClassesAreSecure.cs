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
using CastDotNetExtension.Utils;


namespace CastDotNetExtension
{
   [CastRuleChecker]
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   [RuleDescription(
       Id = "EI_EnsureConstructorsOfSerializableClassesAreSecure",
       Title = "Ensure Constructors Of Serializable Classes Are Secure",
       MessageFormat = "Ensure Constructors Of Serializable Classes Are Secure",
       Category = "Secure Coding - Weak Security Features",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.EnsureConstructorsOfSerializableClassesAreSecure"
   )]
   public class EnsureConstructorsOfSerializableClassesAreSecure : AbstractRuleChecker
   {
      public EnsureConstructorsOfSerializableClassesAreSecure() {
      }

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         context.RegisterSymbolAction(AnalyzeClass, SymbolKind.NamedType);
      }

      private void AnalyzeClass(SymbolAnalysisContext context) {
         INamedTypeSymbol namedType = context.Symbol as INamedTypeSymbol;
         if (null != namedType) {
            if (TypeKind.Class == namedType.TypeKind || TypeKind.Struct == namedType.TypeKind) {
               IList<TypeAttributes.ITypeAttribute> attributesKlazz = new List<TypeAttributes.ITypeAttribute>();
               attributesKlazz = TypeAttributes.Get(namedType, attributesKlazz, new[] { TypeAttributes.AttributeType.Serializable });
               if (null != attributesKlazz) {
                  ISymbol ctorSerializing = null;
                  bool serializationConstructorSecured = true;
                  bool regularConstructorSecured = true;
                  foreach (var ctor in namedType.Constructors) {
                     bool isSerializingCtor = false;
                     if (2 == ctor.Parameters.Length && 
                        "System.Runtime.Serialization.SerializationInfo" == ctor.Parameters.ElementAt(0).OriginalDefinition.ToString() &&
                        "System.Runtime.Serialization.StreamingContext" == ctor.Parameters.ElementAt(1).OriginalDefinition.ToString()
                        ) {
                           ctorSerializing = ctor;
                           isSerializingCtor = true;
                     }
                     List<TypeAttributes.ITypeAttribute> ctorAttributes = new List<TypeAttributes.ITypeAttribute>();
                     var attributesOutCtor = TypeAttributes.Get(ctor, ctorAttributes, new[] { TypeAttributes.AttributeType.FileIOPermissionAttribute });
                     if (!attributesOutCtor.Any()) {
                        if (isSerializingCtor) {
                           serializationConstructorSecured = false;
                        } else {
                           regularConstructorSecured = false;
                        }
                     } else {
                        if (isSerializingCtor) {
                           serializationConstructorSecured = true;
                        } else {
                           regularConstructorSecured = true;
                        }
                     }
                  }
                  if (regularConstructorSecured && !serializationConstructorSecured && null != ctorSerializing) {
                     var pos = ctorSerializing.Locations.FirstOrDefault().GetMappedLineSpan();
                     //Console.WriteLine(ctorSerializing.ToString() + ": " + pos);
                     AddViolation(ctorSerializing, new FileLinePositionSpan[] { pos });
                  }
               }
            }
         }
      }
   }
}
