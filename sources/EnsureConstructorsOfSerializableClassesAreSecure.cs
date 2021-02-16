using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.DotNet.CastDotNetExtension;
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

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         context.RegisterSymbolAction(AnalyzeClass, SymbolKind.NamedType);
      }

      private void AnalyzeClass(SymbolAnalysisContext context) {
         try {
            INamedTypeSymbol namedType = context.Symbol as INamedTypeSymbol;
            if (null != namedType) {
               if (TypeKind.Class == namedType.TypeKind || TypeKind.Struct == namedType.TypeKind) {
                  if (namedType.IsSerializable) {
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
                     if (regularConstructorSecured && !serializationConstructorSecured) {
                        var pos = ctorSerializing.Locations.FirstOrDefault().GetMappedLineSpan();
                        //Console.WriteLine(ctorSerializing.ToString() + ": " + pos);
                        AddViolation(ctorSerializing, new FileLinePositionSpan[] { pos });
                     }
                  }
               }
            }
         } catch (Exception e) {
            HashSet<string> filePaths = new HashSet<string>();
            foreach (var synRef in context.Symbol.DeclaringSyntaxReferences) {
               filePaths.Add(synRef.SyntaxTree.FilePath);
            }
            Log.Warn("Exception while analyzing " + string.Join(",", filePaths), e);
         }
      }
   }
}
