﻿using System;
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
       Category = "Programming Practices - OO Inheritance and Polymorphism",
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
         context.RegisterSemanticModelAction(HandleSemanticModelAnalysisEnd);
      }

      private object _lock = new object();

      protected void VisitClassDeclaration(SyntaxNode node, Compilation compilation, ref HashSet<string> sharedSymbols) {
         var declarationSyntax = node as TypeDeclarationSyntax;
         IList<TypeAttributes.ITypeAttribute> typeAttributes = new List<TypeAttributes.ITypeAttribute>();
         typeAttributes = TypeAttributes.Get(declarationSyntax, typeAttributes, new[] { TypeAttributes.AttributeType.PartCreationPolicy });

         if (null != typeAttributes && typeAttributes.Any()) {
            sharedSymbols.Add(declarationSyntax.Identifier.ValueText);
         }

      }

      protected void VisitObjectCreation(SyntaxNode node, Compilation compilation,
         ref HashSet<string> sharedSymbols,
         ref Dictionary<String, Tuple<SyntaxNode, SyntaxNode, ISymbol>> allCreators,
         SemanticModel semanticModel) {
         var objectCreationSyntax = node as ObjectCreationExpressionSyntax;
         var typename = objectCreationSyntax.Type.ToString();
         if (null != typename && IsTypeRelevant(typename, ref sharedSymbols)) {
            SyntaxNode parentNode = null;
            string name = SyntaxNode2SubjectName.get(node, delegate(SyntaxNode parent) {
               parentNode = parent;
               if (null != parent) {
                  if ("InvocationExpressionSyntax" == parent.GetType().Name) {
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
               ISymbol iSymbol = semanticModel.GetEnclosingSymbol(node.GetLocation().GetMappedLineSpan().Span.Start.Line);
               if (null != iSymbol) {
                  AddCreator(name, parentNode, node, iSymbol, ref allCreators);
               }
            }
         }
      }


      protected void VisitInvocationExpression(SyntaxNode node, Compilation compilation, ref HashSet<String> creatorOrVariable) {

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
                              var identifierNameSyntax = getIdentifierNameSyntax(argument);
                              if (null != identifierNameSyntax) {
                                 string name = getCreatorOrVariableName(identifierNameSyntax, semanticModel);
                                 if (null != name) {
                                    AddCreatorOrVariable(name, ref creatorOrVariable);
                                 }
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

      private void HandleSemanticModelAnalysisEnd(SemanticModelAnalysisContext context) {
         lock (_lock) {
            try {
               if ("C#" == context.SemanticModel.Compilation.Language) {

                  HashSet<string> sharedSymbols = new HashSet<string>();
                  IEnumerable<SyntaxNode> classDeclarations = context.SemanticModel.SyntaxTree.GetRoot().DescendantNodes().Where(n => n.IsKind(SyntaxKind.ClassDeclaration));

                  foreach (var classDeclaration in classDeclarations) {
                     VisitClassDeclaration(classDeclaration, context.SemanticModel.Compilation, ref sharedSymbols);
                  }

                  if (!sharedSymbols.Any()) {
                     Log.Debug("No Shared Symbols Found");
                  } else {
                     HashSet<SyntaxKind> syntaxKinds = new HashSet<SyntaxKind> {
                        SyntaxKind.InvocationExpression, SyntaxKind.ParenthesizedLambdaExpression, SyntaxKind.SimpleLambdaExpression, SyntaxKind.ObjectCreationExpression
                     };

                     IEnumerable<SyntaxNode> nodes = context.SemanticModel.SyntaxTree.GetRoot().DescendantNodes().Where(n => syntaxKinds.Contains(n.Kind()));

                     HashSet<String> creatorOrVariables = new HashSet<string>();
                     Dictionary<String, Tuple<SyntaxNode, SyntaxNode, ISymbol>> allCreators = new Dictionary<string, Tuple<SyntaxNode, SyntaxNode, ISymbol>>();

                     foreach (var node in nodes) {
                        if (node is InvocationExpressionSyntax) {
                           VisitInvocationExpression(node, context.SemanticModel.Compilation, ref creatorOrVariables);
                        } else if (node is ObjectCreationExpressionSyntax) {
                           VisitObjectCreation(node, context.SemanticModel.Compilation, ref sharedSymbols, ref allCreators, context.SemanticModel);
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
                  }
               }
            } catch (Exception e) {
               Log.Warn("Exception while analyzing semantic model " + context.SemanticModel.SyntaxTree.FilePath, e);
            }
         }
      }
   }
}
