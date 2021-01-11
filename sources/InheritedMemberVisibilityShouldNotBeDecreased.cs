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
       Id = "EI_InheritedMemberVisibilityShouldNotBeDecreased",
       Title = "Inherited member visibility should not be decreased",
       MessageFormat = "Inherited member visibility should not be decreased",
       Category = "Unexpected Behavior",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.InheritedMemberVisibilityShouldNotBeDecreased"
   )]
   public class InheritedMemberVisibilityShouldNotBeDecreased : AbstractRuleChecker {

      private Dictionary<string, Dictionary<string, IMethodSymbol>> _klazzToMembers
         = new Dictionary<string, Dictionary<string, IMethodSymbol>>();

      public InheritedMemberVisibilityShouldNotBeDecreased() {
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

      private bool isNewedMethod(IMethodSymbol method) {
         var methodDeclaration = method.DeclaringSyntaxReferences.FirstOrDefault();
         bool newedMethod = false;
         if (null != methodDeclaration) {
            var syntax = methodDeclaration.GetSyntax() as MethodDeclarationSyntax;
            //var signature = syntax.ToString();
            if (null != syntax) {
               foreach (var token in syntax.Modifiers) {
                  if ("new" == token.ToString()) {
                     newedMethod = true;
                     break;
                  }
               }
            }
         }
         return newedMethod;
      }

      private string GetMethodSignature(IMethodSymbol method) {
         string signature = method.OriginalDefinition.ToString();
         int index = signature.LastIndexOf("." + method.Name);
         if (-1 != index) {
            signature = signature.Substring(index);
         }
         else {
            signature = null;
         }

         return signature;
      }

      private Object _lock = new Object();

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
            var klazz = context.Symbol as INamedTypeSymbol;
            if (null != klazz && klazz.TypeKind == TypeKind.Class) {
               Dictionary<string, IMethodSymbol> methods = RetrieveClassMethods(klazz);
               if (null != methods && methods.Any()) {
                  HashSet<String> foundMethods = new HashSet<string>();
                  do {
                     klazz = klazz.BaseType;
                     if (null != klazz && TypeKind.Class == klazz.TypeKind) {
                        var baseFullName = klazz.OriginalDefinition.ToString();
                        if (null == klazz || klazz.ToString().Equals("Object") || klazz.ToString().Equals("System.Object") || "object" == baseFullName) {
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
                                    //Console.WriteLine("Method: " + method.Name + " MainPos " + mainPos.ToString() + " Additional Pos: " + additionalPos.ToString());
                                    AddViolation(sourceMethod, new List<FileLinePositionSpan>() { mainPos, additionalPos });
                                 }

                              }
                           }
                        }
                     }
                              
                  } while (null != klazz && !klazz.ToString().Equals("Object"));
               }
            }
         }
      }
         


      public override void Reset() {
         base.Reset();
         _klazzToMembers.Clear();
      }

   }
}

