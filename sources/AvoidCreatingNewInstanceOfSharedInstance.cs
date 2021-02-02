using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CastDotNetExtension.Utils;
using System.Threading.Tasks;
using Roslyn.DotNet.CastDotNetExtension;
using Roslyn.DotNet.Common;


namespace CastDotNetExtension {
   [CastRuleChecker]
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   [RuleDescription(
       Id = "EI_AvoidCreatingNewInstanceOfSharedInstance",
       Title = "Avoid Creating New Instance Of Shared Instance",
       MessageFormat = "Avoid Creating New Instance Of Shared Instance",
       Category = "Programmingn Practices - OO Inheritance and Polymorphism",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.AvoidCreatingNewInstanceOfSharedInstance"
   )]
   public class AvoidCreatingNewInstanceOfSharedInstance : AbstractRuleChecker {
      public AvoidCreatingNewInstanceOfSharedInstance() {
      }

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         //context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ClassDeclaration, SyntaxKind.InvocationExpression, SyntaxKind.ParenthesizedLambdaExpression, SyntaxKind.SimpleLambdaExpression, SyntaxKind.ObjectCreationExpression);
         context.RegisterSemanticModelAction(HandleSemanticModelAnalysisEnd);
      }

      private object _lock = new object();

      //private HashSet<string> _sharedSymbols = new HashSet<string>();
      //private HashSet<String> _creatorOrVariables = new HashSet<string>();
      //private Dictionary<String, Tuple<SyntaxNode, SyntaxNode, ISymbol>> _allCreators = new Dictionary<string, Tuple<SyntaxNode, SyntaxNode, ISymbol>>();

      protected void Analyze(SyntaxNodeAnalysisContext context, 
         ref HashSet<string> sharedSymbols, 
         ref HashSet<String> creatorOrVariables,
         ref Dictionary<String, Tuple<SyntaxNode, SyntaxNode, ISymbol>> allCreators) {
            lock (_lock) {
               try {
                  if (context.Node is TypeDeclarationSyntax) {
                     VisitClassDeclaration(context.Node, context.Compilation, context.ContainingSymbol, ref sharedSymbols);
                  } else if (context.Node is InvocationExpressionSyntax) {
                     VisitInvocationExpression(context.Node, context.Compilation, context.ContainingSymbol, ref creatorOrVariables);
                  } else if (context.Node is ObjectCreationExpressionSyntax) {
                     VisitObjectCreation(context.Node, context.Compilation, context.ContainingSymbol, ref sharedSymbols, ref allCreators);
                  }
               } catch (Exception e) {
                  Log.Warn("Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath + ": " + context.Node.GetLocation().GetMappedLineSpan(), e);
               }
            }
      }

      protected void Analyze(SyntaxNode node, Compilation compilation, ISymbol containingSymbol,
         ref HashSet<string> sharedSymbols,
         ref HashSet<String> creatorOrVariables,
         ref Dictionary<String, Tuple<SyntaxNode, SyntaxNode, ISymbol>> allCreators) {
         lock (_lock) {
            try {
               if (node is TypeDeclarationSyntax) {
                  VisitClassDeclaration(node, compilation, containingSymbol, ref sharedSymbols);
               } else if (node is InvocationExpressionSyntax) {
                  VisitInvocationExpression(node, compilation, containingSymbol, ref creatorOrVariables);
               } else if (node is ObjectCreationExpressionSyntax) {
                  VisitObjectCreation(node, compilation, containingSymbol, ref sharedSymbols, ref allCreators);
               }
            } catch (Exception e) {
               Log.Warn("Exception while analyzing " + node.SyntaxTree.FilePath + ": " + node.GetLocation().GetMappedLineSpan(), e);
            }
         }
      }

      protected void VisitClassDeclaration(SyntaxNode node, Compilation compilation, ISymbol containingSymbol, ref HashSet<string> sharedSymbols) {
         var declarationSyntax = node as TypeDeclarationSyntax;
         IList<TypeAttributes.ITypeAttribute> typeAttributes = new List<TypeAttributes.ITypeAttribute>();
         typeAttributes = TypeAttributes.Get(declarationSyntax, typeAttributes, new[] { TypeAttributes.AttributeType.PartCreationPolicy });

         if (null != typeAttributes && typeAttributes.Any()) {
            sharedSymbols.Add(declarationSyntax.Identifier.ValueText);
         }

      }

      protected void VisitObjectCreation(SyntaxNode node, Compilation compilation, ISymbol containingSymbol,
         ref HashSet<string> sharedSymbols,
         ref Dictionary<String, Tuple<SyntaxNode, SyntaxNode, ISymbol>> allCreators) {
         var objectCreationSyntax = node as ObjectCreationExpressionSyntax;
         var typename = objectCreationSyntax.Type.ToString();
         if (null != typename && IsTypeRelevant(typename, ref sharedSymbols)) {
            SyntaxNode parentNode = null;
            string name = SyntaxNode2SubjectName.get(node, delegate(SyntaxNode parent) {
               parentNode = parent;
               if (null != parent) {
                  if ("InvocationExpressionSyntax" == parent.GetType().Name) {
                     var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);
                     if (null != semanticModel) {
                        IMethodSymbol invokedMethod = semanticModel.GetSymbolInfo(parent).Symbol as IMethodSymbol;
                        if (null != invokedMethod) {
                           if (IsAddServiceMethod(invokedMethod)) {
                              return false;
                           }
                        }
                     }
                  }
               }

               return true;
            });

            if (null != name && null != parentNode) {
               AddCreator(name, parentNode, node, containingSymbol, ref allCreators);
            }
         }
      }


