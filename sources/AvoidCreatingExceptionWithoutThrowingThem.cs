using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.DotNet.CastDotNetExtension;
using CastDotNetExtension.Utils;

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
         context.RegisterSemanticModelAction(AnalyzeUsingSemanticModel);
      }

      private static bool IsException(INamedTypeSymbol iTypeIn,
         Compilation compilation,
         INamedTypeSymbol systemException,
         ref Dictionary<INamedTypeSymbol, bool> type2IsException
         )
      {
         bool isException = false;
         if (null != iTypeIn && null != systemException) {
            if (type2IsException.TryGetValue(iTypeIn, out isException)) {
               return isException;
            }

            isException = compilation.ClassifyConversion(iTypeIn, systemException).IsImplicit;
            type2IsException[iTypeIn] = isException;
         }
         return isException;
      }


      private void HandleOperation(IOperation operation, Compilation compilation, SemanticModel semanticModel, Context ctx)
      {
         if (OperationKind.ObjectCreation == operation.Kind) {
            var throwOp = null != operation.Parent && OperationKind.Throw == operation.Parent.Kind ?
               operation.Parent :
               OperationKind.Conversion == operation.Parent.Kind && null != operation.Parent.Parent &&
               OperationKind.Throw == operation.Parent.Parent.Kind ? operation.Parent.Parent : null;
            if (null == throwOp) {
               var line = operation.Syntax.GetLocation().GetMappedLineSpan().StartLinePosition.Line;
               var objCreationOperation = operation as IObjectCreationOperation;
               if (null != objCreationOperation && objCreationOperation.Type is INamedTypeSymbol) {
                  if (IsException(objCreationOperation.Type as INamedTypeSymbol, compilation, ctx.SystemException, ref ctx.TypeToIsException)) {
                     if (null != objCreationOperation.Parent) {
                        switch (objCreationOperation.Parent.Kind) {
                           case OperationKind.FieldInitializer: {
                                 var iFieldInitializer = objCreationOperation.Parent as IFieldInitializerOperation;
                                 foreach (ISymbol iField in iFieldInitializer.InitializedFields) {
                                    //ctx.ExceptionVars.Add(iField);
                                    ProcessExceptionVariable(ctx, operation, iField, semanticModel);
                                 }

                                 break;
                              }
                           case OperationKind.VariableInitializer: {
                                 if (null != objCreationOperation.Parent.Parent && OperationKind.VariableDeclarator == objCreationOperation.Parent.Parent.Kind) {
                                    if (null != objCreationOperation.Parent.Parent.Parent && OperationKind.VariableDeclaration == objCreationOperation.Parent.Parent.Parent.Kind) {
                                       var iVariableDeclaration = objCreationOperation.Parent.Parent.Parent as IVariableDeclarationOperation;
                                       foreach (var iVar in iVariableDeclaration.Declarators) {
                                          //ctx.ExceptionVars.Add(iVar.Symbol);
                                          ProcessExceptionVariable(ctx, operation, iVar.Symbol, semanticModel);
                                       }
                                    }
                                 }

                                 break;
                              }
                           default: {
                                 if (OperationKind.ExpressionStatement == operation.Parent.Kind) {

                                    HashSet<FileLinePositionSpan> positions = null;
                                    var symbol = semanticModel.GetEnclosingSymbol(operation.Parent.Syntax.GetLocation().SourceSpan.Start);
                                    if (null != symbol) {
                                       if (!ctx.Symbol2ViolatingNodes.TryGetValue(symbol, out positions)) {
                                          positions = new HashSet<FileLinePositionSpan>();
                                          ctx.Symbol2ViolatingNodes[symbol] = positions;
                                       }
                                       positions.Add(operation.Parent.Syntax.GetLocation().GetMappedLineSpan());
                                    }

                                 } else if (null != operation.Parent && (OperationKind.Conversion == operation.Parent.Kind &&
                                    null != operation.Parent.Parent && OperationKind.SimpleAssignment == operation.Parent.Parent.Kind ||
                                    OperationKind.SimpleAssignment == operation.Parent.Kind)) {
                                    var iSimpleAssignment = OperationKind.SimpleAssignment == operation.Parent.Kind ?
                                       operation.Parent as ISimpleAssignmentOperation : operation.Parent.Parent as ISimpleAssignmentOperation;
                                    if (null != iSimpleAssignment.Target) {
                                       ISymbol iSymbol = null;
                                       switch (iSimpleAssignment.Target.Kind) {
                                          case OperationKind.LocalReference:
                                             iSymbol = (iSimpleAssignment.Target as ILocalReferenceOperation).Local;
                                             break;
                                          case OperationKind.FieldReference:
                                             iSymbol = (iSimpleAssignment.Target as IFieldReferenceOperation).Field;
                                             break;
                                       }
                                       if (null != iSymbol) {
                                          //ctx.ExceptionVars.Add(iSymbol);
                                          ProcessExceptionVariable(ctx, operation, iSymbol, semanticModel);
                                       }
                                    }
                                 } else {
                                    Log.Debug("Unhandled condition: operation.Parent.Kind: " + operation.Parent.Kind);
                                 }

                                 break;
                              }
                        }
                     }
                  }
               }
            } else {
               ctx.ExcludedThrows.Add(throwOp);
            }
         } else if (OperationKind.Throw == operation.Kind && !ctx.ExcludedThrows.Contains(operation)) {
            ctx.Throws.Add(operation as IThrowOperation);
         }
      }

      private void ProcessExceptionVar(Context ctx, IOperation operation, ISymbol exceptionVar, ref bool ret) {
         IThrowOperation throwOp = null;
         foreach (var childOp in operation.Children) {
            if (childOp is ILocalReferenceOperation) {
               var childOpLocalRef = childOp as ILocalReferenceOperation;
               if (exceptionVar == childOpLocalRef.Local) {
                  throwOp = (null != childOp.Parent && OperationKind.Throw == childOp.Parent.Kind ?
                     childOp.Parent :
                     OperationKind.Conversion == childOp.Parent.Kind && null != childOp.Parent.Parent &&
                     OperationKind.Throw == childOp.Parent.Parent.Kind ? childOp.Parent.Parent : null)
                     as IThrowOperation;
                  ret = null != throwOp;
               }
               
            } else if (childOp is IThrowOperation) {
               throwOp = childOp as IThrowOperation;
            }

            if (null != throwOp) {
               ctx.ExcludedThrows.Add(throwOp);
            }
            ProcessExceptionVar(ctx, childOp, exceptionVar, ref ret);
         }
      }

      private void ProcessExceptionVariable(Context ctx, IOperation operation, ISymbol exceptionVar, SemanticModel semanticModel)
      {
         if (exceptionVar is ILocalSymbol && exceptionVar.ContainingSymbol is IMethodSymbol) {
            var syntax = operation.Syntax.Parent;
            while (null != syntax && !syntax.IsKind(SyntaxKind.MethodDeclaration)) {
               syntax = syntax.Parent;
            }

            if (null != syntax) {
               var methodDeclOp = semanticModel.GetOperation(syntax);
               if (null != methodDeclOp) {
                  bool ret = false;
                  ProcessExceptionVar(ctx, methodDeclOp, exceptionVar, ref ret);
                  if (!ret) {
                     ctx.ExceptionVars.Add(exceptionVar);
                  }
               }
            }
            
         } else {
            ctx.ExceptionVars.Add(exceptionVar);
         }
      }

      private void ProcessViolations(Context ctx)
      {
         int totalThrows = ctx.Throws.Count;
         int localRefs = 0, fieldRefs = 0;
         foreach (var aThrow in ctx.Throws) {
            if (1 == aThrow.Children.Count() && OperationKind.Conversion == aThrow.Children.ElementAt(0).Kind) {
               if (1 == aThrow.Children.ElementAt(0).Children.Count()) {
                  ISymbol iSymbol = null;
                  switch (aThrow.Children.ElementAt(0).Children.ElementAt(0).Kind) {
                     case OperationKind.FieldReference: {
                           var iFieldReference = aThrow.Children.ElementAt(0).Children.ElementAt(0) as IFieldReferenceOperation;
                           iSymbol = iFieldReference.Field;
                           fieldRefs++;
                           break;
                        }
                     case OperationKind.LocalReference: {
                           var iLocalReference = aThrow.Children.ElementAt(0).Children.ElementAt(0) as ILocalReferenceOperation;
                           Log.DebugFormat("Found local reference in {0} Pos: {1}",
                              iLocalReference.Syntax.SyntaxTree.FilePath, iLocalReference.Syntax.GetLocation().GetMappedLineSpan());
                           iSymbol = iLocalReference.Local;
                           
                           localRefs++;
                           break;
                        }
                  }

                  if (null != iSymbol) {
                     ctx.ExceptionVars.Remove(iSymbol);
                  }
               }
            }
         }

         if (1 < totalThrows) {
            Log.DebugFormat("Total Throws: {0} Local Refs: {1} Field Refs: {2}", totalThrows, localRefs, fieldRefs);
         }

         foreach (var exceptionVar in ctx.ExceptionVars) {
            ISymbol violatingSymbol = exceptionVar is IFieldSymbol ? exceptionVar : exceptionVar.ContainingSymbol;
            var pos = exceptionVar.Locations.FirstOrDefault().GetMappedLineSpan();
            AddViolation(violatingSymbol, new FileLinePositionSpan[] { pos });
         }

         foreach (var iSymbol in ctx.Symbol2ViolatingNodes.Keys) {
            foreach (var pos in ctx.Symbol2ViolatingNodes[iSymbol]) {
               AddViolation(iSymbol, new FileLinePositionSpan[] { pos });
            }
         }

      }

      private class Context
      {
         public HashSet<IOperation> ExcludedThrows = new HashSet<IOperation>();
         public List<IThrowOperation> Throws = new List<IThrowOperation>();
         public HashSet<ISymbol> ExceptionVars = new HashSet<ISymbol>();
         public Dictionary<INamedTypeSymbol, bool> TypeToIsException = new Dictionary<INamedTypeSymbol, bool>();
         public Dictionary<ISymbol, HashSet<FileLinePositionSpan>> Symbol2ViolatingNodes = new Dictionary<ISymbol, HashSet<FileLinePositionSpan>>();
         public INamedTypeSymbol SystemException = null;
         public Context(INamedTypeSymbol systemException)
         {
            SystemException = systemException;
         }
      }


      private IOperation GetOperation(SyntaxNode node, SemanticModel semanticModel) {
         IOperation iOperation = null;

         bool getOp = false;
         if (node.IsKind(SyntaxKind.ObjectCreationExpression)) {
            var kind = node.Parent.Kind();
            if (SyntaxKind.ThrowStatement != kind && SyntaxKind.ThrowExpression != kind) {
               getOp = true;
            }
         } else {
            var kind = node.Kind();
            if (SyntaxKind.ThrowStatement == kind ||  SyntaxKind.ThrowExpression == kind) {
               if (!node.ChildNodes().FirstOrDefault().IsKind(SyntaxKind.ObjectCreationExpression)) {
                  getOp = true;
               }
            }
         }

         if (getOp) {
            iOperation = semanticModel.GetOperation(node);
         }

         return iOperation;
      }

      private HashSet<string> _analyzedFiles = new HashSet<string>();
      private object _lock = new object();

      private void AnalyzeUsingSemanticModel(SemanticModelAnalysisContext context)
      {

         try {
            bool alreadyProcessed = false;
            lock (_lock) {
               alreadyProcessed = _analyzedFiles.Contains(context.SemanticModel.SyntaxTree.FilePath);
            }

            if (!alreadyProcessed) {
               _analyzedFiles.Add(context.SemanticModel.SyntaxTree.FilePath);

               HashSet<SyntaxKind> syntaxKinds = new HashSet<SyntaxKind> {
                  SyntaxKind.ObjectCreationExpression,
                  SyntaxKind.ThrowExpression,
                  SyntaxKind.ThrowStatement
               };

               HashSet<OperationKind> opKinds = new HashSet<OperationKind> { OperationKind.ObjectCreation, OperationKind.Throw };

               var compilation = context.SemanticModel.Compilation;
               Context ctx = new Context(compilation.GetTypeByMetadataName("System.Exception"));

               var root = context.SemanticModel.SyntaxTree.GetRoot(context.CancellationToken);               
               var nodes = root.DescendantNodesAndSelf().Where(n => n.IsKind(syntaxKinds));

               foreach (var node in nodes) {
                  var operation = GetOperation(node, context.SemanticModel);
                  if (operation.IsKind(opKinds)) {
                     HandleOperation(operation, context.SemanticModel.Compilation, context.SemanticModel, ctx);
                  }
               }

               ProcessViolations(ctx);
            } else {
               Log.DebugFormat("File already analyzed skipping: {0}", context.SemanticModel.SyntaxTree.FilePath);
            }

         } catch (Exception e) {
            Log.Warn("Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath, e);
         }
      }
   }
}
