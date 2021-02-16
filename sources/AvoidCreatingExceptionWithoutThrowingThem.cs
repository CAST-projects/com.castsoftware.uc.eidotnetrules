using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.DotNet.CastDotNetExtension;

namespace CastDotNetExtension {
   [CastRuleChecker]
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   [RuleDescription(
       Id = "EI_AvoidCreatingExceptionWithoutThrowingThem",
       Title = "Avoid creating exception without throwing them",
       MessageFormat = "Avoid creating exception without throwing them",
       Category = "Programming Practices - Error and Exception Handling",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.AvoidCreatingExceptionWithoutThrowingThem"
   )]
   public class AvoidCreatingExceptionWithoutThrowingThem : AbstractRuleChecker {

        /// <summary>
        /// Initialize the QR with the given context and register all the syntax nodes
        /// to listen during the visit and provide a specific callback for each one
        /// </summary>
        /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         context.RegisterOperationAction(AnalyzeOperation, OperationKind.Throw, OperationKind.ObjectCreation, OperationKind.End);
      }

      private HashSet<IOperation> _excludedThrows = new HashSet<IOperation>();
      private Dictionary<SyntaxTree, int> _syntaxTreeRootToObjectCreationOrThrowNodeCount = new Dictionary<SyntaxTree, int>();
      private List<IThrowOperation> _throws = new List<IThrowOperation>();
      private HashSet<ISymbol> _exceptionVars = new HashSet<ISymbol>();
      private Dictionary<INamedTypeSymbol, bool> _typeToIsException = new Dictionary<INamedTypeSymbol, bool>();
      private Dictionary<ISymbol, HashSet<FileLinePositionSpan>> _symbol2ViolatingNodes = new Dictionary<ISymbol, HashSet<FileLinePositionSpan>>();
      private object _lock = new object();


      private static bool IsException(INamedTypeSymbol iTypeIn,
         INamedTypeSymbol systemException,
         INamedTypeSymbol systemObject,
         Compilation compilation,
         ref Dictionary<INamedTypeSymbol, bool> type2IsException
         ) {
         bool isException = false;
         if (null != iTypeIn && null != systemException && null != systemObject) {
            if (type2IsException.TryGetValue(iTypeIn, out isException)) {
               return isException;
            }

            isException = compilation.ClassifyConversion(iTypeIn, systemException).IsImplicit;
            type2IsException[iTypeIn] = isException;
         }
         return isException;
      }


