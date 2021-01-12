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
       Id = "TODO: Add prefix_AvoidUsing_Assembly_LoadFrom_Assembly_LoadFileAndAssembly_LoadWithPartialName",
       Title = "TODO: Add Title",
       MessageFormat = "TODO: Add Message",
       Category = "TODO: Add Category",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.AvoidUsing_Assembly_LoadFrom_Assembly_LoadFileAndAssembly_LoadWithPartialName"
   )]
   public class AvoidUsing_Assembly_LoadFrom_Assembly_LoadFileAndAssembly_LoadWithPartialName : AbstractRuleChecker {

      protected enum CompilationType {
         None,
         CSharp,
         VisualBasic
      }

      private static readonly string[] MethodNames =
        { 
            "LoadFrom",
            "LoadFile",
            "LoadWithPartialName"
        };

      private static HashSet<IMethodSymbol> _methodSymbols = null;
      public AvoidUsing_Assembly_LoadFrom_Assembly_LoadFileAndAssembly_LoadWithPartialName() {
      }

      private CompilationType _typeCompilation = CompilationType.None;

      protected bool isChangedCompilation(bool isCsharpCompilation) {
         if (_typeCompilation == CompilationType.CSharp && !isCsharpCompilation) {
            _typeCompilation = CompilationType.VisualBasic;
            return true;
         }

         if (_typeCompilation == CompilationType.VisualBasic && isCsharpCompilation) {
            _typeCompilation = CompilationType.CSharp;
            return true;
         }

         if (_typeCompilation == CompilationType.None) {
            _typeCompilation = isCsharpCompilation ? CompilationType.CSharp : CompilationType.VisualBasic;
            return true;
         }

         return false;
      }



      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         //TODO: register for events
         context.RegisterSyntaxNodeAction(Analyze, Microsoft.CodeAnalysis.CSharp.SyntaxKind.InvocationExpression);
      }

      protected void Analyze(SyntaxNodeAnalysisContext context) {
         try {
            Init(context.Compilation);
            var model = context.SemanticModel;
            var symbInf = model.GetSymbolInfo(context.Node);
            var invokedMethod = symbInf.Symbol as IMethodSymbol;// get invocation method symbol   
            if (_methodSymbols.Contains(invokedMethod)) {
               var span = context.Node.Span;
               var pos = context.Node.SyntaxTree.GetMappedLineSpan(span);
               //Console.WriteLine(pos.ToString());
               AddViolation(context.ContainingSymbol, new FileLinePositionSpan [] { pos });
            }
         }
         catch (System.Exception e) {
            System.Console.WriteLine(e.Message);
            System.Console.WriteLine(e.StackTrace);
         }
      }

      private void Init(Compilation compil) {
         bool changed = isChangedCompilation((compil as Microsoft.CodeAnalysis.CSharp.CSharpCompilation) != null);
         if (_methodSymbols == null || changed) {
            _methodSymbols = new HashSet<IMethodSymbol>();
            var assembly = compil.GetTypeByMetadataName("System.Reflection.Assembly") as INamedTypeSymbol;
            if (null != assembly) {
               _methodSymbols.UnionWith(assembly.GetMembers().OfType<IMethodSymbol>().Where(m => MethodNames.Contains(m.Name)));
            }
         }
      }
   }
}
