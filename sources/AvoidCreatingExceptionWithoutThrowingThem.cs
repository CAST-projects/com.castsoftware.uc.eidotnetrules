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
       Id = "EI_AvoidCreatingExceptionWithoutThrowingThem",
       Title = "Avoid creating exception without throwing them",
       MessageFormat = "Avoid creating exception without throwing them",
       Category = "Dead Code",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.AvoidCreatingExceptionWithoutThrowingThem"
   )]
   public class AvoidCreatingExceptionWithoutThrowingThem : AbstractRuleChecker {
      public AvoidCreatingExceptionWithoutThrowingThem() {
      }

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         context.RegisterSyntaxNodeAction(this.Analyze, SyntaxKind.ObjectCreationExpression, SyntaxKind.ThrowStatement);
         context.RegisterSemanticModelAction(this.SemenaticModelAnalysisEnd);
      }

      

      private Dictionary<SyntaxNode, ISymbol> _exceptionsNotThrown = new Dictionary<SyntaxNode, ISymbol>();
      private HashSet<SyntaxNode> _exceptionsThrown = new HashSet<SyntaxNode>();
      private Dictionary<String, Dictionary<SyntaxNode, ISymbol>> _exceptionsVars = new Dictionary<String, Dictionary<SyntaxNode, ISymbol>>();

      private object _lock = new object();
      private void Analyze(SyntaxNodeAnalysisContext context) {
         lock (_lock) {
            var newSyntax = context.Node as ObjectCreationExpressionSyntax;
            if (null != newSyntax) {
               if (!_exceptionsThrown.Contains(newSyntax)) {
                     var typeName = newSyntax.Type;
                     if (null != typeName) {
                        var type = context.SemanticModel.GetSymbolInfo(typeName).Symbol as INamedTypeSymbol;
                        if (null != type) {
                           INamedTypeSymbol systemException = context.Compilation.GetTypeByMetadataName("System.Exception");
                           if (null != systemException && context.Compilation.ClassifyConversion(type, systemException).IsImplicit) {
                              if (newSyntax.Parent is EqualsValueClauseSyntax) {
                                 if (newSyntax.Parent.Parent is VariableDeclaratorSyntax) {
                                    var variableDeclarator = newSyntax.Parent.Parent as VariableDeclaratorSyntax;
                                    var varName = variableDeclarator.Identifier.ToString();
                                    Dictionary<SyntaxNode, ISymbol> nodes = null;
                                    if (!_exceptionsVars.TryGetValue(varName, out nodes)) {
                                       nodes = new Dictionary<SyntaxNode, ISymbol>();
                                       _exceptionsVars[varName] = nodes;
                                    }
                                    nodes[newSyntax.Parent.Parent] = context.ContainingSymbol;
                                 }
                              }
                              else {
                                 _exceptionsNotThrown[context.Node] = context.ContainingSymbol;
                              }
                           }
                        }
                     }
               }
               else {
                  _exceptionsThrown.Remove(newSyntax);
               }
            }
            else {
               var throwStatement = context.Node as ThrowStatementSyntax;
               if (null != throwStatement) {
                  var objectCreationSyntax = throwStatement.Expression as ObjectCreationExpressionSyntax;
                  if (null != objectCreationSyntax) {
                     if (_exceptionsNotThrown.Keys.Contains(objectCreationSyntax)) {
                        _exceptionsNotThrown.Remove(objectCreationSyntax);
                     }
                     else {
                        _exceptionsThrown.Add(objectCreationSyntax);
                     }
                  }
                  else {
                     var identifierNameSyntax = throwStatement.Expression as IdentifierNameSyntax;
                     if (_exceptionsVars.Keys.Contains(identifierNameSyntax.Identifier.ToString())) {
                        _exceptionsVars.Remove(identifierNameSyntax.Identifier.ToString());
                     }
                  }
               }
            }
         }
      }

      private void SemenaticModelAnalysisEnd(SemanticModelAnalysisContext context) {
         lock (_lock) {
            try {

               foreach (var exceptions in _exceptionsVars.Values) {
                  foreach (SyntaxNode exception in exceptions.Keys) {
                     var pos = exception.SyntaxTree.GetMappedLineSpan(exception.Span);
                     //Console.WriteLine(pos.ToString());
                     AddViolation(exceptions[exception], new FileLinePositionSpan[] { pos });
                  }
               }

               foreach (SyntaxNode exception in _exceptionsNotThrown.Keys) {
                  var pos = exception.SyntaxTree.GetMappedLineSpan(exception.Span);
                  //Console.WriteLine(pos.ToString());
                  AddViolation(_exceptionsNotThrown[exception], new FileLinePositionSpan[] { pos });
               }


               _exceptionsNotThrown.Clear();
               _exceptionsThrown.Clear();
               _exceptionsVars.Clear();

            }
            catch (Exception e) {
               Console.WriteLine(e.Message);
               Console.WriteLine(e.StackTrace);
            }
         }
      }
   }
}
