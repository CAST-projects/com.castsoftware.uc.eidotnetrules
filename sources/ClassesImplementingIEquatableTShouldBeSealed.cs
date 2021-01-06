﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Symbols;


namespace CastDotNetExtension {
   [CastRuleChecker]
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   [RuleDescription(
       Id = "EI_ClassesImplementingIEquatableTShouldBeSealed",
       Title = "Classes implementing \"IEquatable<T>\" should be sealed",
       MessageFormat = "Classes implementing \"IEquatable<T>\" should be sealed",
       Category = "Programming Practices",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.ClassesImplementingIEquatableTShouldBeSealed"
   )]
   public class ClassesImplementingIEquatableTShouldBeSealed : AbstractRuleChecker {
      public ClassesImplementingIEquatableTShouldBeSealed() {
      }

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         context.RegisterSymbolAction(this.AnalyzeClass, SymbolKind.NamedType);
      }

      private IEnumerable<IMethodSymbol> GetEqualsMethods(INamedTypeSymbol klazz, bool onlyOverride = true) {
         var methods = from IMethodSymbol aMethod in klazz.GetMembers("Equals")
                       where null != aMethod &&
                       aMethod.Kind == SymbolKind.Method &&
                       1 == aMethod.Parameters.Count() &&
                       (!onlyOverride || aMethod.IsOverride)
                       //!aMethod.IsVirtual && !aMethod.IsAbstract
                       //equatableType == ((IMethodSymbol)aMethod).Parameters.First().ToString()
                       select aMethod;

         if (null == methods) {
            methods = new List<IMethodSymbol>();
         }
         return methods;
      }


      private Object _lock = new Object();
      private void AnalyzeClass(SymbolAnalysisContext context) {
         lock (_lock) {
            var klazz = context.Symbol as INamedTypeSymbol;
            if (null != klazz && TypeKind.Class == klazz.TypeKind && !klazz.IsSealed && 
               (Accessibility.Protected == klazz.DeclaredAccessibility || Accessibility.Public == klazz.DeclaredAccessibility)) {
               foreach (var baseInterface in klazz.AllInterfaces) {
                  if ("System.IEquatable<T>" == baseInterface.OriginalDefinition.ToString()) {
                     var equalss = baseInterface.GetMembers().Where(member => member.Name == "Equals");
                     if (null != equalss && 1 == equalss.Count()) {
                        var equalsImplementation = klazz.FindImplementationForInterfaceMember(equalss.First()) as IMethodSymbol;
                        bool addViolation = false;
                        if (null != equalsImplementation && !equalsImplementation.IsAbstract) {
                           if (equalsImplementation.IsVirtual) {
                              if (klazz != equalsImplementation.ContainingType) {
                                 var thisEqualss = GetEqualsMethods(klazz);
                                 var thisEqualsImplementation = thisEqualss.Where(thisEqual => thisEqual.Parameters.First().ToString() == equalsImplementation.Parameters.First().ToString());
                                 if (1 == thisEqualsImplementation.Count() && thisEqualsImplementation.First().IsOverride) {
                                    addViolation = true;
                                 }
                              }
                           }
                           else {
                              addViolation = true;
                           }

                           if (addViolation) {
                              var span = klazz.DeclaringSyntaxReferences.First().Span;
                              var pos = klazz.DeclaringSyntaxReferences.First().SyntaxTree.GetMappedLineSpan(span);
                              //Console.WriteLine("Violation: Class Name: " + klazz.Name + " .AddExpected(" + pos.StartLinePosition.Line + ", " + pos.StartLinePosition.Character + ")");
                              AddViolation(klazz, new FileLinePositionSpan[] { pos });
                           }
                           else {
                              //Console.WriteLine("No Violation: Class Name: " + klazz.Name);
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