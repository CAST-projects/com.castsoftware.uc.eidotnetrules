using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.DotNet.CastDotNetExtension;
using Roslyn.DotNet.Common;

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
      public AvoidCreatingExceptionWithoutThrowingThem()
            : base(ViolationCreationMode.ViolationWithAdditionalBookmarks)
        {
        }


        /// <summary>
        /// Initialize the QR with the given context and register all the syntax nodes
        /// to listen during the visit and provide a specific callback for each one
        /// </summary>
        /// <param name="context"></param>
        public override void Init(AnalysisContext context) {
         //context.RegisterSyntaxNodeAction(this.Analyze, SyntaxKind.ObjectCreationExpression, SyntaxKind.ThrowStatement);
         context.RegisterSemanticModelAction(this.SemenaticModelAnalysisEnd);
      }

      
      
      private Dictionary<SyntaxNode, ISymbol> _exceptionsNotThrown = new Dictionary<SyntaxNode, ISymbol>();
      private HashSet<SyntaxNode> _exceptionsThrown = new HashSet<SyntaxNode>();
      private Dictionary<String, Dictionary<SyntaxNode, ISymbol>> _exceptionsVars = new Dictionary<String, Dictionary<SyntaxNode, ISymbol>>();

      private Dictionary<INamedTypeSymbol, bool> _typeToIsException = new Dictionary<INamedTypeSymbol, bool>();


      private bool IsException(INamedTypeSymbol iTypeIn, INamedTypeSymbol systemException, INamedTypeSymbol systemObject) {
         bool isException = false;
         if (null != iTypeIn && null != systemException && null != systemObject) {
            if (_typeToIsException.TryGetValue(iTypeIn, out isException)) {
               return isException;
            }
            INamedTypeSymbol iType = iTypeIn;
            while (null != iType && systemObject != iType) {
               if (iType == systemException) {
                  isException = true;
                  _typeToIsException[iTypeIn] = isException;
                  break;
               }
               iType = iType.BaseType;
            }
         }
         return isException;
      }

      private object _lock = new object();
      private void Analyze(SyntaxNodeAnalysisContext context) {
         lock (_lock) {
            try {
               var newSyntax = context.Node as ObjectCreationExpressionSyntax;
               if (null != newSyntax) {
                  var typeSymbol = context.SemanticModel.GetTypeInfo(context.Node).Type;
                  if (!_exceptionsThrown.Contains(newSyntax)) {
                     var typeName = newSyntax.Type;
                     if (null != typeName) {
                        var type = context.SemanticModel.GetSymbolInfo(typeName).Symbol as INamedTypeSymbol;
                        if (null != type) {
                           INamedTypeSymbol systemException = context.Compilation.GetTypeByMetadataName("System.Exception");
                           INamedTypeSymbol systemObject = context.Compilation.GetTypeByMetadataName("System.Object");
                           if (null != systemException && IsException(type, systemException, systemObject)) { //context.Compilation.ClassifyConversion(type, systemException).IsImplicit) {
                              if (newSyntax.Parent is EqualsValueClauseSyntax) {
                                 if (newSyntax.Parent.Parent is VariableDeclaratorSyntax) {
                                    var variableDeclarator = newSyntax.Parent.Parent as VariableDeclaratorSyntax;
                                    var varName = variableDeclarator.Identifier.ToString();
                                    ISymbol sym = context.SemanticModel.GetDeclaredSymbol(variableDeclarator);
                                    Dictionary<SyntaxNode, ISymbol> nodes = null;
                                    if (!_exceptionsVars.TryGetValue(varName, out nodes)) {
                                       nodes = new Dictionary<SyntaxNode, ISymbol>();
                                       _exceptionsVars[varName] = nodes;
                                    }
                                    nodes[variableDeclarator] = context.ContainingSymbol;
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
                        var parentKind = objectCreationSyntax.Parent.Kind();
                        if (_exceptionsNotThrown.Keys.Contains(objectCreationSyntax)) {
                           _exceptionsNotThrown.Remove(objectCreationSyntax);
                        }
                        else {
                           _exceptionsThrown.Add(objectCreationSyntax);
                        }
                     }
                     else {
                        if (null != throwStatement.Expression && throwStatement.Expression is IdentifierNameSyntax) {
                           var identifierNameSyntax = throwStatement.Expression as IdentifierNameSyntax;
                           var sym = context.SemanticModel.GetSymbolInfo(identifierNameSyntax);
                           if (_exceptionsVars.Keys.Contains(identifierNameSyntax.Identifier.ToString())) {
                              _exceptionsVars.Remove(identifierNameSyntax.Identifier.ToString());
                           }
                        }
                     }
                  }
               }
            }
            catch (Exception e) {
               Log.Warn("Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath + ": " + context.Node.GetLocation().GetMappedLineSpan(), e);
            }
         }

      }

      private void AnalyzeCreation(ObjectCreationExpressionSyntax creation, SemanticModel semanticModel,
         INamedTypeSymbol systemException, INamedTypeSymbol systemObject, ref HashSet<ISymbol> exceptionVars, 
         ref HashSet<SyntaxNode> violatingNodes) {
         if (null != creation) {
            var typeSymbol = semanticModel.GetTypeInfo(creation).Type;
            int line = creation.GetLocation().GetMappedLineSpan().Span.Start.Line;
            
            if (null != typeSymbol && typeSymbol is INamedTypeSymbol) {
               if (IsException(typeSymbol as INamedTypeSymbol, systemException, systemObject)) {
                  if (creation.Parent is EqualsValueClauseSyntax) {
                     if (creation.Parent.Parent is VariableDeclaratorSyntax) {
                        var variableDeclarator = creation.Parent.Parent as VariableDeclaratorSyntax;
                        ISymbol sym = semanticModel.GetDeclaredSymbol(variableDeclarator);
                        if (null != sym) {
                           exceptionVars.Add(sym);
                        }
                     }
                  } else if (creation.Parent is ExpressionStatementSyntax) {
                     violatingNodes.Add(creation);
                  }
               }
            }
         }
      }

      private void AnalyzeThrow(ThrowStatementSyntax aThrow, SemanticModel semanticModel, ref HashSet<ISymbol> exceptionVars) {
            if (null != aThrow) {
               var identifier = aThrow.Expression as IdentifierNameSyntax;
               if (null != identifier) {
                  var symbol = semanticModel.GetSymbolInfo(identifier).Symbol;
                  if (null != symbol && exceptionVars.Contains(symbol)) {
                     exceptionVars.Remove(symbol);
                  }
               }
            }
      }


      private void SemenaticModelAnalysisEnd(SemanticModelAnalysisContext context) {
         lock (_lock) {
            try {
               if ("C#" == context.SemanticModel.Compilation.Language) {

                  IEnumerable<SyntaxNode> nodes = 
                     context.SemanticModel.SyntaxTree.GetRoot().DescendantNodes().Where(n => (
                        (n.IsKind(SyntaxKind.ObjectCreationExpression) && n.Parent.Kind() != SyntaxKind.ThrowStatement) ||
                        (n.IsKind(SyntaxKind.ThrowStatement) && SyntaxKind.IdentifierName == (n as ThrowStatementSyntax).Expression.Kind())
                        ));

                  INamedTypeSymbol systemException = context.SemanticModel.Compilation.GetTypeByMetadataName("System.Exception");
                  INamedTypeSymbol systemObject = context.SemanticModel.Compilation.GetTypeByMetadataName("System.Object");

                  var exceptionVars = new HashSet<ISymbol>();
                  var violatingNodes = new HashSet<SyntaxNode>();
                  foreach (var node in nodes) {
                     if (node.IsKind(SyntaxKind.ObjectCreationExpression)) {
                        AnalyzeCreation(node as ObjectCreationExpressionSyntax, context.SemanticModel, systemException, systemObject, ref exceptionVars, ref violatingNodes);
                     } else {
                        AnalyzeThrow(node as ThrowStatementSyntax, context.SemanticModel, ref exceptionVars);
                     }
                  }

                  ISymbol violatingSymbol = null;
                  foreach (var exceptionVar in exceptionVars) {
                     
                     if (exceptionVar is IFieldSymbol) {
                        violatingSymbol = exceptionVar;
                     } else {
                        violatingSymbol = exceptionVar.ContainingSymbol;
                     }

                     var pos = exceptionVar.Locations.FirstOrDefault().GetMappedLineSpan();
                     AddViolation(violatingSymbol, new FileLinePositionSpan[] { pos });
                  }

                  foreach (var node in violatingNodes) {
                     violatingSymbol = context.SemanticModel.GetEnclosingSymbol(node.Span.Start);
                     if (null != violatingSymbol) {
                        AddViolation(violatingSymbol, new FileLinePositionSpan[] { node.GetLocation().GetMappedLineSpan() });
                     }
                  }

                  //foreach (var exceptions in _exceptionsVars.Values) {
                  //   foreach (SyntaxNode exception in exceptions.Keys) {
                  //      var pos = exception.SyntaxTree.GetMappedLineSpan(exception.Span);
                  //      //Log.Warn(pos.ToString());
                  //      AddViolation(exceptions[exception], new FileLinePositionSpan[] { pos });
                  //   }
                  //}

                  //foreach (SyntaxNode exception in _exceptionsNotThrown.Keys) {
                  //   var pos = exception.SyntaxTree.GetMappedLineSpan(exception.Span);
                  //   //Log.Warn(pos.ToString());
                  //   AddViolation(_exceptionsNotThrown[exception], new FileLinePositionSpan[] { pos });
                  //}

                  //Log.Info("_exceptionsNotThrown: " + _exceptionsNotThrown.Count);
                  //Log.Info("_exceptionsThrown: " + _exceptionsThrown.Count);
                  //Log.Info("_exceptionsVars: " + _exceptionsVars.Count);
                  //Log.Info("_typeToIsException: " + _typeToIsException.Count);
                  //_exceptionsNotThrown.Clear();
                  //_exceptionsThrown.Clear();
                  //_exceptionsVars.Clear();
                  //_typeToIsException.Clear();
               }

            }
            catch (Exception e) {
               Log.Warn("Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath, e);
            }
         }
      }
   }
}
