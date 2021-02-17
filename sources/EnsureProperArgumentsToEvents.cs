using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.DotNet.CastDotNetExtension;


namespace CastDotNetExtension {
   [CastRuleChecker]
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   [RuleDescription(
       Id = "EI_EnsureProperArgumentsToEvents",
       Title = "Ensure Proper Arguments To Events",
       MessageFormat = "Ensure Proper Arguments To Events",
       Category = "Programming Practices - Error and Exception Handling",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.EnsureProperArgumentsToEvents"
   )]
   public class EnsureProperArgumentsToEvents : AbstractRuleChecker {

      protected enum CompilationType {
         None,
         CSharp,
         VisualBasic
      }


      private static INamedTypeSymbol _eventHandlerSymbols = null;
      private static INamedTypeSymbol _eventHandlerWithArgsSymbols = null;
      private static IMethodSymbol _eventHandlerInvokeMethodSymbols = null;
      private static IMethodSymbol _eventHandlerWithArgsInvokeMethodSymbols = null;
      private static INamedTypeSymbol _eventArgSymbols = null;


      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         //TODO: register for events
         context.RegisterSyntaxNodeAction(Analyze, Microsoft.CodeAnalysis.CSharp.SyntaxKind.InvocationExpression);
      }

      private readonly object _lock = new object();

      protected void Analyze(SyntaxNodeAnalysisContext context) {
         lock (_lock) {
            try {
               Init(context.Compilation);
               var model = context.SemanticModel;
               var symbInf = model.GetSymbolInfo(context.Node);
               var invokedMethod = symbInf.Symbol as IMethodSymbol;// get invocation method symbol   
               if (null != invokedMethod) {
                  var invocationNode = context.Node as Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax;
                  var symbClass = context.ContainingSymbol.ContainingSymbol as INamedTypeSymbol;

                  // get instance event variable name
                  string varName = "";
                  if (invocationNode.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.ConditionalAccessExpressionSyntax) {
                     var condAccessExpression = invocationNode.Parent as Microsoft.CodeAnalysis.CSharp.Syntax.ConditionalAccessExpressionSyntax;
                     var identifierSyntax = condAccessExpression.Expression as Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax;
                     if (identifierSyntax != null) {
                        varName = identifierSyntax.Identifier.ValueText;
                     }
                  } else if (invocationNode.Expression is Microsoft.CodeAnalysis.CSharp.Syntax.MemberAccessExpressionSyntax) {
                     var memberAccessExpression = invocationNode.Expression as Microsoft.CodeAnalysis.CSharp.Syntax.MemberAccessExpressionSyntax;
                     var identifierSyntax = memberAccessExpression.Expression as Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax;
                     if (identifierSyntax != null) {
                        varName = identifierSyntax.Identifier.ValueText;
                     }

                  } else if (invocationNode.Expression is Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax) {
                     var identifierSyntax = invocationNode.Expression as Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax;
                     if (identifierSyntax != null) {
                        varName = identifierSyntax.Identifier.ValueText;
                     }
                  } /*else {
                     // --------------
                  }*/
                  // check if event variable is static
                  bool isEventNonStatic = false;
                  if (varName.Length > 0 && symbClass != null) {
                     var classMembers = symbClass.GetMembers(varName);
                     foreach (ISymbol member in classMembers) {
                        if (!member.IsStatic) {
                           isEventNonStatic = true;
                        }
                     }
                  }
                  // check invocation argument for null sender and null data
                  if (invokedMethod == _eventHandlerInvokeMethodSymbols || invokedMethod.OriginalDefinition == _eventHandlerWithArgsInvokeMethodSymbols) {
                     var firstArgNode = invocationNode.ArgumentList.Arguments[0].Expression as Microsoft.CodeAnalysis.CSharp.Syntax.LiteralExpressionSyntax;
                     var secondArgNode = invocationNode.ArgumentList.Arguments[1].Expression as Microsoft.CodeAnalysis.CSharp.Syntax.LiteralExpressionSyntax;
                     if (firstArgNode != null && isEventNonStatic) {
                        if (firstArgNode.Kind() == Microsoft.CodeAnalysis.CSharp.SyntaxKind.NullLiteralExpression) {
                           var pos = invocationNode.GetLocation().GetMappedLineSpan();
                           //Log.Warn(pos.ToString());
                           AddViolation(context.ContainingSymbol, new FileLinePositionSpan[] { pos });
                        }
                     }
                     if (secondArgNode != null) {
                        if (secondArgNode.Kind() == Microsoft.CodeAnalysis.CSharp.SyntaxKind.NullLiteralExpression) {
                           var pos = invocationNode.GetLocation().GetMappedLineSpan();
                           //Log.Warn(pos.ToString());
                           AddViolation(context.ContainingSymbol, new FileLinePositionSpan[] { pos });
                        }
                     }
                  }
               }
            }
            catch (System.Exception e) {
               Log.Warn("Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath, e);
            }
         }
      }

      private CompilationType _typeCompilation = CompilationType.None;

      protected bool IsChangedCompilation(bool isCsharpCompilation) {
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



      private void Init(Compilation compil) {
         bool changed = IsChangedCompilation(compil is Microsoft.CodeAnalysis.CSharp.CSharpCompilation);

         if (changed) {
            _eventHandlerSymbols = compil.GetTypeByMetadataName("System.EventHandler");
            _eventHandlerWithArgsSymbols = compil.GetTypeByMetadataName("System.EventHandler`1");
            _eventArgSymbols = compil.GetTypeByMetadataName("System.EventArgs");

         }
         else {
            if (_eventHandlerSymbols == null) {
               _eventHandlerSymbols = compil.GetTypeByMetadataName("System.EventHandler");
            }

            if (_eventHandlerWithArgsSymbols == null) {
               _eventHandlerWithArgsSymbols = compil.GetTypeByMetadataName("System.EventHandler`1");
            }

            if (_eventArgSymbols == null) {
               _eventArgSymbols = compil.GetTypeByMetadataName("System.EventArgs");
            }
         }

         if (null == _eventHandlerInvokeMethodSymbols && null != _eventHandlerSymbols) {
            _eventHandlerInvokeMethodSymbols = _eventHandlerSymbols.GetMembers().OfType<IMethodSymbol>().FirstOrDefault(m => "Invoke" == m.Name);
         }

         if (null == _eventHandlerWithArgsInvokeMethodSymbols && null != _eventHandlerWithArgsSymbols) {
            _eventHandlerWithArgsInvokeMethodSymbols = _eventHandlerWithArgsSymbols.GetMembers().OfType<IMethodSymbol>().FirstOrDefault(m => "Invoke" == m.Name);
         }
      }
   }
}
