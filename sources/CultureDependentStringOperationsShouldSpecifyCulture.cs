using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using CastDotNetExtension.Utils;
using Roslyn.DotNet.CastDotNetExtension;


namespace CastDotNetExtension {
   [CastRuleChecker]
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   [RuleDescription(
       Id = "EI_CultureDependentStringOperationsShouldSpecifyCulture",
       Title = "Culture Dependent String operations should specify culture",
       MessageFormat = "Culture Dependent String operations should specify culture",
       Category = "Programming Practices - Unexpected Behaviour",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.CultureDependentStringOperationsShouldSpecifyCulture"
   )]
   public class CultureDependentStringOperationsShouldSpecifyCulture : AbstractRuleChecker {

      private static readonly HashSet<string> MethodNames = new HashSet<string> { 
         "string.Compare(string, string)",
         "string.Compare(string, string, bool)",
         "string.Compare(string, int, string, int, int)",
         "string.Compare(string, int, string, int, int, bool)",
         "string.CompareTo(object)",
         "string.CompareTo(string)",
         "string.IndexOf(char)",
         "string.IndexOf(char, int)",
         "string.IndexOf(char, int, int)",
         "string.IndexOf(string)",
         "string.IndexOf(string, int)",
         "string.IndexOf(string, int, int)",
         "string.LastIndexOf(char)",
         "string.LastIndexOf(char, int)",
         "string.LastIndexOf(char, int, int)",
         "string.LastIndexOf(string)",
         "string.LastIndexOf(string, int)",
         "string.LastIndexOf(string, int, int)",
         "string.ToLower()",
         "string.ToUpper()",                                                     
      };

      private HashSet<IMethodSymbol> _methodSymbols = null;
      private HashSet<INamedTypeSymbol> _cultureArgTypes = new HashSet<INamedTypeSymbol>();

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         context.RegisterSyntaxNodeAction(Analyze, Microsoft.CodeAnalysis.CSharp.SyntaxKind.InvocationExpression);
      }

      private readonly object _lock = new object();

      protected void Analyze(SyntaxNodeAnalysisContext context) {
            try {
               Init(context.Compilation);
               if (_methodSymbols.Any()) {
                  var method = context.IsOneOfMethods(_methodSymbols);
                  if (null != method) {
                     var span = context.Node.Span;
                     var pos = context.Node.SyntaxTree.GetMappedLineSpan(span);
                     AddViolation(context.ContainingSymbol, new FileLinePositionSpan[] { pos });
                  }
               }
            }
            catch (Exception e) {
               Log.Warn(" Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath, e);
            }
      }

      private IAssemblySymbol _mscorlib = null;

      private void Init(Compilation compilation) {
         lock (_lock) {
            compilation.GetMethodSymbolsForSystemClass("System.String", MethodNames, ref _mscorlib, ref _methodSymbols, true, MethodNames.Count);
         }
      }

   }
}