      private void AnalyzeOperation(OperationAnalysisContext context) {
         lock (_lock) {
            try {

               if (OperationKind.ObjectCreation == context.Operation.Kind ||
                  OperationKind.Throw == context.Operation.Kind) {
                  int objectCreationOrThrowNodeCount = 0;
                  if (_syntaxTreeRootToObjectCreationOrThrowNodeCount.TryGetValue(context.Operation.Syntax.SyntaxTree, out objectCreationOrThrowNodeCount)) {
                     _syntaxTreeRootToObjectCreationOrThrowNodeCount[context.Operation.Syntax.SyntaxTree] = objectCreationOrThrowNodeCount - 1;
                  } else {
                     var syntaxTree = context.Operation.Syntax.SyntaxTree;
                     var root = syntaxTree.GetRoot(context.CancellationToken);
                     objectCreationOrThrowNodeCount =
                        root.DescendantNodesAndSelf().Where(
                        n => n.IsKind(SyntaxKind.ObjectCreationExpression) || n.IsKind(SyntaxKind.ThrowStatement)).Count();
                     _syntaxTreeRootToObjectCreationOrThrowNodeCount[syntaxTree] = objectCreationOrThrowNodeCount - 1;
                  }
               }

               var toRemove = _syntaxTreeRootToObjectCreationOrThrowNodeCount.Where(n => (0 == n.Value)).ToArray();
               foreach (var n in toRemove) {
                  _syntaxTreeRootToObjectCreationOrThrowNodeCount.Remove(n.Key);
               }

               bool end = !_syntaxTreeRootToObjectCreationOrThrowNodeCount.Any();

               if (OperationKind.ObjectCreation == context.Operation.Kind) {
                  var throwOp = (null != context.Operation.Parent && OperationKind.Throw == context.Operation.Parent.Kind) ?
                     context.Operation.Parent :
                     (OperationKind.Conversion == context.Operation.Parent.Kind && null != context.Operation.Parent.Parent &&
                     OperationKind.Throw == context.Operation.Parent.Parent.Kind) ? context.Operation.Parent.Parent : null;
                  if (null == throwOp) {
                     var objCreationOperation = context.Operation as IObjectCreationOperation;
                     if (null != objCreationOperation && objCreationOperation.Type is INamedTypeSymbol) {
                        INamedTypeSymbol systemException = context.Compilation.GetTypeByMetadataName("System.Exception");
                        INamedTypeSymbol systemObject = context.Compilation.GetTypeByMetadataName("System.Object");
                        if (IsException(objCreationOperation.Type as INamedTypeSymbol, systemException, systemObject, context.Compilation, ref _typeToIsException)) {
                           if (null != objCreationOperation.Parent) {
                              if (OperationKind.FieldInitializer == objCreationOperation.Parent.Kind) {
                                 var iFieldInitializer = objCreationOperation.Parent as IFieldInitializerOperation;
                                 foreach (ISymbol iField in iFieldInitializer.InitializedFields) {
                                    _exceptionVars.Add(iField);
                                 }
                              } else if (OperationKind.VariableInitializer == objCreationOperation.Parent.Kind) {
                                 if (null != objCreationOperation.Parent.Parent && OperationKind.VariableDeclarator == objCreationOperation.Parent.Parent.Kind) {
                                    if (null != objCreationOperation.Parent.Parent.Parent && OperationKind.VariableDeclaration == objCreationOperation.Parent.Parent.Parent.Kind) {
                                       var iVariableDeclaration = objCreationOperation.Parent.Parent.Parent as IVariableDeclarationOperation;
                                       foreach (var iVar in iVariableDeclaration.Declarators) {
                                          _exceptionVars.Add(iVar.Symbol);
                                       }
                                    }
                                 }
                              } else if (OperationKind.ExpressionStatement == context.Operation.Parent.Kind) {

                                 HashSet<FileLinePositionSpan> positions = null;
                                 if (!_symbol2ViolatingNodes.TryGetValue(context.ContainingSymbol, out positions)) {
                                    positions = new HashSet<FileLinePositionSpan>();
                                    _symbol2ViolatingNodes[context.ContainingSymbol] = positions;
                                 }
                                 positions.Add(context.Operation.Parent.Syntax.GetLocation().GetMappedLineSpan());

                              } else if (null != context.Operation.Parent && ((OperationKind.Conversion == context.Operation.Parent.Kind &&
                                 null != context.Operation.Parent.Parent && OperationKind.SimpleAssignment == context.Operation.Parent.Parent.Kind) ||
                                 OperationKind.SimpleAssignment == context.Operation.Parent.Kind)) {
                                 var iSimpleAssignment = OperationKind.SimpleAssignment == context.Operation.Parent.Kind ?
                                    context.Operation.Parent as ISimpleAssignmentOperation : context.Operation.Parent.Parent as ISimpleAssignmentOperation;
                                 if (null != iSimpleAssignment.Target) {
                                    ISymbol iSymbol = null;
                                    if (OperationKind.LocalReference == iSimpleAssignment.Target.Kind) {
                                       iSymbol = (iSimpleAssignment.Target as ILocalReferenceOperation).Local;
                                    } else if (OperationKind.FieldReference == iSimpleAssignment.Target.Kind) {
                                       iSymbol = (iSimpleAssignment.Target as IFieldReferenceOperation).Field;
                                    }
                                    if (null != iSymbol) {
                                       _exceptionVars.Add(iSymbol);
                                    }
                                 }
                              } else {
                                 Console.WriteLine("context.Operation.Parent.Kind: " + context.Operation.Parent.Kind);
                              }
                           } else {
                              Console.WriteLine("objCreationOperation.Parent = null");
                           }
                        }
                     }
                  } else {
                     _excludedThrows.Add(throwOp);
                  }
               } else if (OperationKind.Throw == context.Operation.Kind && !_excludedThrows.Contains(context.Operation)) {
                  _throws.Add(context.Operation as IThrowOperation);
               }

               if (end) { //if (OperationKind.End == context.Operation.Kind) { <== never receives OperationKind.End notification!
                  foreach (var aThrow in _throws) {
                     if (1 == aThrow.Children.Count() && OperationKind.Conversion == aThrow.Children.ElementAt(0).Kind) {
                        if (1 == aThrow.Children.ElementAt(0).Children.Count()) {
                           ISymbol iSymbol = null;
                           if (OperationKind.FieldReference == aThrow.Children.ElementAt(0).Children.ElementAt(0).Kind) {
                              var iFieldReference = aThrow.Children.ElementAt(0).Children.ElementAt(0) as IFieldReferenceOperation;
                              iSymbol = iFieldReference.Field;
                           } else if (OperationKind.LocalReference == aThrow.Children.ElementAt(0).Children.ElementAt(0).Kind) {
                              var iLocalReference = aThrow.Children.ElementAt(0).Children.ElementAt(0) as ILocalReferenceOperation;
                              iSymbol = iLocalReference.Local;
                           }

                           if (null != iSymbol) {
                              _exceptionVars.Remove(iSymbol);
                           }
                        }
                     }
                  }
                  _throws.Clear();

                  ISymbol violatingSymbol = null;
                  foreach (var exceptionVar in _exceptionVars) {

                     if (exceptionVar is IFieldSymbol) {
                        violatingSymbol = exceptionVar;
                     } else {
                        violatingSymbol = exceptionVar.ContainingSymbol;
                     }

                     var pos = exceptionVar.Locations.FirstOrDefault().GetMappedLineSpan();
                     AddViolation(violatingSymbol, new FileLinePositionSpan[] { pos });
                  }

                  foreach (var iSymbol in _symbol2ViolatingNodes.Keys) {
                     foreach (var pos in _symbol2ViolatingNodes[iSymbol]) {
                        AddViolation(iSymbol, new FileLinePositionSpan[] { pos });
                     }
                  }

                  _exceptionVars.Clear();
                  _symbol2ViolatingNodes.Clear();
                  _typeToIsException.Clear();
                  _excludedThrows.Clear();

               }
            } catch (Exception e) {
               Log.Warn("Exception while analyzing operation " + context.Operation.ToString(), e);
            }
         }
      }
   }
}
