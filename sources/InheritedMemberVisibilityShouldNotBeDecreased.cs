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
       Id = "EI_InheritedMemberVisibilityShouldNotBeDecreased",
       Title = "Inherited member visibility should not be decreased",
       MessageFormat = "Inherited member visibility should not be decreased",
       Category = "Programming Practices - OO Inheritance and Polymorphism",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.InheritedMemberVisibilityShouldNotBeDecreased"
   )]
   public class InheritedMemberVisibilityShouldNotBeDecreased : AbstractRuleChecker {

      private readonly Dictionary<string, Dictionary<string, IMethodSymbol>> _klazzToMembers
         = new Dictionary<string, Dictionary<string, IMethodSymbol>>();

      public InheritedMemberVisibilityShouldNotBeDecreased()
            : base(ViolationCreationMode.ViolationWithAdditionalBookmarks)
        {
        }

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         //TODO: register for events
         context.RegisterSymbolAction(AnalyzeClass, SymbolKind.NamedType);
      }

      private static bool isNewedMethod(IMethodSymbol method)
      {
         var methodDeclaration = method.DeclaringSyntaxReferences.FirstOrDefault();
         bool newedMethod = false;
         if (null != methodDeclaration) {
            var syntax = methodDeclaration.GetSyntax() as MethodDeclarationSyntax;
            //var signature = syntax.ToString();
            if (null != syntax) {
               newedMethod = syntax.Modifiers.FirstOrDefault(t => t.IsKind(SyntaxKind.NewKeyword)).
                  //just silly. First will throw if not found. FirstOrDefault will return SyntaxKind.None.
                  IsKind(SyntaxKind.NewKeyword);
            }
         }
         return newedMethod;
      }

      private static string GetMethodSignature(IMethodSymbol method)
      {
         string signature = method.OriginalDefinition.ToString();
         int index = signature.LastIndexOf("." + method.Name, StringComparison.Ordinal);
         if (-1 != index) {
            signature = signature.Substring(index);
         }
         else {
            signature = null;
         }

         return signature;
      }

      private readonly object _lock = new object();

      private Dictionary<string, IMethodSymbol> RetrieveClassMethods(INamedTypeSymbol klazz) {
         Dictionary<string, IMethodSymbol> methods = null;
         if (null != klazz) {
            var fullname = klazz.OriginalDefinition.ToString();
            if (!_klazzToMembers.TryGetValue(fullname, out methods)) {
               methods = new Dictionary<string, IMethodSymbol>();
               _klazzToMembers[fullname] = methods;

               foreach (var member in klazz.GetMembers()) {
                  var method = member as IMethodSymbol;
                  if (null != method && !method.IsVirtual && !method.IsOverride) {
                     string signature = GetMethodSignature(method);
                     if (null != signature && !isNewedMethod(method)) {
                        methods[signature] = method;
                     }
                  }
               }
            }
         }
         else {
            methods = new Dictionary<string, IMethodSymbol>();
         }

         return methods;
      }

      private void AnalyzeClass(SymbolAnalysisContext context) {
         lock (_lock) {
            try {
               var klazz = context.Symbol as INamedTypeSymbol;
               if (null != klazz && klazz.TypeKind == TypeKind.Class) {
                  Dictionary<string, IMethodSymbol> methods = RetrieveClassMethods(klazz);
                  if (null != methods && methods.Any()) {
                     HashSet<string> foundMethods = new HashSet<string>();
                     do {
                        klazz = klazz.BaseType;
                        if (null != klazz && TypeKind.Class == klazz.TypeKind) {
                           var baseFullName = klazz.OriginalDefinition.ToString();
                           if (klazz.ToString().Equals("Object") || klazz.ToString().Equals("System.Object") || "object" == baseFullName) {
                              break;
                           }

                           var baseMethods = RetrieveClassMethods(klazz);
                           foreach (var aMethod in methods) {
                              IMethodSymbol targetMethod = null;

                              if (!foundMethods.Contains(aMethod.Key) && baseMethods.TryGetValue(aMethod.Key, out targetMethod)) {
                                 foundMethods.Add(aMethod.Key);
                                 IMethodSymbol sourceMethod = aMethod.Value;
                                 if (sourceMethod.DeclaredAccessibility != targetMethod.DeclaredAccessibility) {
                                    bool addViolation = false;
                                    switch (targetMethod.DeclaredAccessibility) {
                                       case Accessibility.Public:
                                          addViolation = true;
                                          break;
                                       case Accessibility.Protected:
                                          if (Accessibility.Private == sourceMethod.DeclaredAccessibility) {
                                             addViolation = true;
                                          }
                                          break;
                                       case Accessibility.Internal:
                                          if (Accessibility.Private == sourceMethod.DeclaredAccessibility ||
                                             Accessibility.Protected == sourceMethod.DeclaredAccessibility) {
                                             addViolation = true;
                                          }
                                          break;
                                    }

                                    if (addViolation) {
                                       var mainPos = sourceMethod.Locations.FirstOrDefault().GetMappedLineSpan();
                                       var additionalPos = targetMethod.Locations.FirstOrDefault().GetMappedLineSpan();
                                       //Log.WarnFormat("Method: {0} MainPos {1} Additional Pos: {2}", method.Name, mainPos.ToString(), additionalPos.ToString());
                                       AddViolation(sourceMethod, new List<FileLinePositionSpan> { mainPos, additionalPos });
                                    }

                                 }
                              }
                           }
                        }

                     } while (null != klazz && !klazz.ToString().Equals("Object"));
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
         


      public override void Reset() {
         lock (_lock) {
            try {
               _klazzToMembers.Clear();
               base.Reset();
            }
            catch (Exception e) {
               Log.Warn("Exception while analyzing during reset", e);
            }
         }
      }

   }
}

