using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.DotNet.CastDotNetExtension;
using CastDotNetExtension.Utils;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;


namespace CastDotNetExtension {

   /// <summary>
   /// This class processes QR "Avoid creating exception without throwing them"
   /// It is different from implementations of other QRs because of Performance requirement.
   /// Usualy, we would use RegisterSemanticModelAction. That presents with following problem:
   ///   Given an object creation expression how do we get which is the target type?
   ///      Obvious anser is SemanticModel.GetSymbolInfo(). That happens to be pretty expensive.
   ///         Another way is GetTypeInfo() but that doesn't always work.
   ///      We cannot exclude call to GetSymbolInfo() because we only need to consider object creation
   ///      expression if it is of exception type.
   /// RegisterOperationAction gives the information we need with type already determined by Roslyn.
   ///   why complicated design or what other simpler methods other than producer\consumer model has been tried?
   ///      - use RegisterSemanticModelAction - see above.
   ///      - use RegisterSemanticModelAction with RegisterOperationAction *based on assumption* that 
   ///         semantic model call back will always be _after_ everything else including exception:
   ///            Unfortunately, in Roslyn concurrent analysis model it cannot be guaranteed.
   ///      - Do everything in RegisterOperationAction - This requires that we use locks. But the number 
   ///         of times lock has to be acquired negates the availability of symbol being identified by Roslyn.
   /// Current Design:
   ///   - Producer: In RegisterOperationAction() callback, we get all types of operations that are of type "OperationKinds"
   ///   - Consumer: In RegisterSemanticMmodel() callback, we get all the expression of "Kinds". We poll until all the 
   ///               expressions have been received. This assumes that SyntaxKind => OperationKind mapping is complete.
   ///   Once consumer receives all the nodes, it processes object creation statements that are not child of throw statement
   ///   and of exception type. Removing exception type variables that are thrown (or just "new Exception()" without LHS)
   ///   are given as violation.
   ///   
   /// Given applications vs LoC following is the performance:
   /// LoC              non producer\consumer model (1.0.0-alpha1)               Producer\Consumer model
   /// 392385                      6 seconds                                           1 second
   /// 2266988                     42 seconds                                          5 seconds
   /// </summary>
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

      private static readonly OperationKind[] OperationKinds =  {
                                                          OperationKind.Throw, 
                                                          OperationKind.ObjectCreation, 
                                                          OperationKind.DynamicObjectCreation, 
                                                          OperationKind.Invalid, 
                                                          OperationKind.DelegateCreation, 
                                                          OperationKind.TypeParameterObjectCreation,
                                                          OperationKind.End,
                                                          //OperationKind.ArrayCreation, 
                                                          //OperationKind.AnonymousObjectCreation,
                                                       };

      private static readonly HashSet<SyntaxKind> SyntaxKinds = new HashSet<SyntaxKind> {
               SyntaxKind.ThrowStatement,
               SyntaxKind.ThrowExpression,
               SyntaxKind.ObjectCreationExpression,
            };

      private class OpDetails
      {
         public List<IOperation> ObjCreationOps { get; private set; }
         public List<IThrowOperation> ThrowOps { get; private set; }
         public List<IOperation> OtherOps { get; private set; }
         public int Count { get; private set; }
         public bool LastNodeFound { get; private set; }
         public int TotalOps { get; private set; }
         private HashSet<SyntaxNode> _nodes;

         public OpDetails(HashSet<SyntaxNode> nodes)
         {
            ObjCreationOps = new List<IOperation>();
            ThrowOps = new List<IThrowOperation>();
            OtherOps = new List<IOperation>();
            _nodes = nodes;
            Count = _nodes.Count;
            LastNodeFound = Count == 0;
            TotalOps = 0;
         }

         public void AddOperation(IOperation op)
         {
            if (_nodes.Remove(op.Syntax)) {
               switch (op.Kind) {
                  case OperationKind.ObjectCreation:
                     ObjCreationOps.Add(op);
                     break;
                  case OperationKind.Throw:
                     ThrowOps.Add(op as IThrowOperation);
                     break;
                  default:
                     //OtherOps.Add(op);
                     break;
               }
               TotalOps++;
               LastNodeFound = Count == TotalOps ? true : false;
            }
         }
      }

      private class Context
      {
         public HashSet<IOperation> ExcludedThrows = new HashSet<IOperation>();
         public List<IThrowOperation> Throws = new List<IThrowOperation>();
         public HashSet<ISymbol> ExceptionVars = new HashSet<ISymbol>();
         public static ConcurrentDictionary<INamedTypeSymbol, bool> TypeToIsException = new ConcurrentDictionary<INamedTypeSymbol, bool>();
         public Dictionary<ISymbol, HashSet<FileLinePositionSpan>> Symbol2ViolatingNodes = new Dictionary<ISymbol, HashSet<FileLinePositionSpan>>();
         public static INamedTypeSymbol SystemException = null;
         public static INamedTypeSymbol Interface_Exception = null;
         public Compilation Compilation;
         public SemanticModel SemanticModel;
         //public static long ConversionCount = 0;
         //public static long StoredCount = 0;

