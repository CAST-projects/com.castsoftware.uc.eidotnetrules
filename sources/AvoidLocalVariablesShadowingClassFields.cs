using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.DotNet.CastDotNetExtension;
using Roslyn.DotNet.Common;

namespace CastDotNetExtension {
   [CastRuleChecker]
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   [RuleDescription(
       Id = "EI_AvoidLocalVariablesShadowingClassFields",
       Title = "Local Variables Shadowing Class Fields",
       MessageFormat = "Avoid Local Variables Shadowing Class Fields",
       Category = "Programming Practices - Unexpected Behavior",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.AvoidLocalVariablesShadowingClassFields"
   )]
   public class AvoidLocalVariablesShadowingClassFields : AbstractRuleChecker {

      private Dictionary<string, Dictionary<string, ISymbol>> _klazzToMembers
         = new Dictionary<string, Dictionary<string, ISymbol>>();

      public AvoidLocalVariablesShadowingClassFields()
            : base(ViolationCreationMode.ViolationWithAdditionalBookmarks)
        {
        }

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         context.RegisterSyntaxNodeAction(AddViolationIfLocalVariableViolates, Microsoft.CodeAnalysis.CSharp.SyntaxKind.VariableDeclarator, Microsoft.CodeAnalysis.CSharp.SyntaxKind.EndOfFileToken);
      }

      private object _lock = new object();  

      protected void AddViolationIfLocalVariableViolates(SyntaxNodeAnalysisContext context) {
         lock (_lock) {
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
                              foreach (var member in type.GetMembers()) {
                                 var field = member as IFieldSymbol;
                                 if (null != field) {
                                    if (Microsoft.CodeAnalysis.SymbolKind.Field == field.Kind) {
                                       if (considerPrivateMembers || Accessibility.Private != field.DeclaredAccessibility) {
                                          fields[field.Name] = field;
                                       }
                                    }
                                 }
                              }
                              considerPrivateMembers = false;
                              type = type.BaseType;
                           } while (null != type && !type.ToString().Equals("object"));
                        }

                        if (fields.ContainsKey(name)) {
                           var pos = context.Node.GetLocation().GetMappedLineSpan();
                           //Log.WarnFormat("Thread ID: {0} Adding violation at {1}", System.Threading.Thread.CurrentThread.ManagedThreadId, pos.StartLinePosition.ToString());
                           AddViolation(context, new List<FileLinePositionSpan>() { pos });
                        }
                     }
                  }
               }
            }
            catch (Exception e) {
               Log.Warn(e.StackTrace);
               Log.WarnFormat("Exception in ... {0}", e.Message);
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
               Log.Warn("Exception during  AvoidLocalVariablesShadowingClassFields.Reset ", e);
            }
         }
      }
   }
}