      protected void VisitInvocationExpression(SyntaxNode node, Compilation compilation, ISymbol containingSymbol, ref HashSet<String> creatorOrVariable) {

         var invokeExpr = node as InvocationExpressionSyntax;
         
         if (null != invokeExpr) {
            var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);
            if (null != semanticModel) {
               var iSymbol = semanticModel.GetSymbolInfo(invokeExpr.Expression).Symbol;
               if (null != iSymbol && iSymbol is IMethodSymbol) {
                  var invokedMethod = iSymbol as IMethodSymbol;
                  if (null != invokedMethod) {
                     if (MethodKind.Ordinary == invokedMethod.MethodKind) {
                        if (IsAddServiceMethod(invokedMethod)) {
                           var invocationExpression = node as InvocationExpressionSyntax;
                           if (2 <= invocationExpression.ArgumentList.Arguments.Count) {
                              var argument = invocationExpression.ArgumentList.Arguments[1];
                              try {
                                 var identifierNameSyntax = getIdentifierNameSyntax(argument);
                                 if (null != identifierNameSyntax) {
                                    string name = getCreatorOrVariableName(identifierNameSyntax, semanticModel);
                                    if (null != name) {
                                       AddCreatorOrVariable(name, ref creatorOrVariable);
                                    }
                                 }
                              } catch (Exception e) {
                                 WriteLine(e.Message);
                              }
                           }
                        }
                     }
                  }
               }
            }
         }
      }

      private string getCreatorOrVariableName(IdentifierNameSyntax identifierNameSyntax, SemanticModel semanticModel) {
         string name = null;
         if (null != identifierNameSyntax) {
            ISymbol iSymbol = semanticModel.GetSymbolInfo(identifierNameSyntax).Symbol;
            if (null != iSymbol && iSymbol is IMethodSymbol) {
               var creator = iSymbol as IMethodSymbol;
               name = creator.Name;
               if (MethodKind.PropertyGet == creator.MethodKind) {
                  name = creator.Name.Substring(4);
               }
            }
            else {
               name = identifierNameSyntax.Identifier.ValueText;
            }
         }
         return name;
      }

      protected IdentifierNameSyntax getIdentifierNameSyntax(ArgumentSyntax argument) {
         if (null != argument) {
            var objectCreationSyntax = argument.Expression as ObjectCreationExpressionSyntax;
            if (null == objectCreationSyntax) {
               IdentifierNameSyntax identifierNameSyntax = argument.Expression as IdentifierNameSyntax;
               return identifierNameSyntax;
            }
         }
         return null;
      }

      private bool IsAddServiceMethod(IMethodSymbol method) {
         var originalDefinition = method.OriginalDefinition.ToString();
         if (originalDefinition.StartsWith("System.ComponentModel.Design.ServiceContainer.AddService")) {
            return true;
         }
         return false;
      }

      private void WriteLine(string msg) {
         //System.Console.WriteLine(msg);
      }

      private bool IsTypeRelevant(string typename, ref HashSet<string> sharedSymbols) {
         WriteLine("IsTypeRelevant: " + typename);
         return sharedSymbols.Contains(typename);
      }

      private void AddCreatorOrVariable(string creatorOrVariable, ref HashSet<string> creatorOrVariables) {
         creatorOrVariables.Add(creatorOrVariable);
         WriteLine("AddCreatorOrVariable: " + creatorOrVariable);
      }

      private void AddCreator(string creator, SyntaxNode creatorContainerSyntax, SyntaxNode creatorSyntax, ISymbol iSymbol,
         ref Dictionary<String, Tuple<SyntaxNode, SyntaxNode, ISymbol>> allCreators) {
         allCreators[creator] = new Tuple<SyntaxNode, SyntaxNode, ISymbol>(creatorContainerSyntax, creatorSyntax, iSymbol);
         WriteLine("AddCreator: " + creator);
      }

      //public override void Reset() {
      //   try {
      //      creatorOrVariables.Clear();
      //      allCreators.Clear();
      //      base.Reset();
      //   } catch (Exception e) {
      //      Log.Warn("Exception while resetting ", e);
      //   }
      //}

      private void HandleSemanticModelAnalysisEnd(SemanticModelAnalysisContext context) {
         lock (_lock) {
            try {
               
               HashSet<SyntaxKind> syntaxKinds = new HashSet<SyntaxKind> {
                  SyntaxKind.ClassDeclaration, SyntaxKind.InvocationExpression, SyntaxKind.ParenthesizedLambdaExpression, SyntaxKind.SimpleLambdaExpression, SyntaxKind.ObjectCreationExpression
               };
               var root = context.SemanticModel.SyntaxTree.GetRoot();
               
               IEnumerable<SyntaxNode> nodes = context.SemanticModel.SyntaxTree.GetRoot().DescendantNodes().Where(n => syntaxKinds.Contains(n.Kind()));
               HashSet<string> sharedSymbols = new HashSet<string>();
               HashSet<String> creatorOrVariables = new HashSet<string>();
               Dictionary<String, Tuple<SyntaxNode, SyntaxNode, ISymbol>> allCreators = new Dictionary<string, Tuple<SyntaxNode, SyntaxNode, ISymbol>>();
               
               foreach (var node in nodes) {
                  ISymbol iSymbol = context.SemanticModel.GetEnclosingSymbol(node.GetLocation().GetMappedLineSpan().Span.Start.Line);
                  if (null == iSymbol) {
                     Console.WriteLine("Could not get symbol for kind " + node.Kind()  +  ": " + node);
                  } else {
                     Analyze(node, context.SemanticModel.Compilation, iSymbol, ref sharedSymbols, ref creatorOrVariables, ref allCreators);      
                  }
               }

               foreach (var creator in allCreators.Keys) {
                  Tuple<SyntaxNode, SyntaxNode, ISymbol> location = allCreators[creator];
                  if (!creatorOrVariables.Contains(creator)) {
                     if (null != location.Item3) {
                        var pos = location.Item2.GetLocation().GetMappedLineSpan();
                        //Console.WriteLine(location.Item3 + ": " + pos);
                        AddViolation(location.Item3, new FileLinePositionSpan[] { pos });
                     }
                  }
               }
            } catch (Exception e) {
               Log.Warn("Exception while analyzing semantic model " + context.SemanticModel.SyntaxTree.FilePath, e);
            }
         }
      }
   }
}