         public Context(Compilation compilation, SemanticModel semanticModel)
         {
            Compilation = compilation;
            SemanticModel = semanticModel;
         }
      }

      private ConcurrentDictionary<string, ConcurrentQueue<IOperation>> _operations =
         new ConcurrentDictionary<string, ConcurrentQueue<IOperation>>();


      public override void Init(AnalysisContext context) {
         context.RegisterCompilationStartAction(OnStartCompilation);
      }

      private void AnalyzeOperation(OperationAnalysisContext context)
      {
         var q = _operations.GetOrAdd(context.Operation.Syntax.SyntaxTree.FilePath, (key) => new ConcurrentQueue<IOperation>());
         q.Enqueue(context.Operation);
      }

      private void AnalyzeUsingSemanticModel(SemanticModelAnalysisContext context)
      {
         try {
            var nodes =
                  context.SemanticModel.SyntaxTree.GetRoot().DescendantNodes().Where(n => SyntaxKinds.Contains(n.Kind())).ToHashSet();

            if (nodes.Any()) {
               //Console.WriteLine("Starting analysis of {0}", context.SemanticModel.SyntaxTree.FilePath);
               ConcurrentQueue<IOperation> operations = null;
               IOperation operation = null;
               OpDetails opDetails = new OpDetails(nodes);

               //var watch = new System.Diagnostics.Stopwatch();
               //watch.Start();
               while (_operations.TryGetValue(context.SemanticModel.SyntaxTree.FilePath, out operations)) {
                  while (operations.TryDequeue(out operation)) {
                     if (OperationKind.End == operation.Kind) {
                        break;
                     }
                     opDetails.AddOperation(operation);
                     if (opDetails.LastNodeFound) {
                        break;
                     }
                  }
                  if (null == operation) {
                     Log.Debug("operation null; trying again!");
                     continue;
                  }
                  if (OperationKind.End == operation.Kind || opDetails.LastNodeFound) {
                     break;
                  }
               }

               if (opDetails.ObjCreationOps.Any()) {
                  Context ctx = new Context(context.SemanticModel.Compilation, context.SemanticModel);

                  ProcessObjectCreationOps(opDetails, ctx);

                  if (ctx.ExceptionVars.Any()) {
                     ctx.Throws = opDetails.ThrowOps;
                     ProcessViolations(ctx);
                  }
               }
               //watch.Stop();
               //Log.WarnFormat("Count And Total Time: {0},{1},{2},{3},\"{4}\"",
               //   opDetails.Count, opDetails.ObjCreationOps.Count, 
               //   opDetails.ThrowOps.Count, watch.ElapsedMilliseconds,
               //   context.SemanticModel.SyntaxTree.FilePath);
            }
         } catch (Exception e) {
            Log.Warn("Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath, e);
         }
      }

      private void ProcessViolations(Context ctx)
      {
         int totalThrows = ctx.Throws.Count;
         int localRefs = 0, fieldRefs = 0;
         foreach (var aThrow in ctx.Throws) {
            if (!ctx.ExcludedThrows.Contains(aThrow) && 
               1 == aThrow.Children.Count() && OperationKind.Conversion == aThrow.Children.ElementAt(0).Kind) {
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

      private void OnStartCompilation(CompilationStartAnalysisContext context)
      {
         try {
            _operations.Clear();
            Context.TypeToIsException.Clear();
            Context.SystemException = context.Compilation.GetTypeByMetadataName("System.Exception");
            Context.Interface_Exception = context.Compilation.GetTypeByMetadataName("System.Runtime.InteropServices._Exception");
            if (null == Context.Interface_Exception) {
               Log.WarnFormat("Could not get type for System.Runtime.InteropServices._Exception while analyzing {0}.",
                  context.Compilation.AssemblyName);
            }
            if (null != Context.SystemException) {
               context.RegisterOperationAction(AnalyzeOperation, OperationKinds);
               context.RegisterSemanticModelAction(AnalyzeUsingSemanticModel);
            } else {
               Log.WarnFormat("Could not get type for System.Exception while analyzing {0}. QR \"{1}\" will be disabled for this project.",
                  context.Compilation.AssemblyName, this.GetRuleName());
            }
         } catch (Exception e) {
            Log.Warn("Exception while analyzing " + context.Compilation.AssemblyName, e);
         }
      }


      private static bool IsException(INamedTypeSymbol iTypeIn, Context ctx)
      {
         bool isException = Context.SystemException == iTypeIn;
         if (!isException && iTypeIn.TypeKind == TypeKind.Class && !iTypeIn.IsAnonymousType) {
            if (null != Context.Interface_Exception) {
               isException = Context.Interface_Exception == iTypeIn.AllInterfaces.ElementAtOrDefault(1);
            } else if (Context.TypeToIsException.TryGetValue(iTypeIn, out isException)) {
               //Interlocked.Increment(ref Context.StoredCount);
            } else {
               //little expensive => isException = ctx.Compilation.ClassifyConversion(iTypeIn, Context.SystemException).IsImplicit;

               //Interlocked.Increment(ref Context.ConversionCount);
               var baseType = iTypeIn.BaseType;
               while (null != baseType && Context.SystemException != baseType) {
                  baseType = baseType.BaseType;
               }
               isException = null != baseType;
            }
            Context.TypeToIsException[iTypeIn] = isException;
         }
         return isException;
      }


      private void ProcessObjectCreationOps(OpDetails opDetails, Context ctx)
      {
         foreach (var objCreationOperation in opDetails.ObjCreationOps) {
            var throwOp = null != objCreationOperation.Parent && OperationKind.Throw == objCreationOperation.Parent.Kind ?
               objCreationOperation.Parent :
               OperationKind.Conversion == objCreationOperation.Parent.Kind && null != objCreationOperation.Parent.Parent &&
               OperationKind.Throw == objCreationOperation.Parent.Parent.Kind ? objCreationOperation.Parent.Parent : null;
            if (null == throwOp) {
               var line = objCreationOperation.Syntax.GetLocation().GetMappedLineSpan().StartLinePosition.Line;
               if (null != objCreationOperation && objCreationOperation.Type is INamedTypeSymbol) {
                  if (IsException(objCreationOperation.Type as INamedTypeSymbol, ctx)) {
                     if (null != objCreationOperation.Parent) {
                        switch (objCreationOperation.Parent.Kind) {
                           case OperationKind.FieldInitializer: {
                                 var iFieldInitializer = objCreationOperation.Parent as IFieldInitializerOperation;
                                 foreach (ISymbol iField in iFieldInitializer.InitializedFields) {
                                    ctx.ExceptionVars.Add(iField);
                                 }

                                 break;
                              }
                           case OperationKind.VariableInitializer: {
                                 if (null != objCreationOperation.Parent.Parent && OperationKind.VariableDeclarator == objCreationOperation.Parent.Parent.Kind) {
                                    if (null != objCreationOperation.Parent.Parent.Parent && OperationKind.VariableDeclaration == objCreationOperation.Parent.Parent.Parent.Kind) {
                                       var iVariableDeclaration = objCreationOperation.Parent.Parent.Parent as IVariableDeclarationOperation;
                                       foreach (var iVar in iVariableDeclaration.Declarators) {
                                          ctx.ExceptionVars.Add(iVar.Symbol);
                                       }
                                    }
                                 }

                                 break;
                              }
                           case OperationKind.ExpressionStatement: {
                                 HashSet<FileLinePositionSpan> positions = null;
                                 var symbol = ctx.SemanticModel.GetEnclosingSymbol(objCreationOperation.Parent.Syntax.GetLocation().SourceSpan.Start);
                                 if (null != symbol) {
                                    if (!ctx.Symbol2ViolatingNodes.TryGetValue(symbol, out positions)) {
                                       positions = new HashSet<FileLinePositionSpan>();
                                       ctx.Symbol2ViolatingNodes[symbol] = positions;
                                    }
                                    positions.Add(objCreationOperation.Parent.Syntax.GetLocation().GetMappedLineSpan());
                                 }
                                 break;
                              }
                           case OperationKind.Conversion:
                           case OperationKind.SimpleAssignment:
                              ISimpleAssignmentOperation simpleAssignment = OperationKind.SimpleAssignment == objCreationOperation.Parent.Kind ?
                                 objCreationOperation.Parent as ISimpleAssignmentOperation :
                                    null != objCreationOperation.Parent.Parent && OperationKind.SimpleAssignment == objCreationOperation.Parent.Parent.Kind ?
                                       objCreationOperation.Parent.Parent as ISimpleAssignmentOperation : null;
                              if (null != simpleAssignment) {
                                 ISymbol iSymbol = null;
                                 switch (simpleAssignment.Target.Kind) {
                                    case OperationKind.LocalReference:
                                       iSymbol = (simpleAssignment.Target as ILocalReferenceOperation).Local;
                                       break;
                                    case OperationKind.FieldReference:
                                       iSymbol = (simpleAssignment.Target as IFieldReferenceOperation).Field;
                                       break;
                                 }
                                 if (null != iSymbol) {
                                    ctx.ExceptionVars.Add(iSymbol);
                                 }
                              }

                              break;
                        }
                     }
                  }
               }
            } else {
               ctx.ExcludedThrows.Add(throwOp);
            }
         }
      }
   }
}
