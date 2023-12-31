﻿using System;
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

         /*lock (_lock)*/ {
            try {
               var type = context.Symbol as INamedTypeSymbol;
               if (null != type) {
                  var systemArray = context.Compilation.GetTypeByMetadataName("System.Array");
                  var systemGenericICollection = context.Compilation.GetTypeByMetadataName("System.Collections.Generic.ICollection`1");
                  var systemCollectionsICollection = context.Compilation.GetTypeByMetadataName("System.Collections.ICollection");
                  if (TypeKind.Class == type.TypeKind || TypeKind.Struct == type.TypeKind) 
                  {
                     var targetFields = type.GetMembers().
                        OfType<IFieldSymbol>().
                        Where(field => !field.IsReadOnly &&
                           Accessibility.Public == field.DeclaredAccessibility &&
                           field.IsStatic &&
                           (
                              field.Type.BaseType == systemArray ||
                              (
                                 null != systemGenericICollection && field.Type.AllInterfaces.Contains(systemGenericICollection) ||
                                 null != systemCollectionsICollection && field.Type.AllInterfaces.Contains(systemCollectionsICollection)
                              )
                              &&
                              !field.Type.OriginalDefinition.ToString().StartsWith("System.Collections.ObjectModel.ReadOnly")
                           )
                           );

                     foreach (IFieldSymbol field in targetFields) 
                     {
                        var pos = field.Locations.FirstOrDefault().GetMappedLineSpan();
                        AddViolation(context.Symbol, new[] { pos });
                     }

                     // exception case : fields read-only with inline initialization with an immutable type
                     var targetFieldsExceptions = type.GetMembers().
                     OfType<IFieldSymbol>().
                     Where(field => field.IsReadOnly &&
                        Accessibility.Public == field.DeclaredAccessibility &&
                        field.IsStatic &&
                        (
                           field.Type.BaseType == systemArray ||
                           (
                              null != systemGenericICollection && field.Type.AllInterfaces.Contains(systemGenericICollection) ||
                              null != systemCollectionsICollection && field.Type.AllInterfaces.Contains(systemCollectionsICollection)
                           )
                           &&
                           !field.Type.OriginalDefinition.ToString().StartsWith("System.Collections.ObjectModel.ReadOnly")
                        )
                        );
                      var model = context.Compilation.GetSemanticModel(context.Compilation.SyntaxTrees.ToList()[0]);
                      foreach(IFieldSymbol field in targetFieldsExceptions)
                      {
                          foreach(var syntRef in field.DeclaringSyntaxReferences)
                          {
                              var node = syntRef.GetSyntax() as Microsoft.CodeAnalysis.CSharp.Syntax.VariableDeclaratorSyntax;
                              if(node!=null)
                              {
                                  var objCreationNode = node.Initializer.Value as Microsoft.CodeAnalysis.CSharp.Syntax.ObjectCreationExpressionSyntax;
                                  if(objCreationNode!=null)
                                  {
                                      var strObjCreatType = objCreationNode.Type.ToString();
                                      if (!strObjCreatType.Contains("ReadOnly") &&
                                         !strObjCreatType.Contains("Immutable"))
                                      {
                                          var pos = field.Locations.FirstOrDefault().GetMappedLineSpan();
                                          AddViolation(context.Symbol, new[] { pos });
                                      }
                                  }
                                  else
                                  {
                                      
                                      var castNode = node.Initializer.Value as Microsoft.CodeAnalysis.CSharp.Syntax.CastExpressionSyntax;
                                      if(castNode!=null)
                                      {
                                          var identifierNode = castNode.Expression as Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax;
                                          if(identifierNode!=null)
                                          {                                             
                                              var typeInfo = model.GetTypeInfo(identifierNode);
                                              var convertedType = typeInfo.ConvertedType;
                                              if (convertedType != null)
                                              {
                                                  var stringType = convertedType.ToString();
                                                  if (!stringType.Contains("ReadOnly") && !stringType.Contains("Immutable"))
                                                  {
                                                      var pos = field.Locations.FirstOrDefault().GetMappedLineSpan();
                                                      AddViolation(context.Symbol, new[] { pos });
                                                  }
                                              }
                                          }
                                      }
                                      else
                                      {
                                          var identifierNode = node.Initializer.Value as Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax;
                                          if (identifierNode != null)
                                          {
                                              var typeInfo = model.GetTypeInfo(identifierNode);
                                              var convertedType = typeInfo.ConvertedType;
                                              if (convertedType != null)
                                              {
                                                  var stringType = convertedType.ToString();
                                                  if (!stringType.Contains("ReadOnly") && !stringType.Contains("Immutable"))
                                                  {
                                                      var pos = field.Locations.FirstOrDefault().GetMappedLineSpan();
                                                      AddViolation(context.Symbol, new[] { pos });
                                                  }
                                              }
                                          }
                                      }
                                  }
                              }
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
