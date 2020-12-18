using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading;

namespace CastDotNetExtension {
   [CastRuleChecker]
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   [RuleDescription(
       Id = "EI_AvoidLocalVariablesShadowingClassFields",
       Title = "Local Variables Shadowing Class Fields",
       MessageFormat = "Avoid Local Variables Shadowing Class Fields",
       Category = "Unexpected Behavior",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "DotNetQualityRules.AvoidLocalVariablesShadowingClassFields"
   )]
   public class AvoidLocalVariablesShadowingClassFields : AbstractRuleChecker {

      private Dictionary<string, Dictionary<string, ISymbol>> _klazzToMembers
         = new Dictionary<string, Dictionary<string, ISymbol>>();

      public AvoidLocalVariablesShadowingClassFields() {
      }

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         context.RegisterSyntaxNodeAction(AddViolationIfLocalVariableViolates, Microsoft.CodeAnalysis.CSharp.SyntaxKind.VariableDeclarator, Microsoft.CodeAnalysis.CSharp.SyntaxKind.EndOfFileToken);
      }

      private object _fieldLock = new object();  

      protected void AddViolationIfLocalVariableViolates(SyntaxNodeAnalysisContext context) {
         lock (_fieldLock) {
            try {
               if (Microsoft.CodeAnalysis.SymbolKind.Method == context.ContainingSymbol.Kind) {
                  var csharpNode = context.Node as Microsoft.CodeAnalysis.CSharp.Syntax.VariableDeclaratorSyntax;
                  string name = csharpNode.Identifier.ValueText;
                  if (null != name) {
                     var type = context.ContainingSymbol.ContainingType as INamedTypeSymbol;
                     if (null != type) {
                        string fullname = type.OriginalDefinition.ToString();
                        Dictionary<string, ISymbol> fields = null;
                        if (!_klazzToMembers.TryGetValue(fullname, out fields)) {
                           fields = new Dictionary<string, ISymbol>();
                           _klazzToMembers[fullname] = fields;
                           string baseName = type.Name;
                           bool considerPrivateMembers = true;
                           do {
                              foreach (var field in type.GetMembers()) {
                                 if (Microsoft.CodeAnalysis.SymbolKind.Field == field.Kind) {
                                    if (considerPrivateMembers || Accessibility.Private != field.DeclaredAccessibility) {
                                       fields[field.Name] = field;
                                    }
                                 }
                              }
                              considerPrivateMembers = false;
                              type = type.BaseType;
                           } while (null != type && !type.ToString().Equals("object"));
                        }

                        if (fields.ContainsKey(name)) {
                           var pos = context.Node.GetLocation().GetMappedLineSpan();
                           //Console.WriteLine("Thread ID: " + System.Threading.Thread.CurrentThread.ManagedThreadId +
                           //   " Adding violation at " + pos.StartLinePosition.ToString());
                           AddViolation(context, new List<FileLinePositionSpan>() { pos });
                        }
                     }
                  }
               }
            }
            catch (Exception e) {
               Console.WriteLine(e.StackTrace);
               System.Console.WriteLine("Exception in ... " + e.Message);
            }
         }
      }

      public override void Reset() {
         base.Reset();
         _klazzToMembers.Clear();
      }
   }
}
