using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.DotNet.CastDotNetExtension;
using CastDotNetExtension.Utils;
using System.Collections.Concurrent;


namespace CastDotNetExtension
{
   [CastRuleChecker]
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   [RuleDescription(
       Id = "EI_EnsureSerializableTypesFollowBestPractices",
       Title = "Ensure Serializable Types Follow Best Practices",
       MessageFormat = "Ensure Serializable Types Follow Best Practices",
       Category = "Programming Practices - Unexpected Behavior",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.EnsureSerializableTypesFollowBestPractices"
   )]
   public class EnsureSerializableTypesFollowBestPractices : AbstractRuleChecker
   {
      private INamedTypeSymbol _SerializableAttr;
      private INamedTypeSymbol _ISerializable;
      private INamedTypeSymbol _SerializationInfo;
      private INamedTypeSymbol _StreamingContext;
      private INamedTypeSymbol _NonSerializedAttr;

      private ConcurrentDictionary<INamedTypeSymbol, Data> _symbolToData =
         new ConcurrentDictionary<INamedTypeSymbol, Data>();

         
      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context)
      {
         _symbolToData.Clear();
         context.RegisterCompilationStartAction(OnCompilationStart);
      }

      private void OnCompilationStart(CompilationStartAnalysisContext context)
      {
         _SerializableAttr = context.Compilation.GetTypeByMetadataName("System.SerializableAttribute");
         if (null == _SerializableAttr) {
            Log.InfoFormat("Could not get type for System.SerializableAttribute. \"{0}\" will be disabled for \"{1}\"", GetRuleName(), context.Compilation.AssemblyName);
         } else {
            _ISerializable = context.Compilation.GetTypeByMetadataName("System.Runtime.Serialization.ISerializable");
            _SerializationInfo = context.Compilation.GetTypeByMetadataName("System.Runtime.Serialization.SerializationInfo");
            _StreamingContext = context.Compilation.GetTypeByMetadataName("System.Runtime.Serialization.StreamingContext");
            _NonSerializedAttr = context.Compilation.GetTypeByMetadataName("System.NonSerializedAttribute");
            if (null == _ISerializable || null == _SerializationInfo || null == _StreamingContext || null == _NonSerializedAttr) {
               Log.WarnFormat("Could not get serialization types. Result for \"{0}\" might be incorrect for \"{1}\".", GetRuleName(), context.Compilation.AssemblyName);
            }

            context.RegisterSymbolAction(OnNamedType, SymbolKind.NamedType);
         }
      }

      private class Data
      {
         public static readonly string GetObjectDataStr = "GetObjectData";
         public bool ImplementsISerializable { get; private set; }
         public bool HasSerializableAttribute { get; private set; }
         public bool HasSerializableFields { get; private set; }
         public bool BaseImplementsISerializable { get; private set; }
         public bool Violationable /*new word in english language */ { get; private set; }
         public FileLinePositionSpan Position { get; private set; }
         public Tuple<IMethodSymbol, bool> SerializableCtor { get; private set; }
         public Tuple<IMethodSymbol, bool> GetObjectData { get; private set; }
         public List<IFieldSymbol> NonSerialiazbleFieldsNotMarked { get; private set; }

         public Data(INamedTypeSymbol type,
            INamedTypeSymbol iSerializable,
            INamedTypeSymbol serializationInfo,
            INamedTypeSymbol streamingContext,
            INamedTypeSymbol nonSerializedAttr)
         {

            HasSerializableAttribute = type.IsSerializable;
            ImplementsISerializable = type.Interfaces.Contains(iSerializable);
            HasSerializableFields = false;

            if (HasSerializableAttribute || ImplementsISerializable) {
               Violationable = true;
               Dictionary<string, IFieldSymbol> fields = new Dictionary<string, IFieldSymbol>();
               var members = type.GetMembers();
               foreach (var member in members) {
                  if (!member.IsImplicitlyDeclared) {
                     if (SymbolKind.Method == member.Kind) {
                        var method = member as IMethodSymbol;
                        if (2 == method.Parameters.Length && serializationInfo == method.Parameters.ElementAt(0).Type && streamingContext == method.Parameters.ElementAt(1).Type) {
                           if (MethodKind.Constructor == method.MethodKind) {
                              SerializableCtor = new Tuple<IMethodSymbol, bool>(method, ((type.IsSealed && Accessibility.Private != method.DeclaredAccessibility) ||
                                 (!type.IsSealed && Accessibility.Protected != method.DeclaredAccessibility)));
                           } else if (MethodKind.Ordinary == method.MethodKind && GetObjectDataStr == method.Name) {
                              bool addViolation = ((!type.IsSealed && !method.IsOverride && !method.IsVirtual) || Accessibility.Public != method.DeclaredAccessibility);
                              GetObjectData = new Tuple<IMethodSymbol, bool>(method, addViolation);
                           }
                        }
                     } else if (SymbolKind.Field == member.Kind && null != nonSerializedAttr) {
                        if (null == member.GetAttributes().FirstOrDefault(a => nonSerializedAttr == a.AttributeClass)) {
                           fields.Add(member.Name, member as IFieldSymbol);
                           HasSerializableFields = true;
                        }
                     }
                  }
               }

               BaseImplementsISerializable = null != type.BaseType && type.BaseType.AllInterfaces.Contains(iSerializable);
               if (null != GetObjectData) {
                  var syntax = GetObjectData.Item1.GetImplemenationSyntax();
                  bool callsBaseGetObjectData = !BaseImplementsISerializable;

                  if (null != syntax) {
                     foreach (var node in syntax.DescendantNodes()) {
                        if (!GetObjectData.Item2 && BaseImplementsISerializable && node is InvocationExpressionSyntax) {
                           callsBaseGetObjectData = node.ToString().StartsWith("base.GetObjectData");
                        } else if (node is IdentifierNameSyntax) {
                           var identifierSyntax = node as IdentifierNameSyntax;
                           fields.Remove(identifierSyntax.Identifier.ValueText);
                        }
                     }
                     NonSerialiazbleFieldsNotMarked = fields.Values.ToList();
                  }

                  if (!callsBaseGetObjectData) {
                     GetObjectData = new Tuple<IMethodSymbol, bool>(GetObjectData.Item1, true);
                  }
               }

               if (null != SerializableCtor && !SerializableCtor.Item2 && BaseImplementsISerializable) {
                  var syntax = SerializableCtor.Item1.GetImplemenationSyntax();
                  if (null != syntax) {
                     bool callsBaseCtor = null != syntax.DescendantNodes().FirstOrDefault(n => n.ToString().Contains("base("));
                     if (!callsBaseCtor) {
                        SerializableCtor = new Tuple<IMethodSymbol, bool>(SerializableCtor.Item1, true);
                     }
                  }
               }
            }
         }
      }

      private void OnNamedType(SymbolAnalysisContext context)
      {
         try {

            var data =
               _symbolToData.GetOrAdd(context.Symbol as INamedTypeSymbol, (key) => new Data(key, _ISerializable, _SerializationInfo, _StreamingContext, _NonSerializedAttr));

            if (data.Violationable) {

               if (((data.ImplementsISerializable && !data.HasSerializableAttribute) || null == data.SerializableCtor) ||
                  (null == data.GetObjectData && data.BaseImplementsISerializable && data.HasSerializableFields)) {
                  List<FileLinePositionSpan> positions = new List<FileLinePositionSpan>();
                  foreach (var synRef in context.Symbol.DeclaringSyntaxReferences) {
                     positions.Add(synRef.GetSyntax().GetLocation().GetMappedLineSpan());
                  }
                  AddViolation(context.Symbol, positions);
               }

               if (null != data.SerializableCtor && data.SerializableCtor.Item2) {
                  AddViolation(data.SerializableCtor.Item1, new[] { data.SerializableCtor.Item1.GetImplemenationSyntax().GetLocation().GetMappedLineSpan()});
               }

               if (null != data.GetObjectData && data.GetObjectData.Item2) {
                  AddViolation(data.SerializableCtor.Item1, new[] { data.GetObjectData.Item1.GetImplemenationSyntax().GetLocation().GetMappedLineSpan() });
               }

               if (null != data.NonSerialiazbleFieldsNotMarked) {
                  
                  foreach (var field in data.NonSerialiazbleFieldsNotMarked) {
                     List<FileLinePositionSpan> positions = new List<FileLinePositionSpan>();
                     foreach (var synRef in field.DeclaringSyntaxReferences) {
                        positions.Add(synRef.GetSyntax().GetLocation().GetMappedLineSpan());
                     }
                     AddViolation(field, positions);
                  }
               }
            }

         } catch (Exception e) {
            Log.Warn("Exception while analyzing " + context.Symbol.OriginalDefinition.ToString(), e);
         }
      }
   }
}
