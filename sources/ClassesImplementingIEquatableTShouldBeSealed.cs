using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.DotNet.CastDotNetExtension;
using Roslyn.DotNet.Common;


namespace CastDotNetExtension {
   [CastRuleChecker]
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   [RuleDescription(
       Id = "EI_ClassesImplementingIEquatableTShouldBeSealed",
       Title = "Classes implementing \"IEquatable<T>\" should be sealed",
       MessageFormat = "Classes implementing \"IEquatable<T>\" should be sealed",
       Category = "Programming Practices - OO Inheritance and Polymorphism",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.ClassesImplementingIEquatableTShouldBeSealed"
   )]
   public class ClassesImplementingIEquatableTShouldBeSealed : AbstractRuleChecker {

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         context.RegisterSymbolAction(AnalyzeClass, SymbolKind.NamedType);
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


      private readonly Object _lock = new Object();
      private void AnalyzeClass(SymbolAnalysisContext context) {
         lock (_lock) {
            try { 
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
                                 //Log.WarnFormat("Violation: Class Name: {0}.AddExpected({1}, {2})", klazz.Name, pos.StartLinePosition.Line, pos.StartLinePosition.Character);
                                 AddViolation(klazz, new FileLinePositionSpan[] { pos });
                              }
                              else {
                                 //Log.WarnFormat("No Violation: Class Name: {0}", klazz.Name);
                              }
                           }
                        }
                     }
                  }
               }
            }
            catch (System.Exception e) {
               HashSet<string> filePaths = new HashSet<string>();
               foreach (var synRef in context.Symbol.DeclaringSyntaxReferences) {
                  filePaths.Add(synRef.SyntaxTree.FilePath);
               }
               Log.Warn("Exception while analyzing " + String.Join(",", filePaths) + ": " + context.Symbol.Locations.FirstOrDefault().GetMappedLineSpan(), e);
            }
         }
      }
   }
}
