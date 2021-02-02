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
         context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ClassDeclaration, SyntaxKind.InvocationExpression, SyntaxKind.ParenthesizedLambdaExpression, SyntaxKind.SimpleLambdaExpression, SyntaxKind.ObjectCreationExpression);
         context.RegisterSemanticModelAction(HandleSemanticModelAnalysisEnd);
      }

      private object _lock = new object();

      private HashSet<string> _sharedSymbols = new HashSet<string>();
      private HashSet<String> _creatorOrVariables = new HashSet<string>();
      private Dictionary<String, Tuple<SyntaxNode, SyntaxNode, ISymbol>> _allCreators = new Dictionary<string, Tuple<SyntaxNode, SyntaxNode, ISymbol>>();

      protected void Analyze(SyntaxNodeAnalysisContext context) {
         lock (_lock) {
            try {
               if (context.Node is TypeDeclarationSyntax) {
                  VisitClassDeclaration(context);
               } else if (context.Node is InvocationExpressionSyntax) {
                  VisitInvocationExpression(context);
               } else if (context.Node is ObjectCreationExpressionSyntax) {
                  VisitObjectCreation(context);
               }
            } catch (Exception e) {
               Log.Warn("Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath + ": " + context.Node.GetLocation().GetMappedLineSpan(), e);
            }
         }
      }


      protected void VisitClassDeclaration(SyntaxNodeAnalysisContext context) {
         var declarationSyntax = context.Node as TypeDeclarationSyntax;
         IList<TypeAttributes.ITypeAttribute> typeAttributes = new List<TypeAttributes.ITypeAttribute>();
         typeAttributes = TypeAttributes.Get(declarationSyntax, typeAttributes, new[] { TypeAttributes.AttributeType.PartCreationPolicy });

         if (null != typeAttributes && typeAttributes.Any()) {
            _sharedSymbols.Add(declarationSyntax.Identifier.ValueText);
         }

      }

      protected void VisitObjectCreation(SyntaxNodeAnalysisContext context) {
         var objectCreationSyntax = context.Node as ObjectCreationExpressionSyntax;
         var typename = objectCreationSyntax.Type.ToString();
         if (null != typename && IsTypeRelevant(typename)) {
            SyntaxNode parentNode = null;
            string name = SyntaxNode2SubjectName.get(context.Node, delegate(SyntaxNode parent) {
               parentNode = parent;
               if (null != parent) {
                  if ("InvocationExpressionSyntax" == parent.GetType().Name) {
                     IMethodSymbol invokedMethod = context.SemanticModel.GetSymbolInfo(parent).Symbol as IMethodSymbol;
                     if (null != invokedMethod) {
                        if (IsAddServiceMethod(invokedMethod)) {
                           return false;
                        }
                     }
                  }
               }

               return true;
            });

            if (null != name && null != parentNode) {
               AddCreator(name, parentNode, context.Node, context.ContainingSymbol);
            }
         }
      }


      protected void VisitInvocationExpression(SyntaxNodeAnalysisContext context) {

         var invokeExpr = context.Node as InvocationExpressionSyntax;
         
         if (null != invokeExpr) {
            var iSymbol = context.SemanticModel.GetSymbolInfo(invokeExpr.Expression).Symbol;
            if (null != iSymbol && iSymbol is IMethodSymbol) {
               var invokedMethod = iSymbol as IMethodSymbol;
               if (null != invokedMethod) {
                  if (MethodKind.Ordinary == invokedMethod.MethodKind) {
                     if (IsAddServiceMethod(invokedMethod)) {
                        var invocationExpression = context.Node as InvocationExpressionSyntax;
                        if (2 <= invocationExpression.ArgumentList.Arguments.Count) {
                           var argument = invocationExpression.ArgumentList.Arguments[1];
                           try {
                              var identifierNameSyntax = getIdentifierNameSyntax(argument);
                              if (null != identifierNameSyntax) {
                                 string name = getCreatorOrVariableName(identifierNameSyntax, context.SemanticModel);
                                 if (null != name) {
                                    AddCreatorOrVariable(name);
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

      private string getCreatorOrVariableName(IdentifierNameSyntax identifierNameSyntax, SemanticModel semanticModel) {
         string name = null;
         if (null != identifierNameSyntax) {
            //TODO: verify
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

      private bool IsTypeRelevant(string typename) {
         WriteLine("IsTypeRelevant: " + typename);
         return _sharedSymbols.Contains(typename);
      }

      private void AddCreatorOrVariable(string creatorOrVariable) {
         _creatorOrVariables.Add(creatorOrVariable);
         WriteLine("AddCreatorOrVariable: " + creatorOrVariable);
      }

      private void AddCreator(string creator, SyntaxNode creatorContainerSyntax, SyntaxNode creatorSyntax, ISymbol iSymbol) {
         _allCreators[creator] = new Tuple<SyntaxNode, SyntaxNode, ISymbol>(creatorContainerSyntax, creatorSyntax, iSymbol);
         WriteLine("AddCreator: " + creator);
      }

      private void HandleSemanticModelAnalysisEnd(SemanticModelAnalysisContext context) {
         lock (_lock) {
            try {
               foreach (var creator in _allCreators.Keys) {
                  Tuple<SyntaxNode, SyntaxNode, ISymbol> location = _allCreators[creator];
                  if (!_creatorOrVariables.Contains(creator)) {
                     if (null != location.Item3) {
                        var pos = location.Item1.GetLocation().GetMappedLineSpan();
                        Console.WriteLine(location.Item3 + ": " + pos);
                        AddViolation(location.Item3, new FileLinePositionSpan[] { pos });
                     }
                  }
               }
               _creatorOrVariables.Clear();
               _allCreators.Clear();
            } catch (Exception e) {
               Log.Warn("Exception while analyzing semantic model " + context.SemanticModel.SyntaxTree.FilePath, e);
            }
         }
      }
   }
}
