using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.DotNet.CastDotNetExtension;
using Roslyn.DotNet.Common;


namespace CastDotNetExtension
{
   [CastRuleChecker]
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   [RuleDescription(
       Id = "EI_MembersOfLargerScopeElementShouldNotHaveConflictingTransparencyAnnotations",
       Title = "Members of larger scope element should not have conflicting transparency annotations",
       MessageFormat = "Members of larger scope element should not have conflicting transparency annotations",
       Category = "Secure Coding - API Abuse",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.MembersOfLargerScopeElementShouldNotHaveConflictingTransparencyAnnotations"
   )]
   public class MembersOfLargerScopeElementShouldNotHaveConflictingTransparencyAnnotations : AbstractRuleChecker
   {
      private static readonly string[] SECURITY_ATTRIBUTE_CLASSES = new[] {
         "System.Security.SecurityCriticalAttribute",
         "System.Security.SecurityRulesAttribute",
         "System.Security.SecuritySafeCriticalAttribute",
         "System.Security.SecurityTransparentAttribute",
         "System.Security.SecurityTreatAsSafeAttribute",
         "System.Security.SuppressUnmanagedCodeSecurityAttribute",
         "System.Security.UnverifiableCodeAttribute",
      };

      public class AllSymbolsSecurityAttrVisitor : SymbolVisitor
      {

         private HashSet<INamedTypeSymbol> _securityAttributeSymbols = new HashSet<INamedTypeSymbol>();
         private Dictionary<ISymbol, List<AttributeData>> _allSecurityAttrsSoFar = new Dictionary<ISymbol, List<AttributeData>>();
         public Dictionary<ISymbol, Tuple<AttributeData, AttributeData>> Violations { get; private set; }

         private enum ProcessInstruction
         {
            GetSecurityAttr,
            RemoveSecurityAttr,
            Both
         }

         public AllSymbolsSecurityAttrVisitor(HashSet<INamedTypeSymbol> securityAttributeSymbols)
         {
            System.Diagnostics.Debug.Assert(null != securityAttributeSymbols && securityAttributeSymbols.Any());
            _securityAttributeSymbols = securityAttributeSymbols;
            Violations = new Dictionary<ISymbol, Tuple<AttributeData, AttributeData>>();
         }


         private ISymbol ProcessSecurityAttribute(ISymbol symbol, ProcessInstruction instruction) {
            var forDebugging = symbol;
            if (ProcessInstruction.RemoveSecurityAttr != instruction) {
               var attrs = symbol.GetAttributes();
               var attr = attrs.Any() ? symbol.GetAttributes().FirstOrDefault(a => _securityAttributeSymbols.Contains(a.AttributeClass)) : null;

               if (null != attr) {
                  if (!_allSecurityAttrsSoFar.ContainsKey(attr.AttributeClass)) {
                     if (_allSecurityAttrsSoFar.Any()) {
                        Violations.Add(symbol, new Tuple<AttributeData, AttributeData>(attr, _allSecurityAttrsSoFar.Last().Value.Last()));
                     }
                     _allSecurityAttrsSoFar[attr.AttributeClass] = new List<AttributeData>();
                  } 
                  _allSecurityAttrsSoFar[attr.AttributeClass].Add(attr);
                  symbol = attr.AttributeClass;
               } else if (ProcessInstruction.GetSecurityAttr != instruction) {
                  symbol = null;
               }
            } 
            
            if (null != symbol && ProcessInstruction.GetSecurityAttr != instruction) {
               if (_allSecurityAttrsSoFar.ContainsKey(symbol)) {
                  if (1 == _allSecurityAttrsSoFar[symbol].Count) {
                     _allSecurityAttrsSoFar.Remove(symbol);
                  } else {
                     _allSecurityAttrsSoFar[symbol].Remove(_allSecurityAttrsSoFar[symbol].Last());
                  }
               }
            }

            return symbol;
         }


         public override void VisitAssembly(IAssemblySymbol symbol)
         {
            ProcessSecurityAttribute(symbol, ProcessInstruction.GetSecurityAttr);
            symbol.GlobalNamespace.Accept(this);
            ProcessSecurityAttribute(symbol, ProcessInstruction.RemoveSecurityAttr);
         }

         public override void VisitNamespace(INamespaceSymbol symbol)
         {
            foreach (var member in symbol.GetMembers()) {
               member.Accept(this);
            }
         }

         public override void VisitNamedType(INamedTypeSymbol symbol)
         {
            var attr = ProcessSecurityAttribute(symbol, ProcessInstruction.GetSecurityAttr);
            foreach (var member in symbol.GetMembers()) {
               if (SymbolKind.NamedType != member.Kind) {
                  ProcessSecurityAttribute(member, ProcessInstruction.Both);
               } else {
                  member.Accept(this);
               }
            }
            ProcessSecurityAttribute(attr, ProcessInstruction.RemoveSecurityAttr);
         }
      }

      public MembersOfLargerScopeElementShouldNotHaveConflictingTransparencyAnnotations() :
         base (ViolationCreationMode.ViolationWithAdditionalBookmarks)
      {
      }

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context)
      {
         context.RegisterCompilationAction(OnCompilationStart);
      }

      private void OnCompilationStart(CompilationAnalysisContext context)
      {
         HashSet<INamedTypeSymbol> securityAttributeSymbols = new HashSet<INamedTypeSymbol>();
         foreach (var attrClassName in SECURITY_ATTRIBUTE_CLASSES) {
            INamedTypeSymbol type = context.Compilation.GetTypeByMetadataName(attrClassName);
            if (null != type) {
               securityAttributeSymbols.Add(type);
            }
         }

         if (!securityAttributeSymbols.Any()) {
            Log.InfoFormat("Could not get symbols for security attributes. \"{0}\" will be disabled for \"{1}\"",
               GetRuleName(), context.Compilation.Assembly);
         } else {
            if (securityAttributeSymbols.Count != SECURITY_ATTRIBUTE_CLASSES.Length) {
               Log.WarnFormat("Could not get one or more symbols for security attributes. \"{0}\" may have incorrect results for \"{1}\"",
                  GetRuleName(), context.Compilation.Assembly);
            }

            try {
               AllSymbolsSecurityAttrVisitor visitor = new AllSymbolsSecurityAttrVisitor(securityAttributeSymbols);
               visitor.Visit(context.Compilation.Assembly);
               foreach (var violation in visitor.Violations) {
                  AddViolation(violation.Key, new[] { violation.Value.Item1.ApplicationSyntaxReference.GetSyntax().GetLocation().GetMappedLineSpan(), violation.Value.Item2.ApplicationSyntaxReference.GetSyntax().GetLocation().GetMappedLineSpan() });
               }
            } catch (Exception e) {
               Log.Warn("Exception while analyzing " + context.Compilation.Assembly.Name, e);
            }
         }
      }

      private void OnSymbol(SymbolAnalysisContext context)
      {

      }

   }
}
