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
         var syntax = methodDeclaration.GetSyntax() as MethodDeclarationSyntax;
         var signature = syntax.ToString();
         bool newedMethod = false;
         if (null != syntax) {
            foreach (var token in syntax.Modifiers) {
               if ("new" == token.ToString()) {
                  newedMethod = true;
                  break;
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

      private void AnalyzeClass(SymbolAnalysisContext context) {
         lock (_lock) {
            var klazz = context.Symbol as INamedTypeSymbol;
            if (null != klazz) {
               if (null != klazz.BaseType && "Object" != klazz.BaseType.Name) {
                  Dictionary<string, IMethodSymbol> methods = new Dictionary<string, IMethodSymbol>();
                  foreach (var member in klazz.GetMembers()) {
                     var method = member as IMethodSymbol;
                     if (null != method && !method.IsVirtual && !method.IsOverride) {
                        string signature = GetMethodSignature(method);
                        if (null != signature && !isNewedMethod(method)) {
                           methods[signature] = method;
                        }
                     }
                  }

                  if (methods.Any()) {
                     do {
                        klazz = klazz.BaseType;
                        if (null == klazz || klazz.ToString().Equals("Object") || klazz.ToString().Equals("System.Object")) {
                           break;
                        }
                        foreach (var member in klazz.GetMembers()) {
                           var method = member as IMethodSymbol;
                           if (null != method) {
                              var signature = GetMethodSignature(method);
                              if (null != signature && methods.ContainsKey(signature)) {
                                 var sourceMethod = methods[signature];
                                 if (sourceMethod.DeclaredAccessibility != method.DeclaredAccessibility) {
                                    bool addViolation = false;
                                    switch (method.DeclaredAccessibility) {
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
                                       var additionalPos = method.Locations.FirstOrDefault().GetMappedLineSpan();
                                       //Console.WriteLine("Method: " + method.Name + " MainPos " + mainPos.ToString() + " Additional Pos: " + additionalPos.ToString());
                                       AddViolation(context.Symbol, new List<FileLinePositionSpan>() { mainPos, additionalPos });
                                    }
                                 }
                                 methods.Remove(signature);
                              }
                           }
                        }
                     } while (null != klazz && !klazz.ToString().Equals("Object"));


                  }
               }
            }
         }
      }
   }
}
