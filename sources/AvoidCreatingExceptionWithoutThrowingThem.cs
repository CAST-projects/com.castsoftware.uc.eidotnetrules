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

      private ConcurrentDictionary<string, ConcurrentQueue<IOperation>> _operations =
         new ConcurrentDictionary<string, ConcurrentQueue<IOperation>>();

      private Dictionary<string, bool> _analyzedFiles = new Dictionary<string, bool>();
      private readonly object _lock = new object();

        /// <summary>
        /// Initialize the QR with the given context and register all the syntax nodes
        /// to listen during the visit and provide a specific callback for each one
        /// </summary>
        /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         //context.RegisterCompilationStartAction(OnStartCompilation);
         context.RegisterSemanticModelAction(AnalyzeUsingSemanticModel);
      }

      private void AnalyzeOperation(OperationAnalysisContext context)
      {
         var q = _operations.GetOrAdd(context.Operation.Syntax.SyntaxTree.FilePath, (key) => new ConcurrentQueue<IOperation>());
         q.Enqueue(context.Operation);
      }


      private class ViolationDetector : OperationWalker
      {
         public List<IObjectCreationOperation> ObjectCreations = new List<IObjectCreationOperation>();
         public List<IThrowOperation> Throws = new List<IThrowOperation>();
         public override void VisitObjectCreation(IObjectCreationOperation operation)
         {
            ObjectCreations.Add(operation);
            base.VisitObjectCreation(operation);
         }

         public override void VisitThrow(IThrowOperation operation)
         {
            Throws.Add(operation);
            base.VisitThrow(operation);
         }

         public override void VisitEnd(IEndOperation operation)
         {
            Console.WriteLine("====> Got end!");
            base.VisitEnd(operation);
         }
      }

      private void AnalyzeUsingSemanticModel(SemanticModelAnalysisContext context)
      {
         var watch = new System.Diagnostics.Stopwatch();
         watch.Start();
         ViolationDetector detector = new ViolationDetector();
         foreach (var node in context.SemanticModel.SyntaxTree.GetRoot().DescendantNodes()) {
            IOperation iOperation = context.SemanticModel.GetOperation(node);
            if (null != iOperation) {
               detector.Visit(iOperation);
            }
         }
         watch.Stop();
         Console.WriteLine("Time: {0}", watch.ElapsedMilliseconds); //<== very expensive

      }

      //private void AnalyzeUsingSemanticModel(SemanticModelAnalysisContext context)
      //{
      //   try {
      //      var nodes =
      //            context.SemanticModel.SyntaxTree.GetRoot().DescendantNodes().Where(n => SyntaxKinds.Contains(n.Kind())).ToHashSet();

      //      if (nodes.Any()) {
      //         //Console.WriteLine("Starting analysis of {0}", context.SemanticModel.SyntaxTree.FilePath);
      //         ConcurrentQueue<IOperation> operations = null;
      //         IOperation operation = null;
      //         OpDetails opDetails = new OpDetails(nodes);

      //         var watch = new System.Diagnostics.Stopwatch();
      //         watch.Start();
      //         long nodeRetrievalTime = 0, objCreationProcessingTime = 0, violationProcessingTime = 0;
      //         while (_operations.TryGetValue(context.SemanticModel.SyntaxTree.FilePath, out operations)) {
      //            while (operations.TryDequeue(out operation)) {
      //               if (OperationKind.End == operation.Kind) {
      //                  break;
      //               }
      //               opDetails.AddOperation(operation);
      //               if (opDetails.LastNodeFound) {
      //                  break;
      //               }
      //            }
      //            if (null == operation) {
      //               Log.Debug("operation null; trying again!");
      //               continue;
      //            }
      //            if (OperationKind.End == operation.Kind || opDetails.LastNodeFound) {
      //               break;
      //            }
      //         }
      //         watch.Stop();
      //         nodeRetrievalTime = watch.ElapsedMilliseconds;

      //         if (opDetails.ObjCreationOps.Any()) {
      //            Context ctx = new Context(context.SemanticModel.Compilation, context.SemanticModel, null);

      //            watch.Start();
      //            ProcessObjectCreationOps(opDetails, ctx);
      //            watch.Stop();
      //            objCreationProcessingTime = watch.ElapsedMilliseconds;

      //            if (ctx.ExceptionVars.Any()) {
      //               ctx.Throws = opDetails.ThrowOps;
      //               watch.Start();
      //               ProcessViolations(ctx);
      //               watch.Stop();
      //               violationProcessingTime = watch.ElapsedMilliseconds;
      //            }
      //         }
      //         //Console.WriteLine("Total: {0} Object Creations: {1} Throws: {2} Time: {3} File: {4}",
      //         Console.WriteLine("Count And Times: {0},{1},{2},{3},{4},{5},{6},\"{7}\"",
      //            opDetails.Count, nodeRetrievalTime, opDetails.ObjCreationOps.Count, objCreationProcessingTime,
      //            opDetails.ThrowOps.Count, violationProcessingTime, nodeRetrievalTime + objCreationProcessingTime + violationProcessingTime,
      //            context.SemanticModel.SyntaxTree.FilePath);

      //      }

      //   } catch (Exception e) {
      //      Log.Warn("Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath, e);
      //   }
      //}

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

      //public class GetAllExceptionTypesVisitor : SymbolVisitor
      //{
      //   public HashSet<INamedTypeSymbol> AllExceptionTypes { get; private set; }
      //   public HashSet<INamedTypeSymbol> AllNonExceptionTypes { get; private set; }
      //   private Compilation _compilation;
      //   private INamedTypeSymbol _systemException;
      //   public int TotalVisited { get; private set; }
      //   public GetAllExceptionTypesVisitor(Compilation compilation, INamedTypeSymbol systemException)
      //   {
      //      _compilation = compilation;
      //      _systemException = systemException;
      //      AllExceptionTypes = new HashSet<INamedTypeSymbol> { _systemException };
      //      AllNonExceptionTypes = new HashSet<INamedTypeSymbol> ();
      //   }

      //   public override void VisitNamespace(INamespaceSymbol symbol)
      //   {
      //      foreach (var member in symbol.GetMembers()) {
      //         member.Accept(this);
      //      }
      //   }

      //   public override void VisitNamedType(INamedTypeSymbol symbol)
      //   {
      //      TotalVisited++;
      //      if (null != symbol.BaseType) {
      //         if (AllExceptionTypes.Contains(symbol.BaseType)) {
      //            AllExceptionTypes.Add(symbol);
      //         } else if (AllNonExceptionTypes.Contains(symbol.BaseType)) {
      //            AllNonExceptionTypes.Add(symbol);
      //         } else {
      //            var parent = symbol.BaseType;
      //            while (null != parent && _systemException != parent) {
      //               parent = parent.BaseType;
      //            }

      //            if (null != parent) {
      //               AllExceptionTypes.Add(symbol);
      //            } else {
      //               AllNonExceptionTypes.Add(symbol);
      //            }
      //            //if (_compilation.ClassifyCommonConversion(symbol, _systemException).IsImplicit) {
      //            //   AllExceptionTypes.Add(symbol);
      //            //}
      //         }
      //      }
      //      foreach (var member in symbol.GetMembers()) {
      //         member.Accept(this);
      //      }
      //   }
      //}


      //private void OnPostCompilation(CompilationAnalysisContext context)
      //{
      //   var watch = new System.Diagnostics.Stopwatch();
      //   int totalVisited = 0;
      //   watch.Start();
      //   Context.AllExceptionTypes = null;
      //   var systemException = context.Compilation.GetTypeByMetadataName("System.Exception");
      //   if (null != systemException) {
      //      GetAllExceptionTypesVisitor visitor = new GetAllExceptionTypesVisitor(context.Compilation, systemException);
      //      visitor.Visit(context.Compilation.Assembly.GlobalNamespace);

      //      foreach (var reference in context.Compilation.References) {
      //         var referencedAssembly =
      //            context.Compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
      //         if (null != referencedAssembly) {
      //            visitor.Visit(referencedAssembly.GlobalNamespace);
      //         }
      //      }
      //      Context.AllExceptionTypes = visitor.AllExceptionTypes;
      //      totalVisited = visitor.TotalVisited;
      //   }

      //   if (null == systemException || !Context.AllExceptionTypes.Any()) {
            
      //   }
      //   watch.Stop();
      //   Console.WriteLine("Number of types visited: {0} Number of exception types: {1} Time: {2}",
      //      totalVisited, Context.AllExceptionTypes.Count, watch.ElapsedMilliseconds);
      //}


      ~AvoidCreatingExceptionWithoutThrowingThem()
      {
         Console.WriteLine("Conversions: {0} Stored: {1}", Context.ConversionCount, Context.StoredCount);
         
         //foreach (var perf in PerfData) {
         //   Console.WriteLine("Title: {0} Count: {1} Time: {2} Ops: {3} ConversionCount: {4} StoredCount: {5}", 
         //      perf.Title, perf.Count, perf.Time, perf.Ops, perf.ConversionCount, perf.StoredCount);
         //}

         //foreach (var name in _symbolsNotFound) {
         //   Console.WriteLine("Didn't find symbol for {0}", name);
         //}
         //Dictionary<OperationKind, string> kinds = new Dictionary<OperationKind, string>();
         //foreach (var item in _fileToOpDetails) {
         //   if (!item.Value.LastNodeFound) {
         //      Console.WriteLine("Last Node not found. Original Count {0} Operations: {1} File: {2}", 
         //         item.Value.Count, item.Value.TotalOps, item.Value.ObjCreationOps.First().Syntax.SyntaxTree.FilePath);
         //      //foreach (var node in item.Value.Nodes) {
         //      //   var operation = item.Value.SemanticModel.GetOperation(node);
         //      //   if (null != operation) {
         //      //      kinds[operation.Kind] = node.ToFullString();
         //      //   } else {
         //      //      Console.WriteLine("   Operation: {0} Code: {1}",
         //      //         (null == operation) ? "No Op" : operation.Kind.ToString(), node.ToFullString());
         //      //   }
         //      //}
         //   }
         //}

         //foreach (var kind in kinds) {
         //   Console.WriteLine(kind.Key + ": " + kind.Value);
         //}
      }

      private class OpDetails
      {
         public List<IOperation> ObjCreationOps { get; private set; }
         public List<IThrowOperation> ThrowOps { get; private set; }
         public List<IOperation> OtherOps { get; private set; }
         public int Count {get; private set;}
         public bool LastNodeFound {get; private set;}
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

      private class CompilationData {
         public INamedTypeSymbol SystemException { get; private set;}
         public Dictionary<INamedTypeSymbol, bool> TypeToIsException { get; set; }
         public CompilationData(INamedTypeSymbol systemException)
         {
            SystemException = systemException;
            TypeToIsException = new Dictionary<INamedTypeSymbol, bool>();
         }
      }

      private Dictionary<SyntaxTree, OpDetails> _fileToOpDetails = new Dictionary<SyntaxTree, OpDetails>();

      private class PerfDatum
      {
         public string Title { get; set; }
         public long Count { get; set; }
         public long Time { get; set; }
         public long Ops { get; set; }
         public int ConversionCount {get;set;}
         public int StoredCount { get; set; }
      }


      private readonly PerfDatum[] PerfData = new PerfDatum[] {
         new PerfDatum {Title = "Locking\\Search"},
         new PerfDatum{Title = "ObjCreations"},
         new PerfDatum{Title = "Violations"},
         new PerfDatum {Title = "Locking-Init"},
         new PerfDatum{Title = "IsException - Stored"},
         new PerfDatum{Title = "IsException - Convertion"},
      };



      //private void AnalyzeOperation(OperationAnalysisContext context)
      //{

      //   try {
      //      //Console.WriteLine(context.GetControlFlowGraph().OriginalOperation.Kind);
      //      System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
      //      System.Diagnostics.Stopwatch watchInit = new System.Diagnostics.Stopwatch();
      //      List<IOperation> objCreationOps = null;
      //      List<IThrowOperation> throwOps = null;
      //      watch.Start();
            
      //      lock (_lock) {
      //         OpDetails opDetails = null;
               
      //         if (!_fileToOpDetails.TryGetValue(context.Operation.Syntax.SyntaxTree, out opDetails) || !opDetails.LastNodeFound) {
                  
      //            if (null == opDetails) {
      //               //watchInit.Start();
                     
      //               int count = 
      //                  context.Operation.Syntax.SyntaxTree.GetRoot().DescendantNodes().Count(
      //                  n => n.IsKind(SyntaxKind.ThrowStatement) || 
      //                     n.IsKind(SyntaxKind.ThrowExpression) || n.IsKind(SyntaxKind.ObjectCreationExpression));

      //               //var nodes = context.Operation.Syntax.SyntaxTree.GetRoot().DescendantNodes().Where(
      //               //   n => n.IsKind(SyntaxKind.ThrowStatement) || n.IsKind(SyntaxKind.ThrowExpression) || n.IsKind(SyntaxKind.ObjectCreationExpression));

      //               opDetails = new OpDetails(count);
      //               _fileToOpDetails[context.Operation.Syntax.SyntaxTree] = opDetails;
      //               //watchInit.Stop();
      //               //lock (PerfData[3]) {
      //               //   PerfData[3].Count++;
      //               //   PerfData[3].Time += watchInit.ElapsedMilliseconds;
      //               //}

      //            }

      //            if (!opDetails.LastNodeFound) {
      //               opDetails.AddOperation(context.Operation);
      //            }

      //            if (opDetails.LastNodeFound) {
      //               objCreationOps = opDetails.ObjCreationOps;
      //               throwOps = opDetails.ThrowOps;
      //            }
      //         }
      //      }
      //      watch.Stop();

      //      lock (PerfData[0]) {
      //         PerfData[0].Count++;
      //         PerfData[0].Time += watch.ElapsedMilliseconds;
      //      }

      //      if (null != objCreationOps && objCreationOps.Any()) {

      //         var systemException = context.Compilation.GetTypeByMetadataName("System.Exception");
      //         if (null == systemException) {
      //            Log.WarnFormat(
      //               "Could not get type for System.Exception while analyzing file {0}. Violations will not be processed.", 
      //               context.Operation.Syntax.SyntaxTree.FilePath);
      //         } else {
      //            Context ctx = new Context(context.Compilation, context.Operation.SemanticModel, systemException, null);

      //            //watch.Start();
      //            foreach (var objCreationOp in objCreationOps) {
      //               ProcessObjectCreationOps(objCreationOp, ctx);
      //            }
      //            //watch.Stop();
      //            //lock (PerfData[1]) {
      //            //   PerfData[1].Count += objCreationOps.Count;
      //            //   PerfData[1].Time += watch.ElapsedMilliseconds;
      //            //}


      //            if (ctx.ExceptionVars.Any()) {
      //               //foreach (var throwOp in throwOps) {
      //               //   ProcessObjectCreationOps(throwOp, ctx);
      //               //}

      //               ctx.Throws = throwOps;

      //               //watch.Start();
      //               ProcessViolations(ctx);
      //               //watch.Stop();
      //               //lock (PerfData[2]) {
      //               //   PerfData[2].Count += ctx.Throws.Count;
      //               //   PerfData[2].Time += watch.ElapsedMilliseconds;
      //               //}

      //            }

      //            //foreach (var item in ctx.MethodToThrownLocalReference) {
      //            //   Console.WriteLine("Method: {0} Local References: {1}", item.Key, item.Value.Count);
      //            //}

      //            //Console.WriteLine("{0},{1},{2},{3},{4}",
      //            //   objCreationOps.Count, objCreationOps.Count(o => o.Kind == OperationKind.ObjectCreation),
      //            //   objCreationOps.Count(o => o.Kind == OperationKind.Throw), ctx.ExceptionVars.Count,
      //            //   context.Operation.Syntax.SyntaxTree.FilePath);
      //         }

      //         lock (_lock) {
      //            objCreationOps.Clear();
      //            throwOps.Clear();
      //         }
      //      }
      //   } catch (Exception e) {
      //      Log.Warn("Exception while analyzing " +  context.Operation.Syntax.SyntaxTree.FilePath, e);
      //   }

      //   //lock (_lock) {
      //   //   bool analysisInProgress = false;
      //   //   if (!_analyzedFiles.TryGetValue(context.Operation.Syntax.SyntaxTree.FilePath, out analysisInProgress)) {
      //   //      List<IOperation> ops = null;
      //   //      if (!_fileToOps.TryGetValue(context.Operation.Syntax.SyntaxTree.FilePath, out ops)) {
      //   //         ops = new List<IOperation>();
      //   //         _fileToOps[context.Operation.Syntax.SyntaxTree.FilePath] = ops;
      //   //      }

      //   //      ops.Add(context.Operation);
      //   //   } else {
      //   //      Log.WarnFormat("Semantic Analysis for {0} {1}", context.Operation.Syntax.SyntaxTree.FilePath, analysisInProgress ? "In Progress" : "already Finished");
      //   //   }
      //   //}
      //}

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
                                    ProcessExceptionVariable(ctx, objCreationOperation, iField);
                                 }

                                 break;
                              }
                           case OperationKind.VariableInitializer: {
                                 if (null != objCreationOperation.Parent.Parent && OperationKind.VariableDeclarator == objCreationOperation.Parent.Parent.Kind) {
                                    if (null != objCreationOperation.Parent.Parent.Parent && OperationKind.VariableDeclaration == objCreationOperation.Parent.Parent.Parent.Kind) {
                                       var iVariableDeclaration = objCreationOperation.Parent.Parent.Parent as IVariableDeclarationOperation;
                                       foreach (var iVar in iVariableDeclaration.Declarators) {
                                          ProcessExceptionVariable(ctx, objCreationOperation, iVar.Symbol);
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
                                    ProcessExceptionVariable(ctx, objCreationOperation, iSymbol);
                                 }
                              }

                              break;
                           //default: {
                           //      if (null != objCreationOperation.Parent && (OperationKind.Conversion == objCreationOperation.Parent.Kind &&
                           //         null != objCreationOperation.Parent.Parent && OperationKind.SimpleAssignment == objCreationOperation.Parent.Parent.Kind ||
                           //         OperationKind.SimpleAssignment == objCreationOperation.Parent.Kind)) {
                           //         var iSimpleAssignment = OperationKind.SimpleAssignment == objCreationOperation.Parent.Kind ?
                           //         objCreationOperation.Parent as ISimpleAssignmentOperation : objCreationOperation.Parent.Parent as ISimpleAssignmentOperation;
                           //         if (null != iSimpleAssignment.Target) {
                           //            ISymbol iSymbol = null;
                           //            switch (iSimpleAssignment.Target.Kind) {
                           //               case OperationKind.LocalReference:
                           //                  iSymbol = (iSimpleAssignment.Target as ILocalReferenceOperation).Local;
                           //                  break;
                           //               case OperationKind.FieldReference:
                           //                  iSymbol = (iSimpleAssignment.Target as IFieldReferenceOperation).Field;
                           //                  break;
                           //            }
                           //            if (null != iSymbol) {
                           //               ProcessExceptionVariable(ctx, objCreationOperation, iSymbol);
                           //            }
                           //         }
                           //      } /*else {
                           //         Log.Debug("Unhandled condition: operation.Parent.Kind: " + objCreationOperation.Parent.Kind);
                           //      }*/
                           //      break;
                           //}
                        }
                     }
                  }
               }
            } else {
               ctx.ExcludedThrows.Add(throwOp);
            }
         }
      }

      private void ProcessExceptionVar(Context ctx, IOperation operation, ISymbol exceptionVar, ref bool ret) {
         IThrowOperation throwOp = null;
         foreach (var childOp in operation.Children) {
            if (childOp is IThrowOperation) {
               throwOp = childOp as IThrowOperation;
               IOperation childThrow = null;
               if (throwOp.Children.Any()) {
                  childThrow = throwOp.Children.ElementAt(0);
                  if (childThrow is IConversionOperation) {
                     if (childThrow.Children.Any()) {
                        childThrow = childThrow.Children.ElementAt(0);
                     }
                  }
               }

               bool isObjectCreation = childThrow is IObjectCreationOperation;
               bool exclude = false;
               if (isObjectCreation) {
                  exclude = true;
               } else if (childThrow is ILocalReferenceOperation) {
                  exclude = true;
                  if (childThrow is ILocalReferenceOperation) {
                     if (exceptionVar == (childThrow as ILocalReferenceOperation).Local) {
                        ret = true;
                     }
                  }
                  ctx.MethodToThrownLocalReference[exceptionVar.ContainingSymbol].Add((childThrow as ILocalReferenceOperation).Local);
               }
               if (exclude) {
                  ctx.ExcludedThrows.Add(throwOp);
               }
            }
            ProcessExceptionVar(ctx, childOp, exceptionVar, ref ret);
         }
      }

      private void ProcessExceptionVariable(Context ctx, IOperation operation, ISymbol exceptionVar)
      {
         ctx.ExceptionVars.Add(exceptionVar);

         //bool isThrown = false;
         //if (exceptionVar is ILocalSymbol && exceptionVar.ContainingSymbol is IMethodSymbol) {
         //   IOperation methodDeclOp = operation.Parent;
         //   while (null != methodDeclOp && OperationKind.MethodBody != methodDeclOp.Kind) {
         //      methodDeclOp = methodDeclOp.Parent;
         //   }
         //   if (null != methodDeclOp) {
         //      HashSet<ILocalSymbol> locals = new HashSet<ILocalSymbol>();
         //      if (!ctx.MethodToThrownLocalReference.TryGetValue(exceptionVar.ContainingSymbol, out locals)) {
         //         ctx.MethodToThrownLocalReference[exceptionVar.ContainingSymbol] = new HashSet<ILocalSymbol>();
         //         ProcessExceptionVar(ctx, methodDeclOp, exceptionVar, ref isThrown);
         //      } else {
         //         isThrown = locals.Contains(exceptionVar);
         //      }
         //   } else {
         //      Console.WriteLine("No Method Op");
         //   }
         //}

         //if (!isThrown) {
         //   ctx.ExceptionVars.Add(exceptionVar);
         //}
      }


      private class Context
      {
         public static HashSet<INamedTypeSymbol> AllExceptionTypes = null;
         public HashSet<IOperation> ExcludedThrows = new HashSet<IOperation>();
         public List<IThrowOperation> Throws = new List<IThrowOperation>();
         public HashSet<ISymbol> ExceptionVars = new HashSet<ISymbol>();
         public static ConcurrentDictionary<INamedTypeSymbol, bool> TypeToIsException = new ConcurrentDictionary<INamedTypeSymbol, bool>();
         public Dictionary<ISymbol, HashSet<FileLinePositionSpan>> Symbol2ViolatingNodes = new Dictionary<ISymbol, HashSet<FileLinePositionSpan>>();
         public Dictionary<ISymbol, HashSet<ILocalSymbol>> MethodToThrownLocalReference = new Dictionary<ISymbol, HashSet<ILocalSymbol>>();
         public static INamedTypeSymbol SystemException = null;
         public static INamedTypeSymbol Interface_Exception = null;
         public Compilation Compilation;
         public SemanticModel SemanticModel;
         public HashSet<string> SymbolsNotFound = new HashSet<string>();
         public static long ConversionCount = 0;
         public static long StoredCount = 0;
         public Dictionary<string, INamedTypeSymbol> NameToSymbol = null;

         public Context(Compilation compilation, SemanticModel semanticModel, INamedTypeSymbol systemException)
         {
            //SystemException = systemException;
            Compilation = compilation;
            SemanticModel = semanticModel;
         }
      }

      private static HashSet<string> _symbolsNotFound = new HashSet<string>();
      private static IOperation ProcessObjectCreation(SyntaxNode node, Context ctx, ref int times)
      {
         IOperation operation = null;
         if (node.IsKind(SyntaxKind.ObjectCreationExpression)) {
            var kind = node.Parent.Kind();
            if (SyntaxKind.ThrowStatement != kind && SyntaxKind.ThrowExpression != kind) {
               if (SyntaxKind.ExpressionStatement == kind) {
                  var iSymbol = ctx.SemanticModel.GetEnclosingSymbol(node.SpanStart);
                  if (null != iSymbol) {
                     HashSet<FileLinePositionSpan> locations = null;
                     if (!ctx.Symbol2ViolatingNodes.TryGetValue(iSymbol, out locations)) {
                        locations = new HashSet<FileLinePositionSpan>();
                        ctx.Symbol2ViolatingNodes[iSymbol] = locations;
                     }
                     locations.Add(node.GetLocation().GetMappedLineSpan());
                  }
               } else if (node.ChildNodes().Any()) {
                  var firstChild = node.ChildNodes().First();
                  var childKind = firstChild.Kind();
                  if (SyntaxKind.IdentifierName == childKind || SyntaxKind.QualifiedName == childKind) {
                     var identifierName = firstChild.ToString();
                     INamedTypeSymbol iSymbol;
                     if (!ctx.NameToSymbol.TryGetValue(identifierName, out iSymbol)) {
                        iSymbol = ctx.SemanticModel.GetSymbolInfo(firstChild as NameSyntax).Symbol as INamedTypeSymbol;
                        times++;
                        if (null != iSymbol) {
                           ctx.NameToSymbol[identifierName] = iSymbol;
                        } else {
                           _symbolsNotFound.Add(identifierName);
                        }
                     }
                     
                     if (null != iSymbol && iSymbol.AllInterfaces.Contains(Context.Interface_Exception)) {
                        operation = null;
                     }


                     //if (!ctx.SymbolsNotFound.Contains(identifierName)) {
                     //   INamedTypeSymbol iSymbol = null;
                     //   if (!ctx.NameToSymbol.TryGetValue(identifierName, out iSymbol)) {
                     //      iSymbol = ctx.SemanticModel.GetSymbolInfo(firstChild).Symbol as INamedTypeSymbol;
                     //      times++;
                     //      if (null != iSymbol) {
                     //         ctx.NameToSymbol[identifierName] = iSymbol;
                     //      }
                     //   }

                     //   if (null != iSymbol) {
                     //      bool isException = iSymbol.AllInterfaces.Contains(ctx.Interface_Exception);
                     //      if (isException) {
                     //         operation = null;
                     //      } else if (null == iSymbol) {
                     //         ctx.SymbolsNotFound.Add(identifierName);
                     //      }
                     //   }
                     //}
                  }
               }
            }
         }
         return operation;
      }

      //private IOperation GetOperation(SyntaxNode node, SemanticModel semanticModel, Context ctx) {
      //   IOperation iOperation = null;

      //   bool getOp = false;
      //   if (node.IsKind(SyntaxKind.ObjectCreationExpression)) {
      //      var kind = node.Parent.Kind();
      //      if (SyntaxKind.ThrowStatement != kind && SyntaxKind.ThrowExpression != kind) {
      //         if (node.ChildNodes().Any()) {
      //            if (SyntaxKind.IdentifierName == node.ChildNodes().ElementAt(0).Kind()) {
      //               var iSymbol = semanticModel.GetSymbolInfo(node.ChildNodes().ElementAt(0)).Symbol;
      //               if (null != iSymbol && IsException(iSymbol as INamedTypeSymbol, semanticModel.Compilation, ctx.SystemException, ref ctx.TypeToIsException)) {
      //                  getOp = true;
      //               }
      //            }
      //         }
      //      } 
      //   } else {
      //      var kind = node.Kind();
      //      if (SyntaxKind.ThrowStatement == kind ||  SyntaxKind.ThrowExpression == kind) {
      //         if (!node.ChildNodes().FirstOrDefault().IsKind(SyntaxKind.ObjectCreationExpression)) {
      //            getOp = true;
      //         }
      //      }
      //   }

      //   if (getOp) {
      //      iOperation = semanticModel.GetOperation(node);
      //   }

      //   return iOperation;
      //}


      //public class GetAllSymbolsVisitor : SymbolVisitor
      //{
      //   public Dictionary<string, INamedTypeSymbol> AllTypes {get; set;}
      //   public GetAllSymbolsVisitor()
      //   {
      //      AllTypes = new Dictionary<string, INamedTypeSymbol>();
      //   }
      //   public override void VisitNamespace(INamespaceSymbol symbol)
      //   {
      //      foreach (var member in symbol.GetMembers()) {
      //         member.Accept(this);
      //      }
      //   }

      //   public override void VisitNamedType(INamedTypeSymbol symbol)
      //   {
      //      AllTypes[symbol.Name] = symbol;
      //      AllTypes[symbol.OriginalDefinition.ToString()] = symbol;
      //      foreach (var member in symbol.GetMembers()) {
      //         member.Accept(this);
      //      }
      //   }
      //}

      //private Dictionary<Compilation, Dictionary<string, INamedTypeSymbol>> _allTypes =
      //   new Dictionary<Compilation, Dictionary<string, INamedTypeSymbol>>();

      //private Dictionary<IAssemblySymbol, Dictionary<string, INamedTypeSymbol>> _allAssemblyTypes =
      //   new Dictionary<IAssemblySymbol, Dictionary<string, INamedTypeSymbol>>();


      //private void AnalyzeUsingSemanticModel(SemanticModelAnalysisContext context)
      //{
      //   try {
      //      Dictionary<string, INamedTypeSymbol> allTypes = null;
      //      bool alreadyProcessed = false;
      //      lock (_lock) {
      //         alreadyProcessed = _analyzedFiles.Keys.Contains(context.SemanticModel.SyntaxTree.FilePath);
      //         if (!alreadyProcessed) {
      //            _analyzedFiles[context.SemanticModel.SyntaxTree.FilePath] = true;
      //         }
               
      //         if (!_allTypes.TryGetValue(context.SemanticModel.Compilation, out allTypes)) {
      //            var watch = new System.Diagnostics.Stopwatch();
      //            watch.Start();
      //            GetAllSymbolsVisitor visitor = new GetAllSymbolsVisitor();
      //            visitor.Visit(context.SemanticModel.Compilation.Assembly.GlobalNamespace);
      //            _allTypes[context.SemanticModel.Compilation] = allTypes = visitor.AllTypes;

      //            //foreach (var reference in context.SemanticModel.Compilation.References) {
      //            //   var referencedAssembly = 
      //            //      context.SemanticModel.Compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
      //            //   if (null != referencedAssembly) {
      //            //      visitor.AllTypes.Clear();
      //            //      if (!_allAssemblyTypes.Keys.Contains(referencedAssembly)) {
      //            //         Console.WriteLine(referencedAssembly.ToString() + " Not found!");
      //            //         visitor.Visit(referencedAssembly.GlobalNamespace);
      //            //         _allAssemblyTypes[referencedAssembly] = visitor.AllTypes;
      //            //      } else {
      //            //         Console.WriteLine(referencedAssembly.ToString() + " found!");
      //            //      }
      //            //   }
      //            //}
                  
      //            watch.Stop();
      //            Console.WriteLine("Number Of Types: " + visitor.AllTypes.Count + " Time: " + watch.ElapsedMilliseconds);
      //         }
      //      }

      //      if (!alreadyProcessed) {
      //         var interface_exception = context.SemanticModel.Compilation.GetTypeByMetadataName("System.Runtime.InteropServices._Exception");
      //         if (null == interface_exception) {
      //            Log.WarnFormat(
      //               "Could not get type for System.Exception while analyzing file {0}. Violations will not be processed.",
      //               context.SemanticModel.SyntaxTree.FilePath);
      //         } else {

      //            Context ctx = new Context(context.SemanticModel.Compilation, context.SemanticModel, interface_exception, allTypes);
      //            var watch = new System.Diagnostics.Stopwatch();
      //            watch.Start();

      //            var nodes = context.SemanticModel.SyntaxTree.GetRoot().DescendantNodes().
      //               Where(n => n.IsKind(SyntaxKind.ObjectCreationExpression));
      //            int ops = 0;
      //            foreach (var node in nodes) {
      //               ProcessObjectCreation(node, ctx, ref ops);
      //            }
      //            watch.Stop();
      //            lock (PerfData[1]) {
      //               PerfData[1].Count++;
      //               PerfData[1].Time += watch.ElapsedMilliseconds;
      //               PerfData[1].Ops += ops;
      //               PerfData[1].ConversionCount += ctx.ConversionCount;
      //               PerfData[1].StoredCount += ctx.StoredCount;
      //            }

      //            //ProcessViolations(ctx);
      //         }

      //         lock (_lock) {
      //            _analyzedFiles[context.SemanticModel.SyntaxTree.FilePath] = false;
      //         }
      //      }
      //   } catch (Exception e) {
      //      Log.Warn("Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath, e);
      //   }
      //}

      //int i = 0;
      //public override void Reset()
      //{
      //   if (2 == i) {
      //      i = 2;
      //   }
      //   Console.WriteLine("Reset");
      //   base.Reset();
      //}

   }
}
