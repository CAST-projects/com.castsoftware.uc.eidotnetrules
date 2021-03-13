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
   public class AvoidCreatingExceptionWithoutThrowingThem : OperationsRetriever
   {



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


      //~AvoidCreatingExceptionWithoutThrowingThem()
      //{
      //   Console.WriteLine("=====================AvoidCreatingExceptionWithoutThrowingThem=====================");
      //   Console.WriteLine(PerfDatum.Headers);
      //   int count = 0, objCreations = 0, throwOps = 0;
      //   long time = 0;
      //   foreach (var perfDatum in _perfData) {
      //      Console.WriteLine(perfDatum.ToString());
      //      count++;
      //      time += perfDatum.Time;
      //      objCreations += perfDatum.ObjCreations;
      //      throwOps += perfDatum.ThrowOps;
      //   }
      //   Console.WriteLine(new PerfDatum(time / TimeSpan.TicksPerMillisecond, "<All>", objCreations, throwOps, count).ToString());
      //}

      private ConcurrentDictionary<string, Context> _fileToContext = new ConcurrentDictionary<string, Context>();
      public override void Init(AnalysisContext context)
      {
         _fileToContext.Clear();
         base.Init(context);
      }

      public override SyntaxKind[] Kinds(CompilationStartAnalysisContext context)
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
               return SyntaxKinds.ToArray();
            } else {
               Log.WarnFormat("Could not get type for System.Exception while analyzing {0}. QR \"{1}\" will be disabled for this project.",
                  context.Compilation.AssemblyName, this.GetRuleName());
            }
         } catch (Exception e) {
            Log.Warn("Exception while analyzing " + context.Compilation.AssemblyName, e);
         }
         return new SyntaxKind[] { };

      }


      private List<PerfDatum> _perfData = new List<PerfDatum>();


      public override void HandleSemanticModelOps(SemanticModelAnalysisContext context,
            OPs ops)
      {
         //var watch = new System.Diagnostics.Stopwatch();
         //watch.Start();
         List<IOperation> objCreationOps = ops[OperationKind.ObjectCreation].Operations;
         //int objCreationsCount = objCreationOps.Count;
         //int throwOpsCount = 0, exceptionVars = 0;
         /*if (ops.TryGetValue(OperationKind.ObjectCreation, out objCreationOps))*/ {
            //Log.WarnFormat("Received Object Creation Ops. Count: {0} File: {1} ", objCreationOps.Count, context.SemanticModel.SyntaxTree.FilePath);
            Context ctx = new Context(context.SemanticModel.Compilation, context.SemanticModel);

            ProcessObjectCreationOps(objCreationOps, ctx);

            if (ctx.ExceptionVars.Any()) {
               //exceptionVars = ctx.ExceptionVars.Count;
               //Log.WarnFormat("Have exception vars. Count: {0} File: {1} ", ctx.ExceptionVars.Count, context.SemanticModel.SyntaxTree.FilePath);
               ctx.Throws = ops[OperationKind.Throw].Operations;
               //throwOpsCount = ctx.Throws.Count;
               /*if (ops.TryGetValue(OperationKind.Throw, out throwOps)) {
                  //Log.WarnFormat("Have Throw ops. Count: {0} File: {1} ", throwOps.Count, context.SemanticModel.SyntaxTree.FilePath);
               }*/
               ProcessViolations(ctx);
            }
         }
         //Console.WriteLine("Obj Creation: {0} Throw: {1} Vars: {2}", objCreationsCount, throwOpsCount, exceptionVars);
         //watch.Stop();
         //lock (_perfData) {
         //   _perfData.Add(new PerfDatum(watch.ElapsedTicks, context.SemanticModel.SyntaxTree.FilePath, objCreationsCount, throwOpsCount));
         //}
      }


      
      public override void HandleOperations(SemanticModelAnalysisContext context,
         ConcurrentQueue<OperationDetails> ops)
      {
         
            Context ctx = new Context(context.SemanticModel.Compilation, context.SemanticModel);

            IEnumerator<OperationDetails> enumertor = null;
            OperationDetails opDetails = null;

            if (null == (enumertor = ops.GetEnumerator())) {
               Log.WarnFormat("Could not get enumerator for file {0}! It will not have violations for {1}.",
                  context.SemanticModel.SyntaxTree.FilePath, GetRuleName());
            } else {

               while (enumertor.MoveNext()) {
                  opDetails = enumertor.Current;
                  if (null != opDetails) {
                     /* if (ctx.AnalysisDone) {
                        Console.WriteLine("after ctx.AnalysisDone");
                     } else {
                  Console.WriteLine("Kind: {0} Line: {1} Code: {2}",
                     opDetails.Operation.Kind, opDetails.Operation.Syntax.GetLocation().GetMappedLineSpan().StartLinePosition.Line,
                     opDetails.Operation.Syntax);
               }*/
                     switch (opDetails.Operation.Kind) {
                        case OperationKind.ObjectCreation:
                           ctx.ObjectCreationVars.Add(opDetails.Operation);
                           ProcessObjCreationOp(opDetails.Operation as IObjectCreationOperation, ctx);
                           break;
                        case OperationKind.Throw:
                           ctx.Throws.Add(opDetails.Operation);
                           break;
                     }
                  }
               }

               ctx.AnalysisDone = true;
               //AddFileVerificationData(context.SemanticModel.SyntaxTree.FilePath, ctx.ObjectCreationVars.Count, ctx.Throws.Count);
               //Console.WriteLine("AvoidCreatingExceptionWithoutThrowingThem: Obj Creation: {0} Throw: {1} File: {2}",
               //   ctx.ObjectCreationVars.Count, ctx.Throws.Count, context.SemanticModel.SyntaxTree.FilePath);
               ProcessViolations(ctx);

            }            


         //Console.WriteLine("AvoidCreatingExceptionWithoutThrowingThem: Thread ID: " + Thread.CurrentThread.ManagedThreadId);
      }


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
         public static ConcurrentDictionary<INamedTypeSymbol, bool> TypeToIsException = new ConcurrentDictionary<INamedTypeSymbol, bool>();
         public static INamedTypeSymbol SystemException = null;
         public static INamedTypeSymbol Interface_Exception = null;

         public bool AnalysisDone = false;
         public List<IOperation> ObjectCreationVars = new List<IOperation>();
         public HashSet<IOperation> ExcludedThrows = new HashSet<IOperation>();
         public List<IOperation> Throws = new List<IOperation>();
         public HashSet<ISymbol> ExceptionVars = new HashSet<ISymbol>();
         public Dictionary<ISymbol, HashSet<FileLinePositionSpan>> Symbol2ViolatingNodes = new Dictionary<ISymbol, HashSet<FileLinePositionSpan>>();

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

                  ProcessObjectCreationOps(opDetails.ObjCreationOps, ctx);

                  if (ctx.ExceptionVars.Any()) {
                     //ctx.Throws = opDetails.ThrowOps;
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
         bool isException = false;
         if (SpecialType.None == iTypeIn.SpecialType) {
            isException = Context.SystemException == iTypeIn;
            if (!isException && iTypeIn.TypeKind == TypeKind.Class && !iTypeIn.IsAnonymousType) {
               if (null != Context.Interface_Exception) {
                  isException = Context.Interface_Exception == iTypeIn.AllInterfaces.ElementAtOrDefault(1);
                  Context.TypeToIsException[iTypeIn] = isException;
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
                  Context.TypeToIsException[iTypeIn] = isException;
               }
            }
         }
         return isException;
      }


      private void ProcessObjCreationOp(IObjectCreationOperation objCreationOperation, Context ctx) {
         var throwOp = null != objCreationOperation.Parent && OperationKind.Throw == objCreationOperation.Parent.Kind ?
            objCreationOperation.Parent :
            OperationKind.Conversion == objCreationOperation.Parent.Kind && null != objCreationOperation.Parent.Parent &&
            OperationKind.Throw == objCreationOperation.Parent.Parent.Kind ? objCreationOperation.Parent.Parent : null;
         if (null == throwOp) {
            //var line = objCreationOperation.Syntax.GetLocation().GetMappedLineSpan().StartLinePosition.Line;
            if (null != objCreationOperation && objCreationOperation.Type is INamedTypeSymbol) {
               if (IsException(objCreationOperation.Type as INamedTypeSymbol, ctx)) {
                  if (null != objCreationOperation.Parent) {
                     if (OperationKind.ExpressionStatement == objCreationOperation.Parent.Kind) {
                        HashSet<FileLinePositionSpan> positions = null;
                        var symbol = ctx.SemanticModel.GetEnclosingSymbol(objCreationOperation.Parent.Syntax.GetLocation().SourceSpan.Start);
                        if (null != symbol) {
                           if (!ctx.Symbol2ViolatingNodes.TryGetValue(symbol, out positions)) {
                              positions = new HashSet<FileLinePositionSpan>();
                              ctx.Symbol2ViolatingNodes[symbol] = positions;
                           }
                           positions.Add(objCreationOperation.Parent.Syntax.GetLocation().GetMappedLineSpan());
                        }
                     } else {
                        ctx.ExceptionVars.UnionWith(objCreationOperation.Parent.GetInitializedSymbols());
                     }
                  }
               }
            }
         } else {
            ctx.ExcludedThrows.Add(throwOp);
         }

      }

      private void ProcessObjectCreationOps(List<IOperation> objCreationOps, Context ctx)
      {
         foreach (var objCreationOperation in objCreationOps) {
            var throwOp = null != objCreationOperation.Parent && OperationKind.Throw == objCreationOperation.Parent.Kind ?
               objCreationOperation.Parent :
               OperationKind.Conversion == objCreationOperation.Parent.Kind && null != objCreationOperation.Parent.Parent &&
               OperationKind.Throw == objCreationOperation.Parent.Parent.Kind ? objCreationOperation.Parent.Parent : null;
            if (null == throwOp) {
               //var line = objCreationOperation.Syntax.GetLocation().GetMappedLineSpan().StartLinePosition.Line;
               if (null != objCreationOperation && objCreationOperation.Type is INamedTypeSymbol) {
                  if (IsException(objCreationOperation.Type as INamedTypeSymbol, ctx)) {
                     if (null != objCreationOperation.Parent) {
                        if (OperationKind.ExpressionStatement == objCreationOperation.Parent.Kind) {
                           HashSet<FileLinePositionSpan> positions = null;
                           var symbol = ctx.SemanticModel.GetEnclosingSymbol(objCreationOperation.Parent.Syntax.GetLocation().SourceSpan.Start);
                           if (null != symbol) {
                              if (!ctx.Symbol2ViolatingNodes.TryGetValue(symbol, out positions)) {
                                 positions = new HashSet<FileLinePositionSpan>();
                                 ctx.Symbol2ViolatingNodes[symbol] = positions;
                              }
                              positions.Add(objCreationOperation.Parent.Syntax.GetLocation().GetMappedLineSpan());
                           }
                        } else {
                           ctx.ExceptionVars.UnionWith(objCreationOperation.Parent.GetInitializedSymbols());
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
