using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using log4net;
using Roslyn.DotNet.CastDotNetExtension;



namespace CastDotNetExtension
{

   public class OperationDetails
   {
      public IOperation Operation { get; private set; }
      public ControlFlowGraph ControlFlowGraph { get; private set; }
      public ISymbol ContainingSymbol { get; private set; }
      public IOperation OriginalOperation { get; private set; }
      public OperationDetails(IOperation operation, ISymbol containingSymbol, ControlFlowGraph controlFlowGraph)
      {
         Operation = operation;
         ContainingSymbol = containingSymbol;
         ControlFlowGraph = controlFlowGraph;
         OriginalOperation = null != ControlFlowGraph ? ControlFlowGraph.OriginalOperation : null;
      }

      public override string ToString()
      {
         return string.Format("Operation Kind: {0} Syntax Kind: {1} Containing Symbol: {2}",
            Operation.Kind, Operation.Syntax.Kind(), ContainingSymbol.Name);
      }
   }

   public class OPList : List<OperationDetails>
   {
      public List<IOperation> Operations { get; private set; }
      public OPList()
         : base()
      {
         Operations = new List<IOperation>();
      }
      public OPList(int capacity)
         : base(capacity)
      {
         Operations = new List<IOperation>(capacity);
      }
      public new void Add(OperationDetails context)
      {
         Operations.Add(context.Operation);
         base.Add(context);
      }
   }

   public class OPs : Dictionary<OperationKind, OPList>
   {

   }

   public interface IOpProcessor
   {
      SyntaxKind[] Kinds(CompilationStartAnalysisContext context);
      void HandleSemanticModelOps(SemanticModelAnalysisContext context,
         OPs ops);

      void HandleOperations(SemanticModelAnalysisContext context,
         ConcurrentQueue<OperationDetails> ops);

   }

   public abstract class OperationsRetriever : AbstractRuleChecker, IOpProcessor
   {
      public abstract SyntaxKind[] Kinds(CompilationStartAnalysisContext context);
      public abstract void HandleSemanticModelOps(SemanticModelAnalysisContext context,
         OPs ops);

      public abstract void HandleOperations(SemanticModelAnalysisContext context,
         ConcurrentQueue<OperationDetails> ops);

      private static ConcurrentDictionary<string, ConcurrentQueue<Tuple<int, int>>> FileVerificationData = new ConcurrentDictionary<string, ConcurrentQueue<Tuple<int, int>>>();

      protected static void AddFileVerificationData(string filePath, int objCreationCount, int throwCount) {
         var fileData = FileVerificationData.GetOrAdd(filePath, (key) => new ConcurrentQueue<Tuple<int, int>>());
         fileData.Enqueue(new Tuple<int, int>(objCreationCount, throwCount));
      }

      ~OperationsRetriever()
      {
         foreach (var fileVerificationData in FileVerificationData) {
            Console.WriteLine("Verifying for {0}", fileVerificationData.Key);
            var fileData = fileVerificationData.Value.ToArray();
            if ( (fileData.Length % 2) == 0) {
               for (int i = 0; i < fileData.Length; i += 2) {
                  if (!fileData.ElementAt(i).Equals(fileData.ElementAt(i + 1))) {
                     Console.WriteLine("Verification failed for {0}: {1} : {2}", fileVerificationData.Key, fileData.ElementAt(i), fileData.ElementAt(i + 1));
                  }
               }
            }
         }
      }


      private class OpsProcessor
      {
         public bool IsActive { get; set; }
         public IOpProcessor OpProcessor { get; private set; }
         public OpsProcessor(IOpProcessor processor, bool isActive = true)
         {
            OpProcessor = processor;
            IsActive = isActive;
         }
      }

      protected class PerfDatumBase
      {
         public string FilePath { get; private set; }
         public long Time { get; private set; }

         public PerfDatumBase(string filePath, long time)
         {
            FilePath = filePath;
            Time = time;
         }

         public static string Headers { get { return "Time,File"; } }
         public override string ToString()
         {
            return string.Format("{0},\"{1}\"",
               Time, FilePath);
         }
      }

      protected class PerfDatumOps : PerfDatumBase
      {
         public long Ops { get; private set; }

         public static new string Headers { get { return "Time,Ops,File"; } }
         public PerfDatumOps(string filePath, long time, long ops) :
            base(filePath, time)
         {
            Ops = ops;
         }

         public override string ToString()
         {
            return string.Format("{0},{1},\"{2}\"",
               Time, Ops, FilePath);
         }
      }

      protected class PerfDatum : PerfDatumBase
      {
         public long Count { get; private set; }
         public int ObjCreations { get; private set; }
         public int ThrowOps { get; private set; }
         public PerfDatum(long time, string filePath, int objCreations, int throwOps, int count = 1) :
            base(filePath, time)
         {
            Count = count;
            ObjCreations = objCreations;
            ThrowOps = throwOps;
         }

         public static new string Headers { get { return "Count,Time,ObjCreations,ThrowOps,File"; } }
         public override string ToString()
         {
            return string.Format("{0},{1},{2},{3},\"{4}\"",
               Count, Time, ObjCreations, ThrowOps, FilePath);
         }
      }

      public OperationsRetriever()
      {
         SubscriberSink.Instance.Log = Log;
         SubscriberSink.Instance.AddOpsProcessor(this);
      }

      


      private class SubscriberSink
      {
         //private Dictionary<string, long> _currentFilesToThreadIDs = new Dictionary<string, long>();

         //private List<Dictionary<string, long>> _currentFilesToThreadIDsToPrint = new List<Dictionary<string, long>>();

         //private List<PerfDatumOps> _perfDataOps = new List<PerfDatumOps>();
         //private List<PerfDatumOps> _perfDataAssembly = new List<PerfDatumOps>();


         private static SubscriberSink ObjSubscriberSink = new SubscriberSink();

         private HashSet<SyntaxKind> _kinds = new HashSet<SyntaxKind>();

         private ConcurrentDictionary<string, ConcurrentQueue<OperationDetails>> _operations =
            new ConcurrentDictionary<string, ConcurrentQueue<OperationDetails>>();
         private HashSet<OperationKind> _opKinds = new HashSet<OperationKind>();
         private Compilation CurrentCompilation { get; set; }

         public ILog Log;
         internal static object Lock = new object();


         public HashSet<OpsProcessor> OpsProcessors { get; private set; }
         private SubscriberSink()
         {
            OpsProcessors = new HashSet<OpsProcessor>();

            _opKindToSyntaxKind = new ConcurrentDictionary<OperationKind, ConcurrentQueue<SyntaxKind>>();
            foreach (var opKind in OperationsKinds) {
               _opKindToSyntaxKind[opKind] = new ConcurrentQueue<SyntaxKind>();
            }
         }

         private HashSet<SyntaxKind> syntaxKinds = new HashSet<SyntaxKind> {
               SyntaxKind.Attribute,
               SyntaxKind.SimpleMemberAccessExpression,
               SyntaxKind.IdentifierName,
               SyntaxKind.PredefinedType,
               SyntaxKind.None
         };

         //~SubscriberSink()
         //{

         //   foreach (var repeatFile in _repeatFiles) {
         //      if (1 < repeatFile.Value) {
         //         Console.WriteLine(repeatFile);
         //      }
         //   }
            
         //   //foreach (var filesDetails in _currentFilesToThreadIDsToPrint) {
         //   //   Console.WriteLine("===================Start: Multiple Files Snapshot=============");   
         //   //   foreach (var fileDetails in filesDetails) {
         //   //      Console.WriteLine("   " + fileDetails);
         //   //   }
         //   //   Console.WriteLine("===================End: Multiple Files Snapshot=============");   
         //   //}
         //}



         //~SubscriberSink()
         //{

         //   Console.WriteLine("============== Start: Op Kinds To Syntax Kinds ===========");
         //   foreach (var opKindDetails in _opKindToSyntaxKind) {
         //      Console.WriteLine(opKindDetails.Key);
         //      HashSet<SyntaxKind> syntaxKinds = opKindDetails.Value.ToHashSet<SyntaxKind>();
         //      if (syntaxKinds.Any()) {
         //         foreach (var syntaxKind in syntaxKinds) {
         //            Console.WriteLine("   {0}", syntaxKind);
         //         }
         //      } else {
         //         Console.WriteLine("   <None>");
         //      }
         //   }
         //   Console.WriteLine("============== End: Op Kinds To Syntax Kinds ===========");

         //   HashSet<OperationKind> notNullOps = _notNullControlFlowGraphOps.ToHashSet();

         //Console.WriteLine("============== Start: Not Null Control Flow Graph Ops ===========");
         //foreach (var opKind in notNullOps) {
         //   Console.WriteLine("    " + opKind);
         //}
         //Console.WriteLine("============== End: Not Null Control Flow Graph Ops ===========");

         //foreach (var nullOpKindDetails in _opsNullControlFlowGraph) {
         //   Console.WriteLine("Null Control Graph For OP: {0}",  nullOpKindDetails.Key);
         //   if (notNullOps.Contains(nullOpKindDetails.Key)) {
         //      Console.WriteLine("   Control Flow Graph for OperationKind {0} was not null later", nullOpKindDetails.Key);
         //   }

         //   foreach (var nullSyntaxKindDetails in nullOpKindDetails.Value) {
         //      Console.WriteLine(nullSyntaxKindDetails.ToString());
         //   }
         //}
         //}

         //~SubscriberSink()
         //{
         //   foreach (var ctx in _opContextToOpCount) {
         //      Console.WriteLine("Op Kind: {0} Symbol: {1} Count: {2}", ctx.Key.Operation.Kind, ctx.Key.ContainingSymbol, ctx.Value);
         //   }
         //}

         //~SubscriberSink()
         //{
         //   long time = 0;
         //   long ops = 0;
         //   Console.WriteLine("================SubscriberSink================");
         //   Console.WriteLine("================SubscriberSink: Compilation Start================");
         //   Console.WriteLine(PerfDatumOps.Headers);
         //   foreach (var perfDatum in _perfDataAssembly) {
         //      Console.WriteLine(perfDatum.ToString());
         //      time += perfDatum.Time;
         //      ops += perfDatum.Ops;
         //   }
         //   Console.WriteLine(new PerfDatumOps("<All-Compilation Start>", time / TimeSpan.TicksPerMillisecond, ops).ToString());

         //   Console.WriteLine("================SubscriberSink: Retrieval================");
         //   Console.WriteLine(PerfDatumOps.Headers);
         //   long retrievalTime = 0;
         //   long retrievalOps = 0;
         //   foreach (var perfDatum in _perfDataOps) {
         //      Console.WriteLine(perfDatum.ToString());
         //      time += perfDatum.Time;
         //      retrievalTime += perfDatum.Time;
         //      retrievalOps += perfDatum.Ops;
         //   }
         //   Console.WriteLine(new PerfDatumOps("<All-Retrieval>", retrievalTime / TimeSpan.TicksPerMillisecond, retrievalOps).ToString());

         //   Console.WriteLine(PerfDatumBase.Headers);
         //   Console.WriteLine(new PerfDatumBase("<All-Compilation Start and Retrieval> 1", time / TimeSpan.TicksPerMillisecond).ToString());
         //}

         public static SubscriberSink Instance { get { return ObjSubscriberSink; } }

         public void AddOpsProcessor(IOpProcessor iOpProcessor)
         {
            if (!OpsProcessors.Any(o => iOpProcessor == o.OpProcessor)) {
               OpsProcessors.Add(new OpsProcessor(iOpProcessor));
            }
         }

         public void RegisterCompilationStartAction(AnalysisContext context)
         {
            context.RegisterCompilationStartAction(CompiliationStart);
         }

         //private void CompiliationStart(CompilationStartAnalysisContext context)
         //{
         //   context.RegisterOperationAction(OnOperation, OperationsKinds);
         //}


         private void CompiliationStart(CompilationStartAnalysisContext context)
         {
            //var watch = new System.Diagnostics.Stopwatch();
            //watch.Start();
            lock (Lock) {
               try {
                  //Log.WarnFormat("Assembly: {0}", context.Compilation.Assembly.Name);
                  if (null == CurrentCompilation || CurrentCompilation.Assembly != context.Compilation.Assembly) {
                     CurrentCompilation = context.Compilation;
                     _symbolToControlFlowGraph.Clear();
                     //Log.WarnFormat("Compilation Changed, Assembly: {0}", CurrentCompilation.Assembly.Name);
                     SyntaxKind[] kinds = null;
                     foreach (var opProcessor in SubscriberSink.Instance.OpsProcessors) {
                        kinds = opProcessor.OpProcessor.Kinds(context);
                        if (!kinds.Any()) {
                           opProcessor.IsActive = false;
                        } else {
                           _kinds.UnionWith(kinds);
                        }
                     }

                     foreach (var kind in _kinds) {
                        switch (kind) {
                           case SyntaxKind.ThrowExpression:
                           case SyntaxKind.ThrowStatement:
                           case SyntaxKind.ThrowKeyword:
                              _opKinds.Add(OperationKind.Throw);
                              break;
                           case SyntaxKind.ObjectCreationExpression:
                              _opKinds.UnionWith(new OperationKind[] {                                                          
                           OperationKind.ObjectCreation, 
                           OperationKind.DynamicObjectCreation, 
                           OperationKind.Invalid, 
                           OperationKind.DelegateCreation, 
                           OperationKind.TypeParameterObjectCreation});
                              break;
                           case SyntaxKind.InvocationExpression:
                              _opKinds.UnionWith(new OperationKind[] {
                           OperationKind.Invocation, 
                           OperationKind.DynamicInvocation,
                           OperationKind.NameOf,
                           OperationKind.Invalid});
                              break;
                        }
                     }
                     if (_opKinds.Any()) {
                        _opKinds.Add(OperationKind.End);
                        _operations.Clear();
                        context.RegisterOperationAction(OnOperation, _opKinds.ToArray());
                        context.RegisterSemanticModelAction(OnSemanticModelAnalysisEnd);
                     }
                     //Log.Warn("End: CompilationStart");
                  }
               } catch (Exception e) {
                  Log.Warn("Exception while initializing Op Retriever for " + context.Compilation.Assembly.Name, e);
               }
            }
            //watch.Stop();
            //lock (_perfDataAssembly)
            //{
            //   var assemblyPerf = _perfDataAssembly.FirstOrDefault(p => p.FilePath == CurrentCompilation.Assembly.Name);
            //   if (null != assemblyPerf)
            //   {
            //      _perfDataAssembly.Remove(assemblyPerf);
            //      assemblyPerf = new PerfDatumOps(CurrentCompilation.Assembly.Name, assemblyPerf.Time + watch.ElapsedTicks, assemblyPerf.Ops + 1);
            //   }
            //   else
            //   {
            //      assemblyPerf = new PerfDatumOps(CurrentCompilation.Assembly.Name, watch.ElapsedTicks, 1);
            //   }
            //   _perfDataAssembly.Add(assemblyPerf);
            //}
         }

         private class OpDetails
         {
            public OperationKind OpKind { get; private set; }
            public SyntaxKind SyntaxKind { get; private set; }
            public string Code { get; private set; }
            public string Symbol { get; private set; }
            public string FilePath { get; private set; }
            public int Line { get; private set; }
            public ControlFlowGraph ControlFlowGraph { get; private set; }
            public OpDetails(OperationKind opKind, SyntaxKind syntaxKind, string code, string symbol, string filepath, int line, ControlFlowGraph controlFlowGraph = null)
            {
               OpKind = opKind;
               SyntaxKind = syntaxKind;
               Code = code;
               Symbol = symbol;
               FilePath = filepath;
               Line = line;
               ControlFlowGraph = controlFlowGraph;
            }
            public override string ToString()
            {
               return string.Format("Operation: {0} Syntax: Kind: {1} Code: {2} for {3} Line: {4} file {5}",
                     OpKind,
                     SyntaxKind,
                     Code,
                     Symbol,
                     Line,
                     FilePath);
            }
         }

         private ConcurrentDictionary<OperationKind, ConcurrentQueue<SyntaxKind>> _opKindToSyntaxKind;
         private ConcurrentDictionary<OperationKind, ConcurrentDictionary<SyntaxKind, ConcurrentQueue<OpDetails>>> _opsNullControlFlowGraph =
            new ConcurrentDictionary<OperationKind, ConcurrentDictionary<SyntaxKind, ConcurrentQueue<OpDetails>>>();
         private ConcurrentQueue<OperationKind> _notNullControlFlowGraphOps = new ConcurrentQueue<OperationKind>();

         private ConcurrentDictionary<ISymbol, OpDetails> _symbolToControlFlowGraph =
            new ConcurrentDictionary<ISymbol, OpDetails>();

         private void OnOperation(OperationAnalysisContext context)
         {
            //try {
            //OpDetails opDetails = null;

            //if (OperationKind.Invalid != context.Operation.Kind) {
            //   if (!_symbolToControlFlowGraph.TryGetValue(context.ContainingSymbol, out opDetails)) {
            //      ControlFlowGraph controlFlowGraph = context.GetControlFlowGraph();
            //      if (null == controlFlowGraph) {
            //         Log.WarnFormat("Control Flow Graph is null Operation: {0} Syntax: Kind: {1} Code: {2} for {3} file {4}",
            //            context.Operation.Kind,
            //            context.Operation.Syntax.Kind(),
            //            context.Operation.Syntax,
            //            context.ContainingSymbol.OriginalDefinition.ToString(),
            //            context.Operation.SemanticModel.SyntaxTree.FilePath);

            //         var dic = _opsNullControlFlowGraph.GetOrAdd(context.Operation.Kind, (key) => new ConcurrentDictionary<SyntaxKind, ConcurrentQueue<OpDetails>>());
            //         var nq = dic.GetOrAdd(context.Operation.Syntax.Kind(), (key) => new ConcurrentQueue<OpDetails>());
            //         nq.Enqueue(new OpDetails(context.Operation.Kind,
            //            context.Operation.Syntax.Kind(),
            //            context.Operation.Syntax.ToString(),
            //            context.ContainingSymbol.OriginalDefinition.ToString(),
            //            context.Operation.SemanticModel.SyntaxTree.FilePath,
            //            context.Operation.Syntax.GetLocation().GetMappedLineSpan().StartLinePosition.Line));
            //      } else {
            //         _symbolToControlFlowGraph[context.ContainingSymbol] = new OpDetails(context.Operation.Kind,
            //            context.Operation.Syntax.Kind(),
            //            context.Operation.Syntax.ToString(),
            //            context.ContainingSymbol.OriginalDefinition.ToString(),
            //            context.Operation.SemanticModel.SyntaxTree.FilePath,
            //            context.Operation.Syntax.GetLocation().GetMappedLineSpan().StartLinePosition.Line,
            //            controlFlowGraph);

            //         _notNullControlFlowGraphOps.Enqueue(context.Operation.Kind);
            //      }
            //   } else {
            //      var controlFlowGraph = context.GetControlFlowGraph();
            //      if (opDetails.ControlFlowGraph != controlFlowGraph) {

            //         Log.WarnFormat("Control Flow Graph is different Operation: {0} Syntax: Kind: {1} Code: {2} for {3} Line: {4} file {5}",
            //            context.Operation.Kind,
            //            context.Operation.Syntax.Kind(),
            //            context.Operation.Syntax,
            //            context.ContainingSymbol.OriginalDefinition.ToString(),
            //            context.Operation.Syntax.GetLocation().GetMappedLineSpan().StartLinePosition.Line,
            //            context.Operation.SemanticModel.SyntaxTree.FilePath);
            //         Log.WarnFormat("Previous: " + opDetails.ToString());
            //      }
            //   }
            //}

            //   var kq = _opKindToSyntaxKind.GetOrAdd(context.Operation.Kind, (key) => new ConcurrentQueue<SyntaxKind>());
            //   kq.Enqueue(context.Operation.Syntax.Kind());
            //} catch (Exception e) {
            //   Log.Warn(string.Format("Exception while retrieving Operation: {0} Syntax: Kind: {1} Code: {2} for {3} file {4}",
            //            context.Operation.Kind,
            //            context.Operation.Syntax.Kind(),
            //            context.Operation.Syntax,
            //            context.ContainingSymbol.OriginalDefinition.ToString(),
            //            context.Operation.SemanticModel.SyntaxTree.FilePath), e);
            //}

            var q = _operations.GetOrAdd(context.Operation.Syntax.SyntaxTree.FilePath, (key) => new ConcurrentQueue<OperationDetails>());
            q.Enqueue(new OperationDetails(context.Operation, context.ContainingSymbol,
               OperationKind.MethodBody == context.Operation.Kind ? context.GetControlFlowGraph() : null));
         }

         //private Dictionary<string, int> _repeatFiles = new Dictionary<string, int>();
         private void OnSemanticModelAnalysisEnd(SemanticModelAnalysisContext context)
         {
            //lock (_currentFilesToThreadIDs) {
            //   _currentFilesToThreadIDs.Add(context.SemanticModel.SyntaxTree.FilePath, Thread.CurrentThread.ManagedThreadId);
            //   if (1 < _currentFilesToThreadIDs.Count) {
            //      _currentFilesToThreadIDsToPrint.Add(new Dictionary<string, long>(_currentFilesToThreadIDs));
            //   }
            //}

            //lock (_repeatFiles) {
            //   if (!_repeatFiles.ContainsKey(context.SemanticModel.SyntaxTree.FilePath)) {
            //      _repeatFiles[context.SemanticModel.SyntaxTree.FilePath] = 0;
            //   }
            //   _repeatFiles[context.SemanticModel.SyntaxTree.FilePath]++;
            //}

            try {
               //var watch = new System.Diagnostics.Stopwatch();
               //watch.Start();
               //Console.WriteLine("Starting Semantic Model Analysis For: {0}", context.SemanticModel.SyntaxTree.FilePath);
               var nodes =
                     context.SemanticModel.SyntaxTree.GetRoot().DescendantNodes().Where(n => _kinds.Contains(n.Kind())).ToHashSet();
               //int nodeCount = nodes.Count;

               if (nodes.Any()) {
                  ConcurrentQueue<OperationDetails> operations = null;
                  while (!_operations.TryGetValue(context.SemanticModel.SyntaxTree.FilePath, out operations)) ;
                  List<Task> handlerTasks = new List<Task>();
                  foreach (var opsProcessor in OpsProcessors) {
                     if (opsProcessor.IsActive) {
                        //Console.WriteLine("   Processing {0}", ((AbstractRuleChecker)opsProcessor.OpProcessor).GetRuleName());
                        handlerTasks.Add(Task.Run(() =>
                        opsProcessor.OpProcessor.HandleOperations(context, operations)
                           ))
                        ;
                     }
                  }

                  Task.WaitAll(handlerTasks.ToArray());

                  

                  





                  /*
                                    List<Action<SemanticModelAnalysisContext, ConcurrentQueue<OperationDetails>>> callbacks = new List<Action<SemanticModelAnalysisContext, ConcurrentQueue<OperationDetails>>>();
                                    foreach (var opsProcessor in OpsProcessors) {
                                       callbacks.Add(opsProcessor.OpProcessor.HandleOperations);
                                    }
                                    //Console.WriteLine("Have nodes, count: {0}", nodes.Count);
                                    OPs ops = new OPs();
                                    foreach (var opKind in _opKinds) {
                                       ops[opKind] = new OPList(25);
                                    }
                                    int retryCount = 0;


                                    OperationDetails operation;

                                    //int nodeCount = nodes.Count;
                                    //int nodeCountReceived = 0;
                                    //List<SyntaxNode> nodesReceived = new List<SyntaxNode>(nodeCount);

                                    //var watch = new System.Diagnostics.Stopwatch();
                                    //watch.Start();

                  

                                    while (_operations.TryGetValue(context.SemanticModel.SyntaxTree.FilePath, out operations)) {
                                       while (operations.TryDequeue(out operation)) {
                                          //if (OperationKind.End == operation.Kind) {
                                          //   break;
                                          //}

                                          //nodesReceived.Add(operation.Syntax);
                                          //nodeCountReceived++;
                                          //if (nodeCount == nodeCountReceived) {
                                          //   nodes.ExceptWith(nodesReceived);
                                          //   nodesReceived.Clear();
                                          //   nodeCount = nodes.Count;
                                          //   nodeCountReceived = 0;
                                          //}
                                          ops[operation.Operation.Kind].Add(operation);
                                          //foreach (var callback in callbacks) {
                                          //   callback.BeginInvoke(context, operation, callback.EndInvoke, null);
                                          //}

                                          //var op = new OperationDetails(operation.Operation, operation.ContainingSymbol, operation.ControlFlowGraph);
                                          //foreach (var opsProcessor in OpsProcessors) {
                                          //   if (opsProcessor.IsActive) {
                                          //      //Console.WriteLine("   Processing {0}", ((AbstractRuleChecker)opsProcessor.OpProcessor).GetRuleName());
                                          //      handlerTasks.Add(Task.Run(() =>
                                          //      opsProcessor.OpProcessor.HandleOperation(context, op)
                                          //         ))
                                          //      ;
                                          //   }
                                          //}

                                          nodes.Remove(operation.Operation.Syntax);
                                          if (!nodes.Any()) {
                                             break;
                                          }
                                       }
                                       if (null == operation) {
                                          retryCount++;
                                          //Log.Warn("operation was null; trying again! File: " + context.SemanticModel.SyntaxTree.FilePath);
                                          continue;
                                       }
                                       if (!nodes.Any()) {
                                          break;
                                       }
                                    }

                                    if (0 < retryCount) {
                                       Log.WarnFormat("File: {0} Retry Count: {1}", context.SemanticModel.SyntaxTree.FilePath, retryCount);
                                    }

                                    if (2 < OpsProcessors.Count) {
                                       throw new InvalidOperationException("OpsProcessors, count: " + OpsProcessors.Count);
                                    }


                                    //Task.WaitAll(handlerTasks.ToArray());
                                    //handlerTasks.Clear();
                                    //Console.WriteLine("Processing Ops For {0} OP Processor Count: {1}" ,
                                    //   context.SemanticModel.SyntaxTree.FilePath, OpsProcessors.Count);
                                    //foreach (var opsProcessor in OpsProcessors) {
                                    //   if (opsProcessor.IsActive) {
                                    //      //Console.WriteLine("   Processing {0}", ((AbstractRuleChecker)opsProcessor.OpProcessor).GetRuleName());
                                    //      handlerTasks.Add(Task.Run(() =>
                                    //      opsProcessor.OpProcessor.HandleOperation(context, null)
                                    //         ))
                                    //      ;
                                    //   }
                                    //}

                                    foreach (var opsProcessor in OpsProcessors) {
                                       if (opsProcessor.IsActive) {
                                          //Console.WriteLine("   Processing {0}", ((AbstractRuleChecker)opsProcessor.OpProcessor).GetRuleName());
                                          handlerTasks.Add(Task.Run(() =>
                                          opsProcessor.OpProcessor.HandleSemanticModelOps(context, ops)
                                             ))
                                          ;
                                       }
                                    }

                                    Task.WaitAll(handlerTasks.ToArray());
                                    //Log.WarnFormat("Operation Null Count: {0} File {1}", operationNullCount, context.SemanticModel.SyntaxTree.FilePath);
                                 }
                                 //watch.Stop();


                                 //lock (_perfDataOps) {
                                 //   _perfDataOps.Add(new PerfDatumOps(context.SemanticModel.SyntaxTree.FilePath, watch.ElapsedTicks, nodeCount));
                                 //}
                   */
               }
            } catch (Exception e) {
               Log.Warn("Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath, e);
            }

            //lock (_currentFilesToThreadIDs) {
            //   _currentFilesToThreadIDs.Remove(context.SemanticModel.SyntaxTree.FilePath);
            //}

         }
      }

      public override void Init(AnalysisContext context)
      {
         try {
            SubscriberSink.Instance.RegisterCompilationStartAction(context);
         } catch (Exception e) {
            Log.Warn("Exception while Initing", e);
         }
      }

      private static readonly OperationKind[] OperationsKinds = {
                        OperationKind.None,
                        OperationKind.Invalid,
                        OperationKind.Block,
                        OperationKind.VariableDeclarationGroup,
                        OperationKind.Switch,
                              OperationKind.Loop,
                        OperationKind.Labeled,
                        OperationKind.Branch,
                        OperationKind.Empty,
                        OperationKind.Return,
                              OperationKind.YieldBreak,
                        OperationKind.Lock,
                        OperationKind.Try,
                        OperationKind.Using,
                              OperationKind.YieldReturn,
                        OperationKind.ExpressionStatement,
                        OperationKind.LocalFunction,
                        OperationKind.Stop,
                        OperationKind.End,
                        OperationKind.RaiseEvent,
                        OperationKind.Literal,
                        OperationKind.Conversion,
                        OperationKind.Invocation,
                        OperationKind.ArrayElementReference,
                        OperationKind.LocalReference,
                        OperationKind.ParameterReference,
                        OperationKind.FieldReference,
                        OperationKind.MethodReference,
                        OperationKind.PropertyReference,
                        OperationKind.EventReference,
                        OperationKind.Unary,
      OperationKind.UnaryOperator,
      OperationKind.BinaryOperator,
                        OperationKind.Binary,
                        OperationKind.Conditional,
                        OperationKind.Coalesce,
                        OperationKind.AnonymousFunction,
                        OperationKind.ObjectCreation,
                        OperationKind.TypeParameterObjectCreation,
                        OperationKind.ArrayCreation,
                        OperationKind.InstanceReference,
                        OperationKind.IsType,
                        OperationKind.Await,
                        OperationKind.SimpleAssignment,
                        OperationKind.CompoundAssignment,
                        OperationKind.Parenthesized,
                        OperationKind.EventAssignment,
                        OperationKind.ConditionalAccess,
                        OperationKind.ConditionalAccessInstance,
                        OperationKind.InterpolatedString,
                        OperationKind.AnonymousObjectCreation,
                        OperationKind.ObjectOrCollectionInitializer,
                        OperationKind.MemberInitializer,
                        OperationKind.NameOf,
                        OperationKind.Tuple,
                        OperationKind.DynamicObjectCreation,
                        OperationKind.DynamicMemberReference,
                        OperationKind.DynamicInvocation,
                        OperationKind.DynamicIndexerAccess,
                        OperationKind.TranslatedQuery,
                        OperationKind.DelegateCreation,
                        OperationKind.DefaultValue,
                        OperationKind.TypeOf,
                        OperationKind.SizeOf,
                        OperationKind.AddressOf,
                        OperationKind.IsPattern,
                              OperationKind.Increment,
                        OperationKind.Throw,
                              OperationKind.Decrement,
                        OperationKind.DeconstructionAssignment,
                        OperationKind.DeclarationExpression,
                        OperationKind.OmittedArgument,
                        OperationKind.FieldInitializer,
                        OperationKind.VariableInitializer,
                        OperationKind.PropertyInitializer,
                        OperationKind.ParameterInitializer,
                        OperationKind.ArrayInitializer,
                        OperationKind.VariableDeclarator,
                        OperationKind.VariableDeclaration,
                        OperationKind.Argument,
                        OperationKind.CatchClause,
                        OperationKind.SwitchCase,
                              OperationKind.CaseClause,
                        OperationKind.InterpolatedStringText,
                        OperationKind.Interpolation,
                        OperationKind.ConstantPattern,
                        OperationKind.DeclarationPattern,
                        OperationKind.TupleBinary,
      OperationKind.TupleBinaryOperator,
      OperationKind.MethodBodyOperation,
                        OperationKind.MethodBody,
                        OperationKind.ConstructorBody,
                              
      OperationKind.ConstructorBodyOperation,
                        OperationKind.Discard,
                        OperationKind.FlowCapture,
                        OperationKind.FlowCaptureReference,
                        OperationKind.IsNull,
                        OperationKind.CaughtException,
                        OperationKind.StaticLocalInitializationSemaphore,
                        OperationKind.FlowAnonymousFunction,
                        OperationKind.CoalesceAssignment,
                        OperationKind.Range,
                        OperationKind.ReDim,
                        OperationKind.ReDimClause,
                        OperationKind.RecursivePattern,
                        OperationKind.DiscardPattern,
                        OperationKind.SwitchExpression,
                        OperationKind.SwitchExpressionArm,
                        OperationKind.PropertySubpattern,
   };

      private static readonly Dictionary<OperationKind, HashSet<SyntaxKind>> OperationKindToSyntaxKinds =
         new Dictionary<OperationKind, HashSet<SyntaxKind>>()
         {
            {OperationKind.None, new HashSet<SyntaxKind> {
               SyntaxKind.Attribute,
               SyntaxKind.SimpleMemberAccessExpression,
               SyntaxKind.IdentifierName,
               SyntaxKind.PredefinedType,
               SyntaxKind.None,
               SyntaxKind.AliasQualifiedName,
            }},
            {OperationKind.Invalid, new HashSet<SyntaxKind> {
               SyntaxKind.ObjectCreationExpression,
               SyntaxKind.SimpleMemberAccessExpression,
               SyntaxKind.InvocationExpression,
               SyntaxKind.BaseConstructorInitializer,
               SyntaxKind.IdentifierName,
               SyntaxKind.ElementAccessExpression,
               SyntaxKind.WhereClause,
               SyntaxKind.None,
               SyntaxKind.SelectClause,
               SyntaxKind.JoinClause,
               SyntaxKind.FromClause,
               SyntaxKind.ComplexElementInitializerExpression,
               SyntaxKind.MemberBindingExpression,
               SyntaxKind.AscendingOrdering,
               SyntaxKind.GroupClause,
               SyntaxKind.DescendingOrdering,
               SyntaxKind.CastExpression,
               SyntaxKind.CoalesceExpression,
            }},
            {OperationKind.Block, new HashSet<SyntaxKind> {
               SyntaxKind.Block,
               SyntaxKind.IdentifierName,
               SyntaxKind.NotEqualsExpression,
               SyntaxKind.AnonymousObjectCreationExpression,
               SyntaxKind.SimpleMemberAccessExpression,
               SyntaxKind.EqualsExpression,
               SyntaxKind.ParenthesizedExpression,
               SyntaxKind.LogicalAndExpression,
               SyntaxKind.SimpleAssignmentExpression,
               SyntaxKind.InvocationExpression,
               SyntaxKind.LessThanExpression,
               SyntaxKind.ElementAccessExpression,
               SyntaxKind.BitwiseAndExpression,
               SyntaxKind.ObjectCreationExpression,
               SyntaxKind.LogicalNotExpression,
               SyntaxKind.None,
               SyntaxKind.AddExpression,
               SyntaxKind.ArrowExpressionClause,
               SyntaxKind.AwaitExpression,
               SyntaxKind.NullLiteralExpression,
               SyntaxKind.LogicalOrExpression,
               SyntaxKind.AndAssignmentExpression,
               SyntaxKind.AddAssignmentExpression,
               SyntaxKind.JoinClause,
               SyntaxKind.GreaterThanExpression,
               SyntaxKind.CastExpression,
               SyntaxKind.TrueLiteralExpression,
               SyntaxKind.GreaterThanOrEqualExpression,
               SyntaxKind.LessThanOrEqualExpression,
               SyntaxKind.IsExpression,
               SyntaxKind.ConditionalExpression,
               SyntaxKind.InterpolatedStringExpression,
               SyntaxKind.FromClause,
               SyntaxKind.CoalesceExpression,
               SyntaxKind.PostIncrementExpression,
               SyntaxKind.FalseLiteralExpression,
               SyntaxKind.MultiplyExpression,
               SyntaxKind.DivideExpression,
               SyntaxKind.PostDecrementExpression,
               SyntaxKind.AsExpression,
               SyntaxKind.ArrayCreationExpression,
               SyntaxKind.StringLiteralExpression,
            }},
            {OperationKind.VariableDeclarationGroup, new HashSet<SyntaxKind> {
               SyntaxKind.LocalDeclarationStatement,
               SyntaxKind.VariableDeclaration,
               SyntaxKind.None,
            }},
            {OperationKind.Switch, new HashSet<SyntaxKind> {
               SyntaxKind.SwitchStatement,
               SyntaxKind.None,
            }},
            {OperationKind.Loop, new HashSet<SyntaxKind> {
               SyntaxKind.ForEachStatement,
               SyntaxKind.ForStatement,
               SyntaxKind.WhileStatement,
               SyntaxKind.None,
               SyntaxKind.DoStatement,
               SyntaxKind.ForEachVariableStatement,
            }},
            {OperationKind.Labeled, new HashSet<SyntaxKind> {
               SyntaxKind.None,
            }},
            {OperationKind.Branch, new HashSet<SyntaxKind> {
               SyntaxKind.BreakStatement,
               SyntaxKind.ContinueStatement,
               SyntaxKind.None,
               SyntaxKind.GotoCaseStatement,
            }},
            {OperationKind.Empty, new HashSet<SyntaxKind> {
               SyntaxKind.EmptyStatement,
            }},
            {OperationKind.Return, new HashSet<SyntaxKind> {
               SyntaxKind.IdentifierName,
               SyntaxKind.ReturnStatement,
               SyntaxKind.NotEqualsExpression,
               SyntaxKind.AnonymousObjectCreationExpression,
               SyntaxKind.SimpleMemberAccessExpression,
               SyntaxKind.EqualsExpression,
               SyntaxKind.ParenthesizedExpression,
               SyntaxKind.LogicalAndExpression,
               SyntaxKind.SimpleAssignmentExpression,
               SyntaxKind.InvocationExpression,
               SyntaxKind.LessThanExpression,
               SyntaxKind.ElementAccessExpression,
               SyntaxKind.Block,
               SyntaxKind.BitwiseAndExpression,
               SyntaxKind.ObjectCreationExpression,
               SyntaxKind.LogicalNotExpression,
               SyntaxKind.None,
               SyntaxKind.AddExpression,
               SyntaxKind.CastExpression,
               SyntaxKind.AwaitExpression,
               SyntaxKind.NullLiteralExpression,
               SyntaxKind.LogicalOrExpression,
               SyntaxKind.AndAssignmentExpression,
               SyntaxKind.CoalesceExpression,
               SyntaxKind.NumericLiteralExpression,
               SyntaxKind.StringLiteralExpression,
               SyntaxKind.AddAssignmentExpression,
               SyntaxKind.JoinClause,
               SyntaxKind.GreaterThanExpression,
               SyntaxKind.TrueLiteralExpression,
               SyntaxKind.GreaterThanOrEqualExpression,
               SyntaxKind.LessThanOrEqualExpression,
               SyntaxKind.IsExpression,
               SyntaxKind.ConditionalExpression,
               SyntaxKind.FalseLiteralExpression,
               SyntaxKind.ConditionalAccessExpression,
               SyntaxKind.InterpolatedStringExpression,
               SyntaxKind.FromClause,
               SyntaxKind.PostIncrementExpression,
               SyntaxKind.AsExpression,
               SyntaxKind.MultiplyExpression,
               SyntaxKind.DivideExpression,
               SyntaxKind.PostDecrementExpression,
               SyntaxKind.ArrayCreationExpression,
            }},
            {OperationKind.YieldBreak, new HashSet<SyntaxKind> {
               SyntaxKind.YieldBreakStatement,
            }},
            {OperationKind.Lock, new HashSet<SyntaxKind> {
               SyntaxKind.LockStatement,
            }},
            {OperationKind.Try, new HashSet<SyntaxKind> {
               SyntaxKind.TryStatement,
               SyntaxKind.None,
            }},
            {OperationKind.Using, new HashSet<SyntaxKind> {
               SyntaxKind.UsingStatement,
               SyntaxKind.None,
            }},
            {OperationKind.YieldReturn, new HashSet<SyntaxKind> {
               SyntaxKind.YieldReturnStatement,
            }},
            {OperationKind.ExpressionStatement, new HashSet<SyntaxKind> {
               SyntaxKind.ExpressionStatement,
               SyntaxKind.PostIncrementExpression,
               SyntaxKind.SimpleAssignmentExpression,
               SyntaxKind.None,
               SyntaxKind.PostDecrementExpression,
               SyntaxKind.InvocationExpression,
               SyntaxKind.AwaitExpression,
               SyntaxKind.AndAssignmentExpression,
               SyntaxKind.AddAssignmentExpression,
               SyntaxKind.PreIncrementExpression,
            }},
            {OperationKind.LocalFunction, new HashSet<SyntaxKind> {
               SyntaxKind.LocalFunctionStatement,
            }},
            {OperationKind.Stop, new HashSet<SyntaxKind> {
            }},
            {OperationKind.End, new HashSet<SyntaxKind> {
            }},
            {OperationKind.RaiseEvent, new HashSet<SyntaxKind> {
            }},
            {OperationKind.Literal, new HashSet<SyntaxKind> {
               SyntaxKind.StringLiteralExpression,
               SyntaxKind.NullLiteralExpression,
               SyntaxKind.TrueLiteralExpression,
               SyntaxKind.FalseLiteralExpression,
               SyntaxKind.NumericLiteralExpression,
               SyntaxKind.ArrayCreationExpression,
               SyntaxKind.CharacterLiteralExpression,
               SyntaxKind.InvocationExpression,
               SyntaxKind.ObjectCreationExpression,
               SyntaxKind.None,
               SyntaxKind.InterpolatedStringText,
               SyntaxKind.ArrayInitializerExpression,
               SyntaxKind.ImplicitArrayCreationExpression,
               SyntaxKind.InterpolationFormatClause,
               SyntaxKind.BaseConstructorInitializer,
               SyntaxKind.UnaryMinusExpression,
               SyntaxKind.ThisConstructorInitializer,
            }},
            {OperationKind.Conversion, new HashSet<SyntaxKind> {
               SyntaxKind.NullLiteralExpression,
               SyntaxKind.CastExpression,
               SyntaxKind.ObjectCreationExpression,
               SyntaxKind.IdentifierName,
               SyntaxKind.LogicalNotExpression,
               SyntaxKind.StringLiteralExpression,
               SyntaxKind.InvocationExpression,
               SyntaxKind.CharacterLiteralExpression,
               SyntaxKind.SimpleMemberAccessExpression,
               SyntaxKind.ThisExpression,
               SyntaxKind.ParenthesizedExpression,
               SyntaxKind.ElementAccessExpression,
               SyntaxKind.GreaterThanExpression,
               SyntaxKind.AsExpression,
               SyntaxKind.NotEqualsExpression,
               SyntaxKind.AddExpression,
               SyntaxKind.EqualsExpression,
               SyntaxKind.LogicalAndExpression,
               SyntaxKind.ConditionalExpression,
               SyntaxKind.LogicalOrExpression,
               SyntaxKind.TrueLiteralExpression,
               SyntaxKind.FalseLiteralExpression,
               SyntaxKind.FromClause,
               SyntaxKind.LessThanExpression,
               SyntaxKind.IsExpression,
               SyntaxKind.NumericLiteralExpression,
               SyntaxKind.SubtractExpression,
               SyntaxKind.UnaryMinusExpression,
               SyntaxKind.BitwiseAndExpression,
               SyntaxKind.MultiplyExpression,
               SyntaxKind.LessThanOrEqualExpression,
               SyntaxKind.QueryExpression,
               SyntaxKind.None,
               SyntaxKind.DivideExpression,
               SyntaxKind.GreaterThanOrEqualExpression,
               SyntaxKind.ArrayCreationExpression,
               SyntaxKind.AwaitExpression,
               SyntaxKind.AnonymousObjectCreationExpression,
               SyntaxKind.ImplicitArrayCreationExpression,
               SyntaxKind.SimpleLambdaExpression,
               SyntaxKind.CoalesceExpression,
               SyntaxKind.DefaultExpression,
               SyntaxKind.ConditionalAccessExpression,
               SyntaxKind.InterpolatedStringExpression,
               SyntaxKind.OrderByClause,
               SyntaxKind.PostIncrementExpression,
               SyntaxKind.BitwiseOrExpression,
               SyntaxKind.ThrowExpression,
               SyntaxKind.IsPatternExpression,
               SyntaxKind.MemberBindingExpression,
               SyntaxKind.UnaryPlusExpression,
               SyntaxKind.SimpleAssignmentExpression,
               SyntaxKind.TypeOfExpression,
               SyntaxKind.JoinClause,
               SyntaxKind.PreIncrementExpression,
               SyntaxKind.ParenthesizedLambdaExpression,
               SyntaxKind.ModuloExpression,
               SyntaxKind.ExclusiveOrExpression,
               SyntaxKind.PostDecrementExpression,
            }},
            {OperationKind.Invocation, new HashSet<SyntaxKind> {
               SyntaxKind.InvocationExpression,
               SyntaxKind.SelectClause,
               SyntaxKind.BaseConstructorInitializer,
               SyntaxKind.WhereClause,
               SyntaxKind.StringLiteralExpression,
               SyntaxKind.FromClause,
               SyntaxKind.AscendingOrdering,
               SyntaxKind.IdentifierName,
               SyntaxKind.None,
               SyntaxKind.GroupClause,
               SyntaxKind.ThisConstructorInitializer,
               SyntaxKind.ObjectCreationExpression,
               SyntaxKind.ComplexElementInitializerExpression,
               SyntaxKind.SimpleMemberAccessExpression,
               SyntaxKind.NumericLiteralExpression,
               SyntaxKind.LetClause,
               SyntaxKind.ElementAccessExpression,
               SyntaxKind.DescendingOrdering,
               SyntaxKind.JoinClause,
               SyntaxKind.TypeOfExpression,
               SyntaxKind.CastExpression,
               SyntaxKind.NullLiteralExpression,
               SyntaxKind.AnonymousObjectCreationExpression,
               SyntaxKind.ConditionalExpression,
            }},
            {OperationKind.ArrayElementReference, new HashSet<SyntaxKind> {
               SyntaxKind.ElementAccessExpression,
               SyntaxKind.None,
            }},
            {OperationKind.LocalReference, new HashSet<SyntaxKind> {
               SyntaxKind.IdentifierName,
               SyntaxKind.None,
               SyntaxKind.SingleVariableDesignation,
            }},
            {OperationKind.ParameterReference, new HashSet<SyntaxKind> {
               SyntaxKind.IdentifierName,
               SyntaxKind.None,
               SyntaxKind.JoinClause,
               SyntaxKind.FromClause,
               SyntaxKind.LetClause,
            }},
            {OperationKind.FieldReference, new HashSet<SyntaxKind> {
               SyntaxKind.SimpleMemberAccessExpression,
               SyntaxKind.IdentifierName,
               SyntaxKind.None,
               SyntaxKind.InvocationExpression,
               SyntaxKind.ElementAccessExpression,
            }},
            {OperationKind.MethodReference, new HashSet<SyntaxKind> {
               SyntaxKind.IdentifierName,
               SyntaxKind.SimpleMemberAccessExpression,
               SyntaxKind.None,
            }},
            {OperationKind.PropertyReference, new HashSet<SyntaxKind> {
               SyntaxKind.SimpleMemberAccessExpression,
               SyntaxKind.IdentifierName,
               SyntaxKind.ElementAccessExpression,
               SyntaxKind.None,
               SyntaxKind.MemberBindingExpression,
               SyntaxKind.JoinClause,
               SyntaxKind.FromClause,
               SyntaxKind.LetClause,
               SyntaxKind.InvocationExpression,
               SyntaxKind.ConditionalExpression,
               SyntaxKind.ImplicitElementAccess,
            }},
            {OperationKind.EventReference, new HashSet<SyntaxKind> {
               SyntaxKind.SimpleMemberAccessExpression,
               SyntaxKind.IdentifierName,
               SyntaxKind.None,
            }},
            {OperationKind.UnaryOperator, new HashSet<SyntaxKind> {
               SyntaxKind.LogicalNotExpression,
               SyntaxKind.UnaryMinusExpression,
               SyntaxKind.None,
               SyntaxKind.LogicalOrExpression,
               SyntaxKind.EqualsExpression,
               SyntaxKind.NotEqualsExpression,
               SyntaxKind.SimpleMemberAccessExpression,
               SyntaxKind.UnaryPlusExpression,
               SyntaxKind.BitwiseNotExpression,
            }},
            {OperationKind.BinaryOperator, new HashSet<SyntaxKind> {
               SyntaxKind.AddExpression,
               SyntaxKind.EqualsExpression,
               SyntaxKind.LogicalAndExpression,
               SyntaxKind.NotEqualsExpression,
               SyntaxKind.LessThanOrEqualExpression,
               SyntaxKind.LogicalOrExpression,
               SyntaxKind.BitwiseAndExpression,
               SyntaxKind.MultiplyExpression,
               SyntaxKind.SubtractExpression,
               SyntaxKind.GreaterThanExpression,
               SyntaxKind.LessThanExpression,
               SyntaxKind.GreaterThanOrEqualExpression,
               SyntaxKind.DivideExpression,
               SyntaxKind.ModuloExpression,
               SyntaxKind.None,
               SyntaxKind.BitwiseOrExpression,
               SyntaxKind.LeftShiftExpression,
               SyntaxKind.ExclusiveOrExpression,
               SyntaxKind.RightShiftExpression,
            }},
            {OperationKind.Conditional, new HashSet<SyntaxKind> {
               SyntaxKind.IfStatement,
               SyntaxKind.ConditionalExpression,
               SyntaxKind.None,
            }},
            {OperationKind.Coalesce, new HashSet<SyntaxKind> {
               SyntaxKind.CoalesceExpression,
            }},
            {OperationKind.AnonymousFunction, new HashSet<SyntaxKind> {
               SyntaxKind.IdentifierName,
               SyntaxKind.SimpleLambdaExpression,
               SyntaxKind.EqualsExpression,
               SyntaxKind.SimpleMemberAccessExpression,
               SyntaxKind.LogicalAndExpression,
               SyntaxKind.InvocationExpression,
               SyntaxKind.ObjectCreationExpression,
               SyntaxKind.ParenthesizedLambdaExpression,
               SyntaxKind.AnonymousMethodExpression,
               SyntaxKind.AnonymousObjectCreationExpression,
               SyntaxKind.LogicalOrExpression,
               SyntaxKind.JoinClause,
               SyntaxKind.AddExpression,
               SyntaxKind.BitwiseAndExpression,
               SyntaxKind.NotEqualsExpression,
               SyntaxKind.ElementAccessExpression,
               SyntaxKind.LogicalNotExpression,
               SyntaxKind.ParenthesizedExpression,
               SyntaxKind.LessThanExpression,
               SyntaxKind.FromClause,
               SyntaxKind.ConditionalExpression,
               SyntaxKind.CastExpression,
               SyntaxKind.GreaterThanExpression,
               SyntaxKind.LessThanOrEqualExpression,
               SyntaxKind.AsExpression,
               SyntaxKind.GreaterThanOrEqualExpression,
               SyntaxKind.ArrayCreationExpression,
            }},
            {OperationKind.ObjectCreation, new HashSet<SyntaxKind> {
               SyntaxKind.ObjectCreationExpression,
               SyntaxKind.None,
               SyntaxKind.InvocationExpression,
            }},
            {OperationKind.TypeParameterObjectCreation, new HashSet<SyntaxKind> {
               SyntaxKind.ObjectCreationExpression,
            }},
            {OperationKind.ArrayCreation, new HashSet<SyntaxKind> {
               SyntaxKind.ArrayCreationExpression,
               SyntaxKind.InvocationExpression,
               SyntaxKind.ObjectCreationExpression,
               SyntaxKind.ArrayInitializerExpression,
               SyntaxKind.ImplicitArrayCreationExpression,
               SyntaxKind.None,
               SyntaxKind.BaseConstructorInitializer,
            }},
            {OperationKind.InstanceReference, new HashSet<SyntaxKind> {
               SyntaxKind.IdentifierName,
               SyntaxKind.ThisExpression,
               SyntaxKind.BaseExpression,
               SyntaxKind.AnonymousObjectCreationExpression,
               SyntaxKind.BaseConstructorInitializer,
               SyntaxKind.GenericName,
               SyntaxKind.None,
               SyntaxKind.ThisConstructorInitializer,
               SyntaxKind.QualifiedName,
               SyntaxKind.JoinClause,
               SyntaxKind.FromClause,
               SyntaxKind.LetClause,
               SyntaxKind.ImplicitElementAccess,
            }},
            {OperationKind.IsType, new HashSet<SyntaxKind> {
               SyntaxKind.IsExpression,
            }},
            {OperationKind.Await, new HashSet<SyntaxKind> {
               SyntaxKind.AwaitExpression,
            }},
            {OperationKind.SimpleAssignment, new HashSet<SyntaxKind> {
               SyntaxKind.SimpleAssignmentExpression,
               SyntaxKind.AnonymousObjectMemberDeclarator,
               SyntaxKind.AttributeArgument,
               SyntaxKind.None,
               SyntaxKind.QueryBody,
               SyntaxKind.LetClause,
               SyntaxKind.ParenthesizedExpression,
            }},
            {OperationKind.CompoundAssignment, new HashSet<SyntaxKind> {
               SyntaxKind.AddAssignmentExpression,
               SyntaxKind.SubtractAssignmentExpression,
               SyntaxKind.MultiplyAssignmentExpression,
               SyntaxKind.LeftShiftAssignmentExpression,
               SyntaxKind.AndAssignmentExpression,
               SyntaxKind.OrAssignmentExpression,
               SyntaxKind.None,
               SyntaxKind.DivideAssignmentExpression,
               SyntaxKind.ExclusiveOrAssignmentExpression,
            }},
            {OperationKind.Parenthesized, new HashSet<SyntaxKind> {
               SyntaxKind.None,
            }},
            {OperationKind.EventAssignment, new HashSet<SyntaxKind> {
               SyntaxKind.AddAssignmentExpression,
               SyntaxKind.SubtractAssignmentExpression,
               SyntaxKind.None,
            }},
            {OperationKind.ConditionalAccess, new HashSet<SyntaxKind> {
               SyntaxKind.ConditionalAccessExpression,
            }},
            {OperationKind.ConditionalAccessInstance, new HashSet<SyntaxKind> {
               SyntaxKind.SimpleMemberAccessExpression,
               SyntaxKind.ElementAccessExpression,
               SyntaxKind.IdentifierName,
               SyntaxKind.MemberBindingExpression,
               SyntaxKind.InvocationExpression,
            }},
            {OperationKind.InterpolatedString, new HashSet<SyntaxKind> {
               SyntaxKind.InterpolatedStringExpression,
            }},
            {OperationKind.AnonymousObjectCreation, new HashSet<SyntaxKind> {
               SyntaxKind.AnonymousObjectCreationExpression,
               SyntaxKind.JoinClause,
               SyntaxKind.FromClause,
               SyntaxKind.LetClause,
            }},
            {OperationKind.ObjectOrCollectionInitializer, new HashSet<SyntaxKind> {
               SyntaxKind.ObjectInitializerExpression,
               SyntaxKind.CollectionInitializerExpression,
               SyntaxKind.None,
            }},
            {OperationKind.MemberInitializer, new HashSet<SyntaxKind> {
               SyntaxKind.SimpleAssignmentExpression,
            }},
            {OperationKind.NameOf, new HashSet<SyntaxKind> {
               SyntaxKind.InvocationExpression,
            }},
            {OperationKind.Tuple, new HashSet<SyntaxKind> {
               SyntaxKind.ParenthesizedVariableDesignation,
               SyntaxKind.TupleExpression,
            }},
            {OperationKind.DynamicObjectCreation, new HashSet<SyntaxKind> {
            }},
            {OperationKind.DynamicMemberReference, new HashSet<SyntaxKind> {
               SyntaxKind.SimpleMemberAccessExpression,
               SyntaxKind.IdentifierName,
               SyntaxKind.None,
            }},
            {OperationKind.DynamicInvocation, new HashSet<SyntaxKind> {
               SyntaxKind.InvocationExpression,
               SyntaxKind.None,
            }},
            {OperationKind.DynamicIndexerAccess, new HashSet<SyntaxKind> {
            }},
            {OperationKind.TranslatedQuery, new HashSet<SyntaxKind> {
               SyntaxKind.QueryExpression,
            }},
            {OperationKind.DelegateCreation, new HashSet<SyntaxKind> {
               SyntaxKind.IdentifierName,
               SyntaxKind.SimpleLambdaExpression,
               SyntaxKind.ObjectCreationExpression,
               SyntaxKind.EqualsExpression,
               SyntaxKind.SimpleMemberAccessExpression,
               SyntaxKind.LogicalAndExpression,
               SyntaxKind.InvocationExpression,
               SyntaxKind.ParenthesizedLambdaExpression,
               SyntaxKind.AnonymousMethodExpression,
               SyntaxKind.AnonymousObjectCreationExpression,
               SyntaxKind.ElementAccessExpression,
               SyntaxKind.LogicalNotExpression,
               SyntaxKind.None,
               SyntaxKind.NotEqualsExpression,
               SyntaxKind.ParenthesizedExpression,
               SyntaxKind.LessThanExpression,
               SyntaxKind.FromClause,
               SyntaxKind.CastExpression,
               SyntaxKind.ConditionalExpression,
               SyntaxKind.LogicalOrExpression,
               SyntaxKind.JoinClause,
               SyntaxKind.LessThanOrEqualExpression,
            }},
            {OperationKind.DefaultValue, new HashSet<SyntaxKind> {
               SyntaxKind.DefaultExpression,
               SyntaxKind.InvocationExpression,
               SyntaxKind.ThisConstructorInitializer,
               SyntaxKind.ObjectCreationExpression,
               SyntaxKind.BaseConstructorInitializer,
            }},
            {OperationKind.TypeOf, new HashSet<SyntaxKind> {
               SyntaxKind.TypeOfExpression,
               SyntaxKind.None,
            }},
            {OperationKind.SizeOf, new HashSet<SyntaxKind> {
               SyntaxKind.SizeOfExpression,
            }},
            {OperationKind.AddressOf, new HashSet<SyntaxKind> {
            }},
            {OperationKind.IsPattern, new HashSet<SyntaxKind> {
               SyntaxKind.IsPatternExpression,
            }},
            {OperationKind.Increment, new HashSet<SyntaxKind> {
               SyntaxKind.PostIncrementExpression,
               SyntaxKind.PreIncrementExpression,
            }},
            {OperationKind.Throw, new HashSet<SyntaxKind> {
               SyntaxKind.ThrowStatement,
               SyntaxKind.None,
               SyntaxKind.ThrowExpression,
            }},
            {OperationKind.Decrement, new HashSet<SyntaxKind> {
               SyntaxKind.PostDecrementExpression,
               SyntaxKind.PreDecrementExpression,
            }},
            {OperationKind.DeconstructionAssignment, new HashSet<SyntaxKind> {
            }},
            {OperationKind.DeclarationExpression, new HashSet<SyntaxKind> {
               SyntaxKind.DeclarationExpression,
            }},
            {OperationKind.OmittedArgument, new HashSet<SyntaxKind> {
               SyntaxKind.None,
            }},
            {OperationKind.FieldInitializer, new HashSet<SyntaxKind> {
               SyntaxKind.EqualsValueClause,
               SyntaxKind.None,
            }},
            {OperationKind.VariableInitializer, new HashSet<SyntaxKind> {
               SyntaxKind.EqualsValueClause,
               SyntaxKind.None,
            }},
            {OperationKind.PropertyInitializer, new HashSet<SyntaxKind> {
               SyntaxKind.EqualsValueClause,
            }},
            {OperationKind.ParameterInitializer, new HashSet<SyntaxKind> {
               SyntaxKind.EqualsValueClause,
               SyntaxKind.None,
            }},
            {OperationKind.ArrayInitializer, new HashSet<SyntaxKind> {
               SyntaxKind.ArrayInitializerExpression,
               SyntaxKind.InvocationExpression,
               SyntaxKind.ObjectCreationExpression,
               SyntaxKind.None,
               SyntaxKind.BaseConstructorInitializer,
            }},
            {OperationKind.VariableDeclarator, new HashSet<SyntaxKind> {
               SyntaxKind.VariableDeclarator,
               SyntaxKind.IdentifierName,
               SyntaxKind.CatchDeclaration,
               SyntaxKind.PredefinedType,
               SyntaxKind.QualifiedName,
               SyntaxKind.None,
               SyntaxKind.GenericName,
               SyntaxKind.ArrayType,
               SyntaxKind.AliasQualifiedName,
               SyntaxKind.NullableType,
            }},
            {OperationKind.VariableDeclaration, new HashSet<SyntaxKind> {
               SyntaxKind.VariableDeclaration,
               SyntaxKind.None,
            }},
            {OperationKind.Argument, new HashSet<SyntaxKind> {
               SyntaxKind.Argument,
               SyntaxKind.InvocationExpression,
               SyntaxKind.IdentifierName,
               SyntaxKind.CastExpression,
               SyntaxKind.QueryExpression,
               SyntaxKind.EqualsExpression,
               SyntaxKind.SimpleMemberAccessExpression,
               SyntaxKind.FromClause,
               SyntaxKind.StringLiteralExpression,
               SyntaxKind.AsExpression,
               SyntaxKind.LogicalAndExpression,
               SyntaxKind.AscendingOrdering,
               SyntaxKind.ObjectCreationExpression,
               SyntaxKind.WhereClause,
               SyntaxKind.SimpleLambdaExpression,
               SyntaxKind.None,
               SyntaxKind.AnonymousObjectCreationExpression,
               SyntaxKind.GroupClause,
               SyntaxKind.ConditionalExpression,
               SyntaxKind.ThisExpression,
               SyntaxKind.DivideExpression,
               SyntaxKind.ElementAccessExpression,
               SyntaxKind.TypeOfExpression,
               SyntaxKind.LogicalNotExpression,
               SyntaxKind.MultiplyExpression,
               SyntaxKind.ArrayCreationExpression,
               SyntaxKind.SubtractExpression,
               SyntaxKind.NumericLiteralExpression,
               SyntaxKind.OrderByClause,
               SyntaxKind.NotEqualsExpression,
               SyntaxKind.ParenthesizedExpression,
               SyntaxKind.LessThanExpression,
               SyntaxKind.LetClause,
               SyntaxKind.AddExpression,
               SyntaxKind.MemberBindingExpression,
               SyntaxKind.BaseConstructorInitializer,
               SyntaxKind.CoalesceExpression,
               SyntaxKind.GreaterThanOrEqualExpression,
               SyntaxKind.LogicalOrExpression,
               SyntaxKind.JoinClause,
               SyntaxKind.ImplicitArrayCreationExpression,
               SyntaxKind.ThisConstructorInitializer,
               SyntaxKind.DescendingOrdering,
               SyntaxKind.UnaryMinusExpression,
               SyntaxKind.GreaterThanExpression,
               SyntaxKind.NullLiteralExpression,
               SyntaxKind.CharacterLiteralExpression,
               SyntaxKind.FalseLiteralExpression,
               SyntaxKind.LessThanOrEqualExpression,
               SyntaxKind.TrueLiteralExpression,
            }},
            {OperationKind.CatchClause, new HashSet<SyntaxKind> {
               SyntaxKind.CatchClause,
               SyntaxKind.None,
            }},
            {OperationKind.SwitchCase, new HashSet<SyntaxKind> {
               SyntaxKind.SwitchSection,
               SyntaxKind.None,
            }},
            {OperationKind.CaseClause, new HashSet<SyntaxKind> {
               SyntaxKind.CaseSwitchLabel,
               SyntaxKind.DefaultSwitchLabel,
               SyntaxKind.None,
               SyntaxKind.CasePatternSwitchLabel,
            }},
            {OperationKind.InterpolatedStringText, new HashSet<SyntaxKind> {
               SyntaxKind.InterpolatedStringText,
            }},
            {OperationKind.Interpolation, new HashSet<SyntaxKind> {
               SyntaxKind.Interpolation,
            }},
            {OperationKind.ConstantPattern, new HashSet<SyntaxKind> {
               SyntaxKind.CaseSwitchLabel,
               SyntaxKind.ConstantPattern,
            }},
            {OperationKind.DeclarationPattern, new HashSet<SyntaxKind> {
               SyntaxKind.DeclarationPattern,
            }},
            {OperationKind.TupleBinaryOperator, new HashSet<SyntaxKind> {
            }},
            {OperationKind.MethodBody, new HashSet<SyntaxKind> {
               SyntaxKind.MethodDeclaration,
               SyntaxKind.GetAccessorDeclaration,
               SyntaxKind.SetAccessorDeclaration,
               SyntaxKind.ConversionOperatorDeclaration,
               SyntaxKind.OperatorDeclaration,
               SyntaxKind.DestructorDeclaration,
               SyntaxKind.AddAccessorDeclaration,
               SyntaxKind.RemoveAccessorDeclaration,
            }},
            {OperationKind.ConstructorBody, new HashSet<SyntaxKind> {
               SyntaxKind.ConstructorDeclaration,
            }},
            {OperationKind.Discard, new HashSet<SyntaxKind> {
               SyntaxKind.IdentifierName,
               SyntaxKind.DeclarationExpression,
            }},
            {OperationKind.FlowCapture, new HashSet<SyntaxKind> {
            }},
            {OperationKind.FlowCaptureReference, new HashSet<SyntaxKind> {
            }},
            {OperationKind.IsNull, new HashSet<SyntaxKind> {
            }},
            {OperationKind.CaughtException, new HashSet<SyntaxKind> {
            }},
            {OperationKind.StaticLocalInitializationSemaphore, new HashSet<SyntaxKind> {
            }},
            {OperationKind.FlowAnonymousFunction, new HashSet<SyntaxKind> {
            }},
            {OperationKind.CoalesceAssignment, new HashSet<SyntaxKind> {
            }},
            {OperationKind.Range, new HashSet<SyntaxKind> {
            }},
            {OperationKind.ReDim, new HashSet<SyntaxKind> {
               SyntaxKind.None,
            }},
            {OperationKind.ReDimClause, new HashSet<SyntaxKind> {
               SyntaxKind.None,
            }},
            {OperationKind.RecursivePattern, new HashSet<SyntaxKind> {
            }},
            {OperationKind.DiscardPattern, new HashSet<SyntaxKind> {
            }},
            {OperationKind.SwitchExpression, new HashSet<SyntaxKind> {
            }},
            {OperationKind.SwitchExpressionArm, new HashSet<SyntaxKind> {
            }},
            {OperationKind.PropertySubpattern, new HashSet<SyntaxKind> {
            }},
         };
#if MAP_KINDS
      private List<Dictionary<OperationKind, HashSet<SyntaxKind>>> OperationToSyntaxKinds = new List<Dictionary<OperationKind, HashSet<SyntaxKind>>> { 
         new Dictionary<OperationKind, HashSet<SyntaxKind>> {
            {OperationKind.None, new HashSet<SyntaxKind> {
               SyntaxKind.Attribute,
               SyntaxKind.SimpleMemberAccessExpression,
               SyntaxKind.IdentifierName,
               SyntaxKind.PredefinedType,
               SyntaxKind.None}},
            {OperationKind.Invalid, new HashSet<SyntaxKind> {
               SyntaxKind.ObjectCreationExpression,
               SyntaxKind.SimpleMemberAccessExpression,
               SyntaxKind.InvocationExpression,
               SyntaxKind.BaseConstructorInitializer,
               SyntaxKind.IdentifierName,
               SyntaxKind.ElementAccessExpression,
               SyntaxKind.WhereClause,
               SyntaxKind.None,
               SyntaxKind.SelectClause,
               SyntaxKind.JoinClause,
               SyntaxKind.FromClause,
               SyntaxKind.ComplexElementInitializerExpression,
               SyntaxKind.MemberBindingExpression,
               SyntaxKind.AscendingOrdering,
               SyntaxKind.GroupClause,
               SyntaxKind.DescendingOrdering}},
            {OperationKind.Block, new HashSet<SyntaxKind> {
               SyntaxKind.Block,
               SyntaxKind.IdentifierName,
               SyntaxKind.NotEqualsExpression,
               SyntaxKind.AnonymousObjectCreationExpression,
               SyntaxKind.SimpleMemberAccessExpression,
               SyntaxKind.EqualsExpression,
               SyntaxKind.ParenthesizedExpression,
               SyntaxKind.LogicalAndExpression,
               SyntaxKind.SimpleAssignmentExpression,
               SyntaxKind.InvocationExpression,
               SyntaxKind.LessThanExpression,
               SyntaxKind.ElementAccessExpression,
               SyntaxKind.BitwiseAndExpression,
               SyntaxKind.ObjectCreationExpression,
               SyntaxKind.LogicalNotExpression,
               SyntaxKind.None,
               SyntaxKind.AddExpression,
               SyntaxKind.ArrowExpressionClause,
               SyntaxKind.AwaitExpression,
               SyntaxKind.NullLiteralExpression,
               SyntaxKind.LogicalOrExpression,
               SyntaxKind.AndAssignmentExpression,
               SyntaxKind.AddAssignmentExpression,
               SyntaxKind.JoinClause,
               SyntaxKind.GreaterThanExpression,
               SyntaxKind.CastExpression,
               SyntaxKind.TrueLiteralExpression,
               SyntaxKind.GreaterThanOrEqualExpression,
               SyntaxKind.LessThanOrEqualExpression,
               SyntaxKind.IsExpression,
               SyntaxKind.ConditionalExpression,
               SyntaxKind.InterpolatedStringExpression,
               SyntaxKind.FromClause,
               SyntaxKind.CoalesceExpression}},
            {OperationKind.VariableDeclarationGroup, new HashSet<SyntaxKind> {
               SyntaxKind.LocalDeclarationStatement,
               SyntaxKind.VariableDeclaration,
               SyntaxKind.None}},
            {OperationKind.Switch, new HashSet<SyntaxKind> {
               SyntaxKind.SwitchStatement,
               SyntaxKind.None}},
            {OperationKind.Loop, new HashSet<SyntaxKind> {
               SyntaxKind.ForEachStatement,
               SyntaxKind.ForStatement,
               SyntaxKind.WhileStatement,
               SyntaxKind.None,
               SyntaxKind.DoStatement,
               SyntaxKind.ForEachVariableStatement}},
            {OperationKind.Labeled, new HashSet<SyntaxKind> {
               SyntaxKind.None}},
            {OperationKind.Branch, new HashSet<SyntaxKind> {
               SyntaxKind.BreakStatement,
               SyntaxKind.ContinueStatement,
               SyntaxKind.None}},
            {OperationKind.Empty, new HashSet<SyntaxKind> {
               SyntaxKind.EmptyStatement}},
            {OperationKind.Return, new HashSet<SyntaxKind> {
               SyntaxKind.IdentifierName,
               SyntaxKind.ReturnStatement,
               SyntaxKind.NotEqualsExpression,
               SyntaxKind.AnonymousObjectCreationExpression,
               SyntaxKind.SimpleMemberAccessExpression,
               SyntaxKind.EqualsExpression,
               SyntaxKind.ParenthesizedExpression,
               SyntaxKind.LogicalAndExpression,
               SyntaxKind.SimpleAssignmentExpression,
               SyntaxKind.InvocationExpression,
               SyntaxKind.LessThanExpression,
               SyntaxKind.ElementAccessExpression,
               SyntaxKind.Block,
               SyntaxKind.BitwiseAndExpression,
               SyntaxKind.ObjectCreationExpression,
               SyntaxKind.LogicalNotExpression,
               SyntaxKind.None,
               SyntaxKind.AddExpression,
               SyntaxKind.CastExpression,
               SyntaxKind.AwaitExpression,
               SyntaxKind.NullLiteralExpression,
               SyntaxKind.LogicalOrExpression,
               SyntaxKind.AndAssignmentExpression,
               SyntaxKind.CoalesceExpression,
               SyntaxKind.NumericLiteralExpression,
               SyntaxKind.StringLiteralExpression,
               SyntaxKind.AddAssignmentExpression,
               SyntaxKind.JoinClause,
               SyntaxKind.GreaterThanExpression,
               SyntaxKind.TrueLiteralExpression,
               SyntaxKind.GreaterThanOrEqualExpression,
               SyntaxKind.LessThanOrEqualExpression,
               SyntaxKind.IsExpression,
               SyntaxKind.ConditionalExpression,
               SyntaxKind.FalseLiteralExpression,
               SyntaxKind.ConditionalAccessExpression,
               SyntaxKind.InterpolatedStringExpression,
               SyntaxKind.FromClause}},
            {OperationKind.YieldBreak, new HashSet<SyntaxKind> {
               }},
            {OperationKind.Lock, new HashSet<SyntaxKind> {
               SyntaxKind.LockStatement}},
            {OperationKind.Try, new HashSet<SyntaxKind> {
               SyntaxKind.TryStatement,
               SyntaxKind.None}},
            {OperationKind.Using, new HashSet<SyntaxKind> {
               SyntaxKind.UsingStatement,
               SyntaxKind.None}},
            {OperationKind.YieldReturn, new HashSet<SyntaxKind> {
               SyntaxKind.YieldReturnStatement}},
            {OperationKind.ExpressionStatement, new HashSet<SyntaxKind> {
               SyntaxKind.ExpressionStatement,
               SyntaxKind.PostIncrementExpression,
               SyntaxKind.SimpleAssignmentExpression,
               SyntaxKind.None,
               SyntaxKind.PostDecrementExpression,
               SyntaxKind.InvocationExpression,
               SyntaxKind.AwaitExpression,
               SyntaxKind.AndAssignmentExpression,
               SyntaxKind.AddAssignmentExpression,
               SyntaxKind.PreIncrementExpression}},
            {OperationKind.LocalFunction, new HashSet<SyntaxKind> {
               SyntaxKind.LocalFunctionStatement}},
            {OperationKind.Stop, new HashSet<SyntaxKind> {
               }},
            {OperationKind.End, new HashSet<SyntaxKind> {
               }},
            {OperationKind.RaiseEvent, new HashSet<SyntaxKind> {
               }},
            {OperationKind.Literal, new HashSet<SyntaxKind> {
               SyntaxKind.StringLiteralExpression,
               SyntaxKind.NullLiteralExpression,
               SyntaxKind.TrueLiteralExpression,
               SyntaxKind.FalseLiteralExpression,
               SyntaxKind.NumericLiteralExpression,
               SyntaxKind.ArrayCreationExpression,
               SyntaxKind.CharacterLiteralExpression,
               SyntaxKind.InvocationExpression,
               SyntaxKind.ObjectCreationExpression,
               SyntaxKind.None,
               SyntaxKind.InterpolatedStringText,
               SyntaxKind.ArrayInitializerExpression,
               SyntaxKind.ImplicitArrayCreationExpression,
               SyntaxKind.InterpolationFormatClause}},
            {OperationKind.Conversion, new HashSet<SyntaxKind> {
               SyntaxKind.NullLiteralExpression,
               SyntaxKind.CastExpression,
               SyntaxKind.ObjectCreationExpression,
               SyntaxKind.IdentifierName,
               SyntaxKind.LogicalNotExpression,
               SyntaxKind.StringLiteralExpression,
               SyntaxKind.InvocationExpression,
               SyntaxKind.CharacterLiteralExpression,
               SyntaxKind.SimpleMemberAccessExpression,
               SyntaxKind.ThisExpression,
               SyntaxKind.ParenthesizedExpression,
               SyntaxKind.ElementAccessExpression,
               SyntaxKind.GreaterThanExpression,
               SyntaxKind.AsExpression,
               SyntaxKind.NotEqualsExpression,
               SyntaxKind.AddExpression,
               SyntaxKind.EqualsExpression,
               SyntaxKind.LogicalAndExpression,
               SyntaxKind.ConditionalExpression,
               SyntaxKind.LogicalOrExpression,
               SyntaxKind.TrueLiteralExpression,
               SyntaxKind.FalseLiteralExpression,
               SyntaxKind.FromClause,
               SyntaxKind.LessThanExpression,
               SyntaxKind.IsExpression,
               SyntaxKind.NumericLiteralExpression,
               SyntaxKind.SubtractExpression,
               SyntaxKind.UnaryMinusExpression,
               SyntaxKind.BitwiseAndExpression,
               SyntaxKind.MultiplyExpression,
               SyntaxKind.LessThanOrEqualExpression,
               SyntaxKind.QueryExpression,
               SyntaxKind.None,
               SyntaxKind.DivideExpression,
               SyntaxKind.GreaterThanOrEqualExpression,
               SyntaxKind.ArrayCreationExpression,
               SyntaxKind.AwaitExpression,
               SyntaxKind.AnonymousObjectCreationExpression,
               SyntaxKind.ImplicitArrayCreationExpression,
               SyntaxKind.SimpleLambdaExpression,
               SyntaxKind.CoalesceExpression,
               SyntaxKind.DefaultExpression,
               SyntaxKind.ConditionalAccessExpression,
               SyntaxKind.InterpolatedStringExpression,
               SyntaxKind.OrderByClause,
               SyntaxKind.PostIncrementExpression,
               SyntaxKind.BitwiseOrExpression,
               SyntaxKind.ThrowExpression,
               SyntaxKind.IsPatternExpression,
               SyntaxKind.MemberBindingExpression}},
            {OperationKind.Invocation, new HashSet<SyntaxKind> {
               SyntaxKind.InvocationExpression,
               SyntaxKind.SelectClause,
               SyntaxKind.BaseConstructorInitializer,
               SyntaxKind.WhereClause,
               SyntaxKind.StringLiteralExpression,
               SyntaxKind.FromClause,
               SyntaxKind.AscendingOrdering,
               SyntaxKind.IdentifierName,
               SyntaxKind.None,
               SyntaxKind.GroupClause,
               SyntaxKind.ThisConstructorInitializer,
               SyntaxKind.ObjectCreationExpression,
               SyntaxKind.ComplexElementInitializerExpression,
               SyntaxKind.SimpleMemberAccessExpression,
               SyntaxKind.NumericLiteralExpression,
               SyntaxKind.LetClause,
               SyntaxKind.ElementAccessExpression}},
            {OperationKind.ArrayElementReference, new HashSet<SyntaxKind> {
               SyntaxKind.ElementAccessExpression,
               SyntaxKind.None}},
            {OperationKind.LocalReference, new HashSet<SyntaxKind> {
               SyntaxKind.IdentifierName,
               SyntaxKind.None,
               SyntaxKind.SingleVariableDesignation}},
            {OperationKind.ParameterReference, new HashSet<SyntaxKind> {
               SyntaxKind.IdentifierName,
               SyntaxKind.None,
               SyntaxKind.JoinClause,
               SyntaxKind.FromClause,
               SyntaxKind.LetClause}},
            {OperationKind.FieldReference, new HashSet<SyntaxKind> {
               SyntaxKind.SimpleMemberAccessExpression,
               SyntaxKind.IdentifierName,
               SyntaxKind.None,
               SyntaxKind.InvocationExpression,
               SyntaxKind.ElementAccessExpression}},
            {OperationKind.MethodReference, new HashSet<SyntaxKind> {
               SyntaxKind.IdentifierName,
               SyntaxKind.SimpleMemberAccessExpression,
               SyntaxKind.None}},
            {OperationKind.PropertyReference, new HashSet<SyntaxKind> {
               SyntaxKind.SimpleMemberAccessExpression,
               SyntaxKind.IdentifierName,
               SyntaxKind.ElementAccessExpression,
               SyntaxKind.None,
               SyntaxKind.MemberBindingExpression,
               SyntaxKind.JoinClause,
               SyntaxKind.FromClause,
               SyntaxKind.LetClause,
               SyntaxKind.InvocationExpression}},
            {OperationKind.EventReference, new HashSet<SyntaxKind> {
               SyntaxKind.SimpleMemberAccessExpression,
               SyntaxKind.IdentifierName,
               SyntaxKind.None}},
            {OperationKind.UnaryOperator, new HashSet<SyntaxKind> {
               SyntaxKind.LogicalNotExpression,
               SyntaxKind.UnaryMinusExpression,
               SyntaxKind.None,
               SyntaxKind.LogicalOrExpression,
               SyntaxKind.EqualsExpression,
               SyntaxKind.NotEqualsExpression,
               SyntaxKind.SimpleMemberAccessExpression}},
            {OperationKind.BinaryOperator, new HashSet<SyntaxKind> {
               SyntaxKind.AddExpression,
               SyntaxKind.EqualsExpression,
               SyntaxKind.LogicalAndExpression,
               SyntaxKind.NotEqualsExpression,
               SyntaxKind.LessThanOrEqualExpression,
               SyntaxKind.LogicalOrExpression,
               SyntaxKind.BitwiseAndExpression,
               SyntaxKind.MultiplyExpression,
               SyntaxKind.SubtractExpression,
               SyntaxKind.GreaterThanExpression,
               SyntaxKind.LessThanExpression,
               SyntaxKind.GreaterThanOrEqualExpression,
               SyntaxKind.DivideExpression,
               SyntaxKind.ModuloExpression,
               SyntaxKind.None,
               SyntaxKind.BitwiseOrExpression,
               SyntaxKind.LeftShiftExpression}},
            {OperationKind.Conditional, new HashSet<SyntaxKind> {
               SyntaxKind.IfStatement,
               SyntaxKind.ConditionalExpression,
               SyntaxKind.None}},
            {OperationKind.Coalesce, new HashSet<SyntaxKind> {
               SyntaxKind.CoalesceExpression}},
            {OperationKind.AnonymousFunction, new HashSet<SyntaxKind> {
               SyntaxKind.IdentifierName,
               SyntaxKind.SimpleLambdaExpression,
               SyntaxKind.EqualsExpression,
               SyntaxKind.SimpleMemberAccessExpression,
               SyntaxKind.LogicalAndExpression,
               SyntaxKind.InvocationExpression,
               SyntaxKind.ObjectCreationExpression,
               SyntaxKind.ParenthesizedLambdaExpression,
               SyntaxKind.AnonymousMethodExpression,
               SyntaxKind.AnonymousObjectCreationExpression,
               SyntaxKind.LogicalOrExpression,
               SyntaxKind.JoinClause,
               SyntaxKind.AddExpression,
               SyntaxKind.BitwiseAndExpression,
               SyntaxKind.NotEqualsExpression,
               SyntaxKind.ElementAccessExpression,
               SyntaxKind.LogicalNotExpression,
               SyntaxKind.ParenthesizedExpression,
               SyntaxKind.LessThanExpression,
               SyntaxKind.FromClause}},
            {OperationKind.ObjectCreation, new HashSet<SyntaxKind> {
               SyntaxKind.ObjectCreationExpression,
               SyntaxKind.None}},
            {OperationKind.TypeParameterObjectCreation, new HashSet<SyntaxKind> {
               SyntaxKind.ObjectCreationExpression}},
            {OperationKind.ArrayCreation, new HashSet<SyntaxKind> {
               SyntaxKind.ArrayCreationExpression,
               SyntaxKind.InvocationExpression,
               SyntaxKind.ObjectCreationExpression,
               SyntaxKind.ArrayInitializerExpression,
               SyntaxKind.ImplicitArrayCreationExpression,
               SyntaxKind.None}},
            {OperationKind.InstanceReference, new HashSet<SyntaxKind> {
               SyntaxKind.IdentifierName,
               SyntaxKind.ThisExpression,
               SyntaxKind.BaseExpression,
               SyntaxKind.AnonymousObjectCreationExpression,
               SyntaxKind.BaseConstructorInitializer,
               SyntaxKind.GenericName,
               SyntaxKind.None,
               SyntaxKind.ThisConstructorInitializer,
               SyntaxKind.QualifiedName,
               SyntaxKind.JoinClause,
               SyntaxKind.FromClause,
               SyntaxKind.LetClause}},
            {OperationKind.IsType, new HashSet<SyntaxKind> {
               SyntaxKind.IsExpression}},
            {OperationKind.Await, new HashSet<SyntaxKind> {
               SyntaxKind.AwaitExpression}},
            {OperationKind.SimpleAssignment, new HashSet<SyntaxKind> {
               SyntaxKind.SimpleAssignmentExpression,
               SyntaxKind.AnonymousObjectMemberDeclarator,
               SyntaxKind.AttributeArgument,
               SyntaxKind.None,
               SyntaxKind.QueryBody,
               SyntaxKind.LetClause}},
            {OperationKind.CompoundAssignment, new HashSet<SyntaxKind> {
               SyntaxKind.AddAssignmentExpression,
               SyntaxKind.SubtractAssignmentExpression,
               SyntaxKind.MultiplyAssignmentExpression,
               SyntaxKind.LeftShiftAssignmentExpression,
               SyntaxKind.AndAssignmentExpression,
               SyntaxKind.OrAssignmentExpression,
               SyntaxKind.None}},
            {OperationKind.Parenthesized, new HashSet<SyntaxKind> {
               SyntaxKind.None}},
            {OperationKind.EventAssignment, new HashSet<SyntaxKind> {
               SyntaxKind.AddAssignmentExpression,
               SyntaxKind.SubtractAssignmentExpression,
               SyntaxKind.None}},
            {OperationKind.ConditionalAccess, new HashSet<SyntaxKind> {
               SyntaxKind.ConditionalAccessExpression}},
            {OperationKind.ConditionalAccessInstance, new HashSet<SyntaxKind> {
               SyntaxKind.SimpleMemberAccessExpression,
               SyntaxKind.ElementAccessExpression,
               SyntaxKind.IdentifierName,
               SyntaxKind.MemberBindingExpression,
               SyntaxKind.InvocationExpression}},
            {OperationKind.InterpolatedString, new HashSet<SyntaxKind> {
               SyntaxKind.InterpolatedStringExpression}},
            {OperationKind.AnonymousObjectCreation, new HashSet<SyntaxKind> {
               SyntaxKind.AnonymousObjectCreationExpression,
               SyntaxKind.JoinClause,
               SyntaxKind.FromClause,
               SyntaxKind.LetClause}},
            {OperationKind.ObjectOrCollectionInitializer, new HashSet<SyntaxKind> {
               SyntaxKind.ObjectInitializerExpression,
               SyntaxKind.CollectionInitializerExpression,
               SyntaxKind.None}},
            {OperationKind.MemberInitializer, new HashSet<SyntaxKind> {
               SyntaxKind.SimpleAssignmentExpression}},
            {OperationKind.NameOf, new HashSet<SyntaxKind> {
               SyntaxKind.InvocationExpression}},
            {OperationKind.Tuple, new HashSet<SyntaxKind> {
               SyntaxKind.ParenthesizedVariableDesignation,
               SyntaxKind.TupleExpression}},
            {OperationKind.DynamicObjectCreation, new HashSet<SyntaxKind> {
               }},
            {OperationKind.DynamicMemberReference, new HashSet<SyntaxKind> {
               SyntaxKind.SimpleMemberAccessExpression,
               SyntaxKind.IdentifierName,
               SyntaxKind.None}},
            {OperationKind.DynamicInvocation, new HashSet<SyntaxKind> {
               SyntaxKind.InvocationExpression,
               SyntaxKind.None}},
            {OperationKind.DynamicIndexerAccess, new HashSet<SyntaxKind> {
               }},
            {OperationKind.TranslatedQuery, new HashSet<SyntaxKind> {
               SyntaxKind.QueryExpression}},
            {OperationKind.DelegateCreation, new HashSet<SyntaxKind> {
               SyntaxKind.IdentifierName,
               SyntaxKind.SimpleLambdaExpression,
               SyntaxKind.ObjectCreationExpression,
               SyntaxKind.EqualsExpression,
               SyntaxKind.SimpleMemberAccessExpression,
               SyntaxKind.LogicalAndExpression,
               SyntaxKind.InvocationExpression,
               SyntaxKind.ParenthesizedLambdaExpression,
               SyntaxKind.AnonymousMethodExpression,
               SyntaxKind.AnonymousObjectCreationExpression,
               SyntaxKind.ElementAccessExpression,
               SyntaxKind.LogicalNotExpression,
               SyntaxKind.None,
               SyntaxKind.NotEqualsExpression,
               SyntaxKind.ParenthesizedExpression,
               SyntaxKind.LessThanExpression,
               SyntaxKind.FromClause,
               SyntaxKind.CastExpression}},
            {OperationKind.DefaultValue, new HashSet<SyntaxKind> {
               SyntaxKind.DefaultExpression,
               SyntaxKind.InvocationExpression}},
            {OperationKind.TypeOf, new HashSet<SyntaxKind> {
               SyntaxKind.TypeOfExpression,
               SyntaxKind.None}},
            {OperationKind.SizeOf, new HashSet<SyntaxKind> {
               SyntaxKind.SizeOfExpression}},
            {OperationKind.AddressOf, new HashSet<SyntaxKind> {
               }},
            {OperationKind.IsPattern, new HashSet<SyntaxKind> {
               SyntaxKind.IsPatternExpression}},
            {OperationKind.Increment, new HashSet<SyntaxKind> {
               SyntaxKind.PostIncrementExpression,
               SyntaxKind.PreIncrementExpression}},
            {OperationKind.Throw, new HashSet<SyntaxKind> {
               SyntaxKind.ThrowStatement,
               SyntaxKind.None,
               SyntaxKind.ThrowExpression}},
            {OperationKind.Decrement, new HashSet<SyntaxKind> {
               SyntaxKind.PostDecrementExpression}},
            {OperationKind.DeconstructionAssignment, new HashSet<SyntaxKind> {
               }},
            {OperationKind.DeclarationExpression, new HashSet<SyntaxKind> {
               SyntaxKind.DeclarationExpression}},
            {OperationKind.OmittedArgument, new HashSet<SyntaxKind> {
               SyntaxKind.None}},
            {OperationKind.FieldInitializer, new HashSet<SyntaxKind> {
               SyntaxKind.EqualsValueClause,
               SyntaxKind.None}},
            {OperationKind.VariableInitializer, new HashSet<SyntaxKind> {
               SyntaxKind.EqualsValueClause,
               SyntaxKind.None}},
            {OperationKind.PropertyInitializer, new HashSet<SyntaxKind> {
               SyntaxKind.EqualsValueClause}},
            {OperationKind.ParameterInitializer, new HashSet<SyntaxKind> {
               SyntaxKind.EqualsValueClause,
               SyntaxKind.None}},
            {OperationKind.ArrayInitializer, new HashSet<SyntaxKind> {
               SyntaxKind.ArrayInitializerExpression,
               SyntaxKind.InvocationExpression,
               SyntaxKind.ObjectCreationExpression,
               SyntaxKind.None}},
            {OperationKind.VariableDeclarator, new HashSet<SyntaxKind> {
               SyntaxKind.VariableDeclarator,
               SyntaxKind.IdentifierName,
               SyntaxKind.CatchDeclaration,
               SyntaxKind.PredefinedType,
               SyntaxKind.QualifiedName,
               SyntaxKind.None,
               SyntaxKind.GenericName,
               SyntaxKind.ArrayType}},
            {OperationKind.VariableDeclaration, new HashSet<SyntaxKind> {
               SyntaxKind.VariableDeclaration,
               SyntaxKind.None}},
            {OperationKind.Argument, new HashSet<SyntaxKind> {
               SyntaxKind.Argument,
               SyntaxKind.InvocationExpression,
               SyntaxKind.IdentifierName,
               SyntaxKind.CastExpression,
               SyntaxKind.QueryExpression,
               SyntaxKind.EqualsExpression,
               SyntaxKind.SimpleMemberAccessExpression,
               SyntaxKind.FromClause,
               SyntaxKind.StringLiteralExpression,
               SyntaxKind.AsExpression,
               SyntaxKind.LogicalAndExpression,
               SyntaxKind.AscendingOrdering,
               SyntaxKind.ObjectCreationExpression,
               SyntaxKind.WhereClause,
               SyntaxKind.SimpleLambdaExpression,
               SyntaxKind.None,
               SyntaxKind.AnonymousObjectCreationExpression,
               SyntaxKind.GroupClause,
               SyntaxKind.ConditionalExpression,
               SyntaxKind.ThisExpression,
               SyntaxKind.DivideExpression,
               SyntaxKind.ElementAccessExpression,
               SyntaxKind.TypeOfExpression,
               SyntaxKind.LogicalNotExpression,
               SyntaxKind.MultiplyExpression,
               SyntaxKind.ArrayCreationExpression,
               SyntaxKind.SubtractExpression,
               SyntaxKind.NumericLiteralExpression,
               SyntaxKind.OrderByClause,
               SyntaxKind.NotEqualsExpression,
               SyntaxKind.ParenthesizedExpression,
               SyntaxKind.LessThanExpression,
               SyntaxKind.LetClause,
               SyntaxKind.AddExpression,
               SyntaxKind.MemberBindingExpression}},
            {OperationKind.CatchClause, new HashSet<SyntaxKind> {
               SyntaxKind.CatchClause,
               SyntaxKind.None}},
            {OperationKind.SwitchCase, new HashSet<SyntaxKind> {
               SyntaxKind.SwitchSection,
               SyntaxKind.None}},
            {OperationKind.CaseClause, new HashSet<SyntaxKind> {
               SyntaxKind.CaseSwitchLabel,
               SyntaxKind.DefaultSwitchLabel,
               SyntaxKind.None,
               SyntaxKind.CasePatternSwitchLabel}},
            {OperationKind.InterpolatedStringText, new HashSet<SyntaxKind> {
               SyntaxKind.InterpolatedStringText}},
            {OperationKind.Interpolation, new HashSet<SyntaxKind> {
               SyntaxKind.Interpolation}},
            {OperationKind.ConstantPattern, new HashSet<SyntaxKind> {
               SyntaxKind.CaseSwitchLabel,
               SyntaxKind.ConstantPattern}},
            {OperationKind.DeclarationPattern, new HashSet<SyntaxKind> {
               SyntaxKind.DeclarationPattern}},
            {OperationKind.TupleBinaryOperator, new HashSet<SyntaxKind> {
               }},
            {OperationKind.MethodBody, new HashSet<SyntaxKind> {
               SyntaxKind.MethodDeclaration,
               SyntaxKind.GetAccessorDeclaration,
               SyntaxKind.SetAccessorDeclaration,
               SyntaxKind.ConversionOperatorDeclaration,
               SyntaxKind.OperatorDeclaration,
               SyntaxKind.DestructorDeclaration}},
            {OperationKind.ConstructorBody, new HashSet<SyntaxKind> {
               SyntaxKind.ConstructorDeclaration}},
            {OperationKind.Discard, new HashSet<SyntaxKind> {
               SyntaxKind.IdentifierName,
               SyntaxKind.DeclarationExpression}},
            {OperationKind.FlowCapture, new HashSet<SyntaxKind> {
               }},
            {OperationKind.FlowCaptureReference, new HashSet<SyntaxKind> {
               }},
            {OperationKind.IsNull, new HashSet<SyntaxKind> {
               }},
            {OperationKind.CaughtException, new HashSet<SyntaxKind> {
               }},
            {OperationKind.StaticLocalInitializationSemaphore, new HashSet<SyntaxKind> {
               }},
            {OperationKind.FlowAnonymousFunction, new HashSet<SyntaxKind> {
               }},
            {OperationKind.CoalesceAssignment, new HashSet<SyntaxKind> {
               }},
            {OperationKind.Range, new HashSet<SyntaxKind> {
               }},
            {OperationKind.ReDim, new HashSet<SyntaxKind> {
               SyntaxKind.None}},
            {OperationKind.ReDimClause, new HashSet<SyntaxKind> {
               SyntaxKind.None}},
            {OperationKind.RecursivePattern, new HashSet<SyntaxKind> {
               }},
            {OperationKind.DiscardPattern, new HashSet<SyntaxKind> {
               }},
            {OperationKind.SwitchExpression, new HashSet<SyntaxKind> {
               }},
            {OperationKind.SwitchExpressionArm, new HashSet<SyntaxKind> {
               }},
            {OperationKind.PropertySubpattern, new HashSet<SyntaxKind> {
               }},
         },
         new Dictionary<OperationKind, HashSet<SyntaxKind>> {
         {OperationKind.None, new HashSet<SyntaxKind> {
            SyntaxKind.Attribute,
            SyntaxKind.IdentifierName,
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxKind.PredefinedType,
            SyntaxKind.AliasQualifiedName}},
         {OperationKind.Invalid, new HashSet<SyntaxKind> {
            SyntaxKind.InvocationExpression,
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxKind.IdentifierName,
            SyntaxKind.ObjectCreationExpression,
            SyntaxKind.ElementAccessExpression,
            SyntaxKind.CastExpression,
            SyntaxKind.CoalesceExpression,
            SyntaxKind.BaseConstructorInitializer}},
         {OperationKind.Block, new HashSet<SyntaxKind> {
            SyntaxKind.Block,
            SyntaxKind.EqualsExpression,
            SyntaxKind.SimpleMemberAccessExpression}},
         {OperationKind.VariableDeclarationGroup, new HashSet<SyntaxKind> {
            SyntaxKind.LocalDeclarationStatement,
            SyntaxKind.VariableDeclaration}},
         {OperationKind.Switch, new HashSet<SyntaxKind> {
            SyntaxKind.SwitchStatement}},
         {OperationKind.Loop, new HashSet<SyntaxKind> {
            SyntaxKind.WhileStatement,
            SyntaxKind.ForStatement,
            SyntaxKind.ForEachStatement,
            SyntaxKind.DoStatement}},
         {OperationKind.Labeled, new HashSet<SyntaxKind> {
            }},
         {OperationKind.Branch, new HashSet<SyntaxKind> {
            SyntaxKind.BreakStatement,
            SyntaxKind.ContinueStatement}},
         {OperationKind.Empty, new HashSet<SyntaxKind> {
            SyntaxKind.EmptyStatement}},
         {OperationKind.Return, new HashSet<SyntaxKind> {
            SyntaxKind.ReturnStatement,
            SyntaxKind.EqualsExpression,
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxKind.Block}},
         {OperationKind.YieldBreak, new HashSet<SyntaxKind> {
            }},
         {OperationKind.Lock, new HashSet<SyntaxKind> {
            SyntaxKind.LockStatement}},
         {OperationKind.Try, new HashSet<SyntaxKind> {
            SyntaxKind.TryStatement}},
         {OperationKind.Using, new HashSet<SyntaxKind> {
            SyntaxKind.UsingStatement}},
         {OperationKind.YieldReturn, new HashSet<SyntaxKind> {
            }},
         {OperationKind.ExpressionStatement, new HashSet<SyntaxKind> {
            SyntaxKind.ExpressionStatement,
            SyntaxKind.PostIncrementExpression,
            SyntaxKind.PostDecrementExpression,
            SyntaxKind.SimpleAssignmentExpression,
            SyntaxKind.AddAssignmentExpression,
            SyntaxKind.InvocationExpression}},
         {OperationKind.LocalFunction, new HashSet<SyntaxKind> {
            }},
         {OperationKind.Stop, new HashSet<SyntaxKind> {
            }},
         {OperationKind.End, new HashSet<SyntaxKind> {
            }},
         {OperationKind.RaiseEvent, new HashSet<SyntaxKind> {
            }},
         {OperationKind.Literal, new HashSet<SyntaxKind> {
            SyntaxKind.StringLiteralExpression,
            SyntaxKind.NullLiteralExpression,
            SyntaxKind.FalseLiteralExpression,
            SyntaxKind.NumericLiteralExpression,
            SyntaxKind.TrueLiteralExpression,
            SyntaxKind.InvocationExpression,
            SyntaxKind.ArrayCreationExpression,
            SyntaxKind.CharacterLiteralExpression,
            SyntaxKind.ObjectCreationExpression,
            SyntaxKind.BaseConstructorInitializer,
            SyntaxKind.ArrayInitializerExpression,
            SyntaxKind.UnaryMinusExpression}},
         {OperationKind.Conversion, new HashSet<SyntaxKind> {
            SyntaxKind.NullLiteralExpression,
            SyntaxKind.CastExpression,
            SyntaxKind.ObjectCreationExpression,
            SyntaxKind.IdentifierName,
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxKind.ThisExpression,
            SyntaxKind.AsExpression,
            SyntaxKind.NumericLiteralExpression,
            SyntaxKind.PostIncrementExpression,
            SyntaxKind.InvocationExpression,
            SyntaxKind.ElementAccessExpression,
            SyntaxKind.AddExpression,
            SyntaxKind.TrueLiteralExpression,
            SyntaxKind.FalseLiteralExpression,
            SyntaxKind.UnaryMinusExpression,
            SyntaxKind.StringLiteralExpression,
            SyntaxKind.MultiplyExpression,
            SyntaxKind.NotEqualsExpression,
            SyntaxKind.LessThanOrEqualExpression,
            SyntaxKind.SubtractExpression,
            SyntaxKind.ConditionalExpression,
            SyntaxKind.ArrayCreationExpression,
            SyntaxKind.LogicalAndExpression,
            SyntaxKind.EqualsExpression,
            SyntaxKind.LessThanExpression,
            SyntaxKind.LogicalNotExpression,
            SyntaxKind.GreaterThanExpression,
            SyntaxKind.DivideExpression,
            SyntaxKind.LogicalOrExpression,
            SyntaxKind.CharacterLiteralExpression,
            SyntaxKind.IsExpression,
            SyntaxKind.CoalesceExpression,
            SyntaxKind.UnaryPlusExpression,
            SyntaxKind.ParenthesizedExpression,
            SyntaxKind.GreaterThanOrEqualExpression,
            SyntaxKind.SimpleAssignmentExpression,
            SyntaxKind.DefaultExpression}},
         {OperationKind.Invocation, new HashSet<SyntaxKind> {
            SyntaxKind.InvocationExpression,
            SyntaxKind.BaseConstructorInitializer,
            SyntaxKind.StringLiteralExpression,
            SyntaxKind.ThisConstructorInitializer,
            SyntaxKind.IdentifierName}},
         {OperationKind.ArrayElementReference, new HashSet<SyntaxKind> {
            SyntaxKind.ElementAccessExpression}},
         {OperationKind.LocalReference, new HashSet<SyntaxKind> {
            SyntaxKind.IdentifierName}},
         {OperationKind.ParameterReference, new HashSet<SyntaxKind> {
            SyntaxKind.IdentifierName}},
         {OperationKind.FieldReference, new HashSet<SyntaxKind> {
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxKind.IdentifierName}},
         {OperationKind.MethodReference, new HashSet<SyntaxKind> {
            SyntaxKind.IdentifierName,
            SyntaxKind.SimpleMemberAccessExpression}},
         {OperationKind.PropertyReference, new HashSet<SyntaxKind> {
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxKind.IdentifierName,
            SyntaxKind.ElementAccessExpression}},
         {OperationKind.EventReference, new HashSet<SyntaxKind> {
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxKind.IdentifierName}},
         {OperationKind.UnaryOperator, new HashSet<SyntaxKind> {
            SyntaxKind.LogicalNotExpression,
            SyntaxKind.UnaryMinusExpression,
            SyntaxKind.UnaryPlusExpression}},
         {OperationKind.BinaryOperator, new HashSet<SyntaxKind> {
            SyntaxKind.EqualsExpression,
            SyntaxKind.AddExpression,
            SyntaxKind.NotEqualsExpression,
            SyntaxKind.MultiplyExpression,
            SyntaxKind.LessThanOrEqualExpression,
            SyntaxKind.LessThanExpression,
            SyntaxKind.LogicalAndExpression,
            SyntaxKind.SubtractExpression,
            SyntaxKind.GreaterThanExpression,
            SyntaxKind.DivideExpression,
            SyntaxKind.LogicalOrExpression,
            SyntaxKind.BitwiseOrExpression,
            SyntaxKind.GreaterThanOrEqualExpression,
            SyntaxKind.BitwiseAndExpression,
            SyntaxKind.ExclusiveOrExpression,
            SyntaxKind.ModuloExpression}},
         {OperationKind.Conditional, new HashSet<SyntaxKind> {
            SyntaxKind.IfStatement,
            SyntaxKind.ConditionalExpression}},
         {OperationKind.Coalesce, new HashSet<SyntaxKind> {
            SyntaxKind.CoalesceExpression}},
         {OperationKind.AnonymousFunction, new HashSet<SyntaxKind> {
            SyntaxKind.AnonymousMethodExpression,
            SyntaxKind.SimpleLambdaExpression}},
         {OperationKind.ObjectCreation, new HashSet<SyntaxKind> {
            SyntaxKind.ObjectCreationExpression}},
         {OperationKind.TypeParameterObjectCreation, new HashSet<SyntaxKind> {
            SyntaxKind.ObjectCreationExpression}},
         {OperationKind.ArrayCreation, new HashSet<SyntaxKind> {
            SyntaxKind.InvocationExpression,
            SyntaxKind.ArrayCreationExpression,
            SyntaxKind.ObjectCreationExpression,
            SyntaxKind.BaseConstructorInitializer,
            SyntaxKind.ArrayInitializerExpression}},
         {OperationKind.InstanceReference, new HashSet<SyntaxKind> {
            SyntaxKind.IdentifierName,
            SyntaxKind.ThisExpression,
            SyntaxKind.BaseConstructorInitializer,
            SyntaxKind.BaseExpression,
            SyntaxKind.GenericName,
            SyntaxKind.ThisConstructorInitializer}},
         {OperationKind.IsType, new HashSet<SyntaxKind> {
            SyntaxKind.IsExpression}},
         {OperationKind.Await, new HashSet<SyntaxKind> {
            }},
         {OperationKind.SimpleAssignment, new HashSet<SyntaxKind> {
            SyntaxKind.AttributeArgument,
            SyntaxKind.SimpleAssignmentExpression}},
         {OperationKind.CompoundAssignment, new HashSet<SyntaxKind> {
            SyntaxKind.AddAssignmentExpression,
            SyntaxKind.SubtractAssignmentExpression,
            SyntaxKind.MultiplyAssignmentExpression,
            SyntaxKind.DivideAssignmentExpression,
            SyntaxKind.AndAssignmentExpression}},
         {OperationKind.Parenthesized, new HashSet<SyntaxKind> {
            }},
         {OperationKind.EventAssignment, new HashSet<SyntaxKind> {
            SyntaxKind.AddAssignmentExpression,
            SyntaxKind.SubtractAssignmentExpression}},
         {OperationKind.ConditionalAccess, new HashSet<SyntaxKind> {
            }},
         {OperationKind.ConditionalAccessInstance, new HashSet<SyntaxKind> {
            }},
         {OperationKind.InterpolatedString, new HashSet<SyntaxKind> {
            }},
         {OperationKind.AnonymousObjectCreation, new HashSet<SyntaxKind> {
            }},
         {OperationKind.ObjectOrCollectionInitializer, new HashSet<SyntaxKind> {
            SyntaxKind.CollectionInitializerExpression,
            SyntaxKind.ObjectInitializerExpression}},
         {OperationKind.MemberInitializer, new HashSet<SyntaxKind> {
            }},
         {OperationKind.NameOf, new HashSet<SyntaxKind> {
            }},
         {OperationKind.Tuple, new HashSet<SyntaxKind> {
            }},
         {OperationKind.DynamicObjectCreation, new HashSet<SyntaxKind> {
            }},
         {OperationKind.DynamicMemberReference, new HashSet<SyntaxKind> {
            }},
         {OperationKind.DynamicInvocation, new HashSet<SyntaxKind> {
            }},
         {OperationKind.DynamicIndexerAccess, new HashSet<SyntaxKind> {
            }},
         {OperationKind.TranslatedQuery, new HashSet<SyntaxKind> {
            }},
         {OperationKind.DelegateCreation, new HashSet<SyntaxKind> {
            SyntaxKind.ObjectCreationExpression,
            SyntaxKind.AnonymousMethodExpression,
            SyntaxKind.IdentifierName,
            SyntaxKind.SimpleLambdaExpression,
            SyntaxKind.SimpleMemberAccessExpression}},
         {OperationKind.DefaultValue, new HashSet<SyntaxKind> {
            SyntaxKind.DefaultExpression}},
         {OperationKind.TypeOf, new HashSet<SyntaxKind> {
            SyntaxKind.TypeOfExpression}},
         {OperationKind.SizeOf, new HashSet<SyntaxKind> {
            }},
         {OperationKind.AddressOf, new HashSet<SyntaxKind> {
            }},
         {OperationKind.IsPattern, new HashSet<SyntaxKind> {
            }},
         {OperationKind.Increment, new HashSet<SyntaxKind> {
            SyntaxKind.PostIncrementExpression,
            SyntaxKind.PreIncrementExpression}},
         {OperationKind.Throw, new HashSet<SyntaxKind> {
            SyntaxKind.ThrowStatement}},
         {OperationKind.Decrement, new HashSet<SyntaxKind> {
            SyntaxKind.PreDecrementExpression,
            SyntaxKind.PostDecrementExpression}},
         {OperationKind.DeconstructionAssignment, new HashSet<SyntaxKind> {
            }},
         {OperationKind.DeclarationExpression, new HashSet<SyntaxKind> {
            }},
         {OperationKind.OmittedArgument, new HashSet<SyntaxKind> {
            }},
         {OperationKind.FieldInitializer, new HashSet<SyntaxKind> {
            SyntaxKind.EqualsValueClause}},
         {OperationKind.VariableInitializer, new HashSet<SyntaxKind> {
            SyntaxKind.EqualsValueClause}},
         {OperationKind.PropertyInitializer, new HashSet<SyntaxKind> {
            }},
         {OperationKind.ParameterInitializer, new HashSet<SyntaxKind> {
            }},
         {OperationKind.ArrayInitializer, new HashSet<SyntaxKind> {
            SyntaxKind.InvocationExpression,
            SyntaxKind.ArrayInitializerExpression,
            SyntaxKind.ObjectCreationExpression,
            SyntaxKind.BaseConstructorInitializer}},
         {OperationKind.VariableDeclarator, new HashSet<SyntaxKind> {
            SyntaxKind.VariableDeclarator,
            SyntaxKind.CatchDeclaration,
            SyntaxKind.IdentifierName,
            SyntaxKind.PredefinedType,
            SyntaxKind.GenericName,
            SyntaxKind.QualifiedName,
            SyntaxKind.AliasQualifiedName,
            SyntaxKind.NullableType,
            SyntaxKind.ArrayType}},
         {OperationKind.VariableDeclaration, new HashSet<SyntaxKind> {
            SyntaxKind.VariableDeclaration}},
         {OperationKind.Argument, new HashSet<SyntaxKind> {
            SyntaxKind.Argument,
            SyntaxKind.InvocationExpression,
            SyntaxKind.CastExpression,
            SyntaxKind.ObjectCreationExpression,
            SyntaxKind.StringLiteralExpression,
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxKind.AsExpression,
            SyntaxKind.IdentifierName,
            SyntaxKind.MultiplyExpression,
            SyntaxKind.BaseConstructorInitializer,
            SyntaxKind.SubtractExpression,
            SyntaxKind.AddExpression,
            SyntaxKind.CoalesceExpression,
            SyntaxKind.ConditionalExpression,
            SyntaxKind.DivideExpression,
            SyntaxKind.LogicalAndExpression,
            SyntaxKind.GreaterThanOrEqualExpression,
            SyntaxKind.EqualsExpression,
            SyntaxKind.NotEqualsExpression,
            SyntaxKind.LogicalOrExpression}},
         {OperationKind.CatchClause, new HashSet<SyntaxKind> {
            SyntaxKind.CatchClause}},
         {OperationKind.SwitchCase, new HashSet<SyntaxKind> {
            SyntaxKind.SwitchSection}},
         {OperationKind.CaseClause, new HashSet<SyntaxKind> {
            SyntaxKind.CaseSwitchLabel,
            SyntaxKind.DefaultSwitchLabel}},
         {OperationKind.InterpolatedStringText, new HashSet<SyntaxKind> {
            }},
         {OperationKind.Interpolation, new HashSet<SyntaxKind> {
            }},
         {OperationKind.ConstantPattern, new HashSet<SyntaxKind> {
            SyntaxKind.CaseSwitchLabel}},
         {OperationKind.DeclarationPattern, new HashSet<SyntaxKind> {
            }},
         {OperationKind.TupleBinaryOperator, new HashSet<SyntaxKind> {
            }},
         {OperationKind.MethodBody, new HashSet<SyntaxKind> {
            SyntaxKind.GetAccessorDeclaration,
            SyntaxKind.SetAccessorDeclaration,
            SyntaxKind.MethodDeclaration,
            SyntaxKind.DestructorDeclaration,
            SyntaxKind.OperatorDeclaration,
            SyntaxKind.AddAccessorDeclaration,
            SyntaxKind.RemoveAccessorDeclaration}},
         {OperationKind.ConstructorBody, new HashSet<SyntaxKind> {
            SyntaxKind.ConstructorDeclaration}},
         {OperationKind.Discard, new HashSet<SyntaxKind> {
            }},
         {OperationKind.FlowCapture, new HashSet<SyntaxKind> {
            }},
         {OperationKind.FlowCaptureReference, new HashSet<SyntaxKind> {
            }},
         {OperationKind.IsNull, new HashSet<SyntaxKind> {
            }},
         {OperationKind.CaughtException, new HashSet<SyntaxKind> {
            }},
         {OperationKind.StaticLocalInitializationSemaphore, new HashSet<SyntaxKind> {
            }},
         {OperationKind.FlowAnonymousFunction, new HashSet<SyntaxKind> {
            }},
         {OperationKind.CoalesceAssignment, new HashSet<SyntaxKind> {
            }},
         {OperationKind.Range, new HashSet<SyntaxKind> {
            }},
         {OperationKind.ReDim, new HashSet<SyntaxKind> {
            }},
         {OperationKind.ReDimClause, new HashSet<SyntaxKind> {
            }},
         {OperationKind.RecursivePattern, new HashSet<SyntaxKind> {
            }},
         {OperationKind.DiscardPattern, new HashSet<SyntaxKind> {
            }},
         {OperationKind.SwitchExpression, new HashSet<SyntaxKind> {
            }},
         {OperationKind.SwitchExpressionArm, new HashSet<SyntaxKind> {
            }},
         {OperationKind.PropertySubpattern, new HashSet<SyntaxKind> {
            }},
         },
      new Dictionary<OperationKind, HashSet<SyntaxKind>> {
         {OperationKind.None, new HashSet<SyntaxKind> {
         SyntaxKind.Attribute,
         SyntaxKind.SimpleMemberAccessExpression,
         SyntaxKind.IdentifierName,
         SyntaxKind.PredefinedType}},
      {OperationKind.Invalid, new HashSet<SyntaxKind> {
         SyntaxKind.InvocationExpression,
         SyntaxKind.IdentifierName,
         SyntaxKind.ObjectCreationExpression,
         SyntaxKind.BaseConstructorInitializer,
         SyntaxKind.SimpleMemberAccessExpression,
         SyntaxKind.ElementAccessExpression,
         SyntaxKind.ComplexElementInitializerExpression}},
      {OperationKind.Block, new HashSet<SyntaxKind> {
         SyntaxKind.Block,
         SyntaxKind.EqualsExpression,
         SyntaxKind.ObjectCreationExpression,
         SyntaxKind.SimpleMemberAccessExpression,
         SyntaxKind.InvocationExpression,
         SyntaxKind.LogicalAndExpression,
         SyntaxKind.LogicalNotExpression,
         SyntaxKind.JoinClause,
         SyntaxKind.IdentifierName,
         SyntaxKind.AnonymousObjectCreationExpression,
         SyntaxKind.FromClause,
         SyntaxKind.ConditionalExpression,
         SyntaxKind.LogicalOrExpression,
         SyntaxKind.NotEqualsExpression,
         SyntaxKind.AddExpression,
         SyntaxKind.CastExpression,
         SyntaxKind.GreaterThanExpression,
         SyntaxKind.PostIncrementExpression,
         SyntaxKind.SimpleAssignmentExpression,
         SyntaxKind.FalseLiteralExpression,
         SyntaxKind.TrueLiteralExpression,
         SyntaxKind.AddAssignmentExpression,
         SyntaxKind.ParenthesizedExpression,
         SyntaxKind.ArrowExpressionClause,
         SyntaxKind.CoalesceExpression,
         SyntaxKind.ElementAccessExpression,
         SyntaxKind.LessThanOrEqualExpression,
         SyntaxKind.GreaterThanOrEqualExpression,
         SyntaxKind.LessThanExpression,
         SyntaxKind.MultiplyExpression,
         SyntaxKind.DivideExpression,
         SyntaxKind.NullLiteralExpression,
         SyntaxKind.PostDecrementExpression,
         SyntaxKind.AsExpression,
         SyntaxKind.IsExpression}},
      {OperationKind.VariableDeclarationGroup, new HashSet<SyntaxKind> {
         SyntaxKind.LocalDeclarationStatement,
         SyntaxKind.VariableDeclaration}},
      {OperationKind.Switch, new HashSet<SyntaxKind> {
         SyntaxKind.SwitchStatement}},
      {OperationKind.Loop, new HashSet<SyntaxKind> {
         SyntaxKind.WhileStatement,
         SyntaxKind.ForEachStatement,
         SyntaxKind.ForStatement,
         SyntaxKind.DoStatement}},
      {OperationKind.Labeled, new HashSet<SyntaxKind> {
         }},
      {OperationKind.Branch, new HashSet<SyntaxKind> {
         SyntaxKind.BreakStatement,
         SyntaxKind.ContinueStatement}},
      {OperationKind.Empty, new HashSet<SyntaxKind> {
         SyntaxKind.EmptyStatement}},
      {OperationKind.Return, new HashSet<SyntaxKind> {
         SyntaxKind.ReturnStatement,
         SyntaxKind.EqualsExpression,
         SyntaxKind.ObjectCreationExpression,
         SyntaxKind.SimpleMemberAccessExpression,
         SyntaxKind.InvocationExpression,
         SyntaxKind.LogicalAndExpression,
         SyntaxKind.LogicalNotExpression,
         SyntaxKind.JoinClause,
         SyntaxKind.IdentifierName,
         SyntaxKind.AnonymousObjectCreationExpression,
         SyntaxKind.FromClause,
         SyntaxKind.ConditionalExpression,
         SyntaxKind.LogicalOrExpression,
         SyntaxKind.NotEqualsExpression,
         SyntaxKind.AddExpression,
         SyntaxKind.CastExpression,
         SyntaxKind.GreaterThanExpression,
         SyntaxKind.PostIncrementExpression,
         SyntaxKind.SimpleAssignmentExpression,
         SyntaxKind.FalseLiteralExpression,
         SyntaxKind.TrueLiteralExpression,
         SyntaxKind.AddAssignmentExpression,
         SyntaxKind.ParenthesizedExpression,
         SyntaxKind.Block,
         SyntaxKind.CoalesceExpression,
         SyntaxKind.AsExpression,
         SyntaxKind.StringLiteralExpression,
         SyntaxKind.ElementAccessExpression,
         SyntaxKind.LessThanOrEqualExpression,
         SyntaxKind.GreaterThanOrEqualExpression,
         SyntaxKind.LessThanExpression,
         SyntaxKind.MultiplyExpression,
         SyntaxKind.DivideExpression,
         SyntaxKind.NullLiteralExpression,
         SyntaxKind.PostDecrementExpression,
         SyntaxKind.IsExpression}},
      {OperationKind.YieldBreak, new HashSet<SyntaxKind> {
         SyntaxKind.YieldBreakStatement}},
      {OperationKind.Lock, new HashSet<SyntaxKind> {
         SyntaxKind.LockStatement}},
      {OperationKind.Try, new HashSet<SyntaxKind> {
         SyntaxKind.TryStatement}},
      {OperationKind.Using, new HashSet<SyntaxKind> {
         SyntaxKind.UsingStatement}},
      {OperationKind.YieldReturn, new HashSet<SyntaxKind> {
         SyntaxKind.YieldReturnStatement}},
      {OperationKind.ExpressionStatement, new HashSet<SyntaxKind> {
         SyntaxKind.ExpressionStatement,
         SyntaxKind.InvocationExpression,
         SyntaxKind.PostIncrementExpression,
         SyntaxKind.PreIncrementExpression,
         SyntaxKind.SimpleAssignmentExpression,
         SyntaxKind.PostDecrementExpression,
         SyntaxKind.AddAssignmentExpression}},
      {OperationKind.LocalFunction, new HashSet<SyntaxKind> {
         }},
      {OperationKind.Stop, new HashSet<SyntaxKind> {
         }},
      {OperationKind.End, new HashSet<SyntaxKind> {
         }},
      {OperationKind.RaiseEvent, new HashSet<SyntaxKind> {
         }},
      {OperationKind.Literal, new HashSet<SyntaxKind> {
         SyntaxKind.StringLiteralExpression,
         SyntaxKind.FalseLiteralExpression,
         SyntaxKind.NumericLiteralExpression,
         SyntaxKind.NullLiteralExpression,
         SyntaxKind.TrueLiteralExpression,
         SyntaxKind.InvocationExpression,
         SyntaxKind.ObjectCreationExpression,
         SyntaxKind.ArrayInitializerExpression,
         SyntaxKind.ImplicitArrayCreationExpression,
         SyntaxKind.CharacterLiteralExpression,
         SyntaxKind.ArrayCreationExpression,
         SyntaxKind.InterpolatedStringText}},
      {OperationKind.Conversion, new HashSet<SyntaxKind> {
         SyntaxKind.ObjectCreationExpression,
         SyntaxKind.CastExpression,
         SyntaxKind.NullLiteralExpression,
         SyntaxKind.IdentifierName,
         SyntaxKind.StringLiteralExpression,
         SyntaxKind.InvocationExpression,
         SyntaxKind.SimpleMemberAccessExpression,
         SyntaxKind.TrueLiteralExpression,
         SyntaxKind.FalseLiteralExpression,
         SyntaxKind.ElementAccessExpression,
         SyntaxKind.LogicalNotExpression,
         SyntaxKind.NumericLiteralExpression,
         SyntaxKind.AsExpression,
         SyntaxKind.TypeOfExpression,
         SyntaxKind.SimpleLambdaExpression,
         SyntaxKind.ThisExpression,
         SyntaxKind.LogicalAndExpression,
         SyntaxKind.ConditionalExpression,
         SyntaxKind.EqualsExpression,
         SyntaxKind.JoinClause,
         SyntaxKind.OrderByClause,
         SyntaxKind.LogicalOrExpression,
         SyntaxKind.IsExpression,
         SyntaxKind.NotEqualsExpression,
         SyntaxKind.AnonymousObjectCreationExpression,
         SyntaxKind.CoalesceExpression,
         SyntaxKind.FromClause,
         SyntaxKind.DefaultExpression,
         SyntaxKind.SubtractExpression,
         SyntaxKind.ArrayCreationExpression,
         SyntaxKind.ImplicitArrayCreationExpression,
         SyntaxKind.UnaryMinusExpression,
         SyntaxKind.AddExpression,
         SyntaxKind.QueryExpression,
         SyntaxKind.MultiplyExpression,
         SyntaxKind.PreIncrementExpression,
         SyntaxKind.PostIncrementExpression,
         SyntaxKind.DivideExpression,
         SyntaxKind.ConditionalAccessExpression,
         SyntaxKind.MemberBindingExpression,
         SyntaxKind.InterpolatedStringExpression,
         SyntaxKind.CharacterLiteralExpression,
         SyntaxKind.ParenthesizedLambdaExpression,
         SyntaxKind.GreaterThanExpression,
         SyntaxKind.UnaryPlusExpression,
         SyntaxKind.ModuloExpression,
         SyntaxKind.LessThanOrEqualExpression,
         SyntaxKind.AwaitExpression,
         SyntaxKind.ExclusiveOrExpression}},
      {OperationKind.Invocation, new HashSet<SyntaxKind> {
         SyntaxKind.InvocationExpression,
         SyntaxKind.ComplexElementInitializerExpression,
         SyntaxKind.BaseConstructorInitializer,
         SyntaxKind.ThisConstructorInitializer,
         SyntaxKind.DescendingOrdering,
         SyntaxKind.WhereClause,
         SyntaxKind.SelectClause,
         SyntaxKind.JoinClause,
         SyntaxKind.IdentifierName,
         SyntaxKind.GroupClause,
         SyntaxKind.FromClause,
         SyntaxKind.ObjectCreationExpression,
         SyntaxKind.LetClause,
         SyntaxKind.StringLiteralExpression,
         SyntaxKind.SimpleMemberAccessExpression,
         SyntaxKind.TypeOfExpression,
         SyntaxKind.CastExpression,
         SyntaxKind.ElementAccessExpression,
         SyntaxKind.NumericLiteralExpression,
         SyntaxKind.NullLiteralExpression,
         SyntaxKind.AnonymousObjectCreationExpression}},
      {OperationKind.ArrayElementReference, new HashSet<SyntaxKind> {
         SyntaxKind.ElementAccessExpression}},
      {OperationKind.LocalReference, new HashSet<SyntaxKind> {
         SyntaxKind.IdentifierName}},
      {OperationKind.ParameterReference, new HashSet<SyntaxKind> {
         SyntaxKind.IdentifierName,
         SyntaxKind.JoinClause,
         SyntaxKind.FromClause,
         SyntaxKind.LetClause}},
      {OperationKind.FieldReference, new HashSet<SyntaxKind> {
         SyntaxKind.IdentifierName,
         SyntaxKind.SimpleMemberAccessExpression}},
      {OperationKind.MethodReference, new HashSet<SyntaxKind> {
         SyntaxKind.IdentifierName,
         SyntaxKind.SimpleMemberAccessExpression}},
      {OperationKind.PropertyReference, new HashSet<SyntaxKind> {
         SyntaxKind.IdentifierName,
         SyntaxKind.SimpleMemberAccessExpression,
         SyntaxKind.ElementAccessExpression,
         SyntaxKind.JoinClause,
         SyntaxKind.FromClause,
         SyntaxKind.LetClause,
         SyntaxKind.ConditionalExpression,
         SyntaxKind.InvocationExpression,
         SyntaxKind.MemberBindingExpression,
         SyntaxKind.ImplicitElementAccess}},
      {OperationKind.EventReference, new HashSet<SyntaxKind> {
         SyntaxKind.SimpleMemberAccessExpression}},
      {OperationKind.UnaryOperator, new HashSet<SyntaxKind> {
         SyntaxKind.LogicalNotExpression,
         SyntaxKind.UnaryMinusExpression,
         SyntaxKind.UnaryPlusExpression,
         SyntaxKind.NotEqualsExpression}},
      {OperationKind.BinaryOperator, new HashSet<SyntaxKind> {
         SyntaxKind.LogicalOrExpression,
         SyntaxKind.EqualsExpression,
         SyntaxKind.NotEqualsExpression,
         SyntaxKind.GreaterThanExpression,
         SyntaxKind.LogicalAndExpression,
         SyntaxKind.AddExpression,
         SyntaxKind.LessThanExpression,
         SyntaxKind.BitwiseOrExpression,
         SyntaxKind.SubtractExpression,
         SyntaxKind.LessThanOrEqualExpression,
         SyntaxKind.GreaterThanOrEqualExpression,
         SyntaxKind.DivideExpression,
         SyntaxKind.MultiplyExpression,
         SyntaxKind.ExclusiveOrExpression,
         SyntaxKind.ModuloExpression,
         SyntaxKind.BitwiseAndExpression,
         SyntaxKind.LeftShiftExpression}},
      {OperationKind.Conditional, new HashSet<SyntaxKind> {
         SyntaxKind.IfStatement,
         SyntaxKind.ConditionalExpression}},
      {OperationKind.Coalesce, new HashSet<SyntaxKind> {
         SyntaxKind.CoalesceExpression}},
      {OperationKind.AnonymousFunction, new HashSet<SyntaxKind> {
         SyntaxKind.SimpleLambdaExpression,
         SyntaxKind.ParenthesizedLambdaExpression,
         SyntaxKind.LogicalAndExpression,
         SyntaxKind.SimpleMemberAccessExpression,
         SyntaxKind.EqualsExpression,
         SyntaxKind.ObjectCreationExpression,
         SyntaxKind.JoinClause,
         SyntaxKind.IdentifierName,
         SyntaxKind.AnonymousObjectCreationExpression,
         SyntaxKind.InvocationExpression,
         SyntaxKind.FromClause,
         SyntaxKind.ConditionalExpression,
         SyntaxKind.NotEqualsExpression,
         SyntaxKind.LogicalNotExpression,
         SyntaxKind.LogicalOrExpression,
         SyntaxKind.AnonymousMethodExpression,
         SyntaxKind.ElementAccessExpression,
         SyntaxKind.CastExpression,
         SyntaxKind.GreaterThanExpression}},
      {OperationKind.ObjectCreation, new HashSet<SyntaxKind> {
         SyntaxKind.ObjectCreationExpression,
         SyntaxKind.InvocationExpression}},
      {OperationKind.TypeParameterObjectCreation, new HashSet<SyntaxKind> {
         SyntaxKind.ObjectCreationExpression}},
      {OperationKind.ArrayCreation, new HashSet<SyntaxKind> {
         SyntaxKind.InvocationExpression,
         SyntaxKind.ArrayInitializerExpression,
         SyntaxKind.ArrayCreationExpression,
         SyntaxKind.ImplicitArrayCreationExpression,
         SyntaxKind.ObjectCreationExpression}},
      {OperationKind.InstanceReference, new HashSet<SyntaxKind> {
         SyntaxKind.IdentifierName,
         SyntaxKind.GenericName,
         SyntaxKind.ThisExpression,
         SyntaxKind.BaseConstructorInitializer,
         SyntaxKind.BaseExpression,
         SyntaxKind.ThisConstructorInitializer,
         SyntaxKind.JoinClause,
         SyntaxKind.AnonymousObjectCreationExpression,
         SyntaxKind.FromClause,
         SyntaxKind.LetClause,
         SyntaxKind.QualifiedName,
         SyntaxKind.ImplicitElementAccess}},
      {OperationKind.IsType, new HashSet<SyntaxKind> {
         SyntaxKind.IsExpression}},
      {OperationKind.Await, new HashSet<SyntaxKind> {
         SyntaxKind.AwaitExpression}},
      {OperationKind.SimpleAssignment, new HashSet<SyntaxKind> {
         SyntaxKind.SimpleAssignmentExpression,
         SyntaxKind.AttributeArgument,
         SyntaxKind.QueryBody,
         SyntaxKind.AnonymousObjectMemberDeclarator,
         SyntaxKind.LetClause,
         SyntaxKind.ParenthesizedExpression}},
      {OperationKind.CompoundAssignment, new HashSet<SyntaxKind> {
         SyntaxKind.AddAssignmentExpression,
         SyntaxKind.SubtractAssignmentExpression,
         SyntaxKind.AndAssignmentExpression}},
      {OperationKind.Parenthesized, new HashSet<SyntaxKind> {
         }},
      {OperationKind.EventAssignment, new HashSet<SyntaxKind> {
         SyntaxKind.SubtractAssignmentExpression,
         SyntaxKind.AddAssignmentExpression}},
      {OperationKind.ConditionalAccess, new HashSet<SyntaxKind> {
         SyntaxKind.ConditionalAccessExpression}},
      {OperationKind.ConditionalAccessInstance, new HashSet<SyntaxKind> {
         SyntaxKind.IdentifierName,
         SyntaxKind.MemberBindingExpression,
         SyntaxKind.SimpleMemberAccessExpression,
         SyntaxKind.InvocationExpression}},
      {OperationKind.InterpolatedString, new HashSet<SyntaxKind> {
         SyntaxKind.InterpolatedStringExpression}},
      {OperationKind.AnonymousObjectCreation, new HashSet<SyntaxKind> {
         SyntaxKind.JoinClause,
         SyntaxKind.AnonymousObjectCreationExpression,
         SyntaxKind.FromClause,
         SyntaxKind.LetClause}},
      {OperationKind.ObjectOrCollectionInitializer, new HashSet<SyntaxKind> {
         SyntaxKind.ObjectInitializerExpression,
         SyntaxKind.CollectionInitializerExpression}},
      {OperationKind.MemberInitializer, new HashSet<SyntaxKind> {
         SyntaxKind.SimpleAssignmentExpression}},
      {OperationKind.NameOf, new HashSet<SyntaxKind> {
         SyntaxKind.InvocationExpression}},
      {OperationKind.Tuple, new HashSet<SyntaxKind> {
         }},
      {OperationKind.DynamicObjectCreation, new HashSet<SyntaxKind> {
         }},
      {OperationKind.DynamicMemberReference, new HashSet<SyntaxKind> {
         SyntaxKind.SimpleMemberAccessExpression,
         SyntaxKind.IdentifierName}},
      {OperationKind.DynamicInvocation, new HashSet<SyntaxKind> {
         SyntaxKind.InvocationExpression}},
      {OperationKind.DynamicIndexerAccess, new HashSet<SyntaxKind> {
         }},
      {OperationKind.TranslatedQuery, new HashSet<SyntaxKind> {
         SyntaxKind.QueryExpression}},
      {OperationKind.DelegateCreation, new HashSet<SyntaxKind> {
         SyntaxKind.SimpleLambdaExpression,
         SyntaxKind.IdentifierName,
         SyntaxKind.ParenthesizedLambdaExpression,
         SyntaxKind.SimpleMemberAccessExpression,
         SyntaxKind.ObjectCreationExpression,
         SyntaxKind.NotEqualsExpression,
         SyntaxKind.ConditionalExpression,
         SyntaxKind.InvocationExpression,
         SyntaxKind.LogicalNotExpression,
         SyntaxKind.LogicalAndExpression,
         SyntaxKind.FromClause,
         SyntaxKind.EqualsExpression,
         SyntaxKind.AnonymousObjectCreationExpression,
         SyntaxKind.AnonymousMethodExpression,
         SyntaxKind.ElementAccessExpression}},
      {OperationKind.DefaultValue, new HashSet<SyntaxKind> {
         SyntaxKind.DefaultExpression,
         SyntaxKind.InvocationExpression,
         SyntaxKind.ThisConstructorInitializer,
         SyntaxKind.ObjectCreationExpression}},
      {OperationKind.TypeOf, new HashSet<SyntaxKind> {
         SyntaxKind.TypeOfExpression}},
      {OperationKind.SizeOf, new HashSet<SyntaxKind> {
         }},
      {OperationKind.AddressOf, new HashSet<SyntaxKind> {
         }},
      {OperationKind.IsPattern, new HashSet<SyntaxKind> {
         }},
      {OperationKind.Increment, new HashSet<SyntaxKind> {
         SyntaxKind.PostIncrementExpression,
         SyntaxKind.PreIncrementExpression}},
      {OperationKind.Throw, new HashSet<SyntaxKind> {
         SyntaxKind.ThrowStatement}},
      {OperationKind.Decrement, new HashSet<SyntaxKind> {
         SyntaxKind.PostDecrementExpression}},
      {OperationKind.DeconstructionAssignment, new HashSet<SyntaxKind> {
         }},
      {OperationKind.DeclarationExpression, new HashSet<SyntaxKind> {
         }},
      {OperationKind.OmittedArgument, new HashSet<SyntaxKind> {
         }},
      {OperationKind.FieldInitializer, new HashSet<SyntaxKind> {
         SyntaxKind.EqualsValueClause}},
      {OperationKind.VariableInitializer, new HashSet<SyntaxKind> {
         SyntaxKind.EqualsValueClause}},
      {OperationKind.PropertyInitializer, new HashSet<SyntaxKind> {
         }},
      {OperationKind.ParameterInitializer, new HashSet<SyntaxKind> {
         SyntaxKind.EqualsValueClause}},
      {OperationKind.ArrayInitializer, new HashSet<SyntaxKind> {
         SyntaxKind.InvocationExpression,
         SyntaxKind.ArrayInitializerExpression,
         SyntaxKind.ObjectCreationExpression}},
      {OperationKind.VariableDeclarator, new HashSet<SyntaxKind> {
         SyntaxKind.VariableDeclarator,
         SyntaxKind.CatchDeclaration,
         SyntaxKind.IdentifierName,
         SyntaxKind.PredefinedType,
         SyntaxKind.GenericName,
         SyntaxKind.NullableType,
         SyntaxKind.ArrayType}},
      {OperationKind.VariableDeclaration, new HashSet<SyntaxKind> {
         SyntaxKind.VariableDeclaration}},
      {OperationKind.Argument, new HashSet<SyntaxKind> {
         SyntaxKind.Argument,
         SyntaxKind.SimpleMemberAccessExpression,
         SyntaxKind.ObjectCreationExpression,
         SyntaxKind.InvocationExpression,
         SyntaxKind.IdentifierName,
         SyntaxKind.TypeOfExpression,
         SyntaxKind.NumericLiteralExpression,
         SyntaxKind.StringLiteralExpression,
         SyntaxKind.WhereClause,
         SyntaxKind.LogicalAndExpression,
         SyntaxKind.QueryExpression,
         SyntaxKind.EqualsExpression,
         SyntaxKind.JoinClause,
         SyntaxKind.OrderByClause,
         SyntaxKind.AnonymousObjectCreationExpression,
         SyntaxKind.GroupClause,
         SyntaxKind.FromClause,
         SyntaxKind.ConditionalExpression,
         SyntaxKind.MultiplyExpression,
         SyntaxKind.CastExpression,
         SyntaxKind.NotEqualsExpression,
         SyntaxKind.LetClause,
         SyntaxKind.LogicalNotExpression,
         SyntaxKind.SimpleLambdaExpression,
         SyntaxKind.ImplicitArrayCreationExpression,
         SyntaxKind.LogicalOrExpression,
         SyntaxKind.ThisConstructorInitializer,
         SyntaxKind.ThisExpression,
         SyntaxKind.ElementAccessExpression,
         SyntaxKind.MemberBindingExpression,
         SyntaxKind.DescendingOrdering,
         SyntaxKind.UnaryMinusExpression,
         SyntaxKind.GreaterThanExpression,
         SyntaxKind.ArrayCreationExpression,
         SyntaxKind.CoalesceExpression,
         SyntaxKind.NullLiteralExpression,
         SyntaxKind.CharacterLiteralExpression,
         SyntaxKind.FalseLiteralExpression}},
      {OperationKind.CatchClause, new HashSet<SyntaxKind> {
         SyntaxKind.CatchClause}},
      {OperationKind.SwitchCase, new HashSet<SyntaxKind> {
         SyntaxKind.SwitchSection}},
      {OperationKind.CaseClause, new HashSet<SyntaxKind> {
         SyntaxKind.CaseSwitchLabel,
         SyntaxKind.DefaultSwitchLabel}},
      {OperationKind.InterpolatedStringText, new HashSet<SyntaxKind> {
         SyntaxKind.InterpolatedStringText}},
      {OperationKind.Interpolation, new HashSet<SyntaxKind> {
         SyntaxKind.Interpolation}},
      {OperationKind.ConstantPattern, new HashSet<SyntaxKind> {
         SyntaxKind.CaseSwitchLabel}},
      {OperationKind.DeclarationPattern, new HashSet<SyntaxKind> {
         }},
      {OperationKind.TupleBinaryOperator, new HashSet<SyntaxKind> {
         }},
      {OperationKind.MethodBody, new HashSet<SyntaxKind> {
         SyntaxKind.MethodDeclaration,
         SyntaxKind.GetAccessorDeclaration,
         SyntaxKind.SetAccessorDeclaration,
         SyntaxKind.OperatorDeclaration,
         SyntaxKind.DestructorDeclaration,
         SyntaxKind.AddAccessorDeclaration,
         SyntaxKind.RemoveAccessorDeclaration,
         SyntaxKind.ConversionOperatorDeclaration}},
      {OperationKind.ConstructorBody, new HashSet<SyntaxKind> {
         SyntaxKind.ConstructorDeclaration}},
      {OperationKind.Discard, new HashSet<SyntaxKind> {
         }},
      {OperationKind.FlowCapture, new HashSet<SyntaxKind> {
         }},
      {OperationKind.FlowCaptureReference, new HashSet<SyntaxKind> {
         }},
      {OperationKind.IsNull, new HashSet<SyntaxKind> {
         }},
      {OperationKind.CaughtException, new HashSet<SyntaxKind> {
         }},
      {OperationKind.StaticLocalInitializationSemaphore, new HashSet<SyntaxKind> {
         }},
      {OperationKind.FlowAnonymousFunction, new HashSet<SyntaxKind> {
         }},
      {OperationKind.CoalesceAssignment, new HashSet<SyntaxKind> {
         }},
      {OperationKind.Range, new HashSet<SyntaxKind> {
         }},
      {OperationKind.ReDim, new HashSet<SyntaxKind> {
         }},
      {OperationKind.ReDimClause, new HashSet<SyntaxKind> {
         }},
      {OperationKind.RecursivePattern, new HashSet<SyntaxKind> {
         }},
      {OperationKind.DiscardPattern, new HashSet<SyntaxKind> {
         }},
      {OperationKind.SwitchExpression, new HashSet<SyntaxKind> {
         }},
      {OperationKind.SwitchExpressionArm, new HashSet<SyntaxKind> {
         }},
      {OperationKind.PropertySubpattern, new HashSet<SyntaxKind> {
         }},
      },
      new Dictionary<OperationKind, HashSet<SyntaxKind>> {
         {OperationKind.None, new HashSet<SyntaxKind> {
         SyntaxKind.Attribute,
         SyntaxKind.SimpleMemberAccessExpression,
         SyntaxKind.PredefinedType,
         SyntaxKind.IdentifierName}},
      {OperationKind.Invalid, new HashSet<SyntaxKind> {
         SyntaxKind.SimpleMemberAccessExpression,
         SyntaxKind.InvocationExpression,
         SyntaxKind.IdentifierName,
         SyntaxKind.ObjectCreationExpression,
         SyntaxKind.ElementAccessExpression}},
      {OperationKind.Block, new HashSet<SyntaxKind> {
         SyntaxKind.Block,
         SyntaxKind.IdentifierName,
         SyntaxKind.ObjectCreationExpression,
         SyntaxKind.InvocationExpression,
         SyntaxKind.SimpleMemberAccessExpression,
         SyntaxKind.EqualsExpression,
         SyntaxKind.IsExpression,
         SyntaxKind.LogicalAndExpression,
         SyntaxKind.NotEqualsExpression,
         SyntaxKind.AsExpression,
         SyntaxKind.SimpleAssignmentExpression,
         SyntaxKind.LessThanExpression,
         SyntaxKind.ParenthesizedExpression,
         SyntaxKind.LogicalOrExpression,
         SyntaxKind.GreaterThanExpression,
         SyntaxKind.AddAssignmentExpression,
         SyntaxKind.ConditionalExpression,
         SyntaxKind.LessThanOrEqualExpression,
         SyntaxKind.CastExpression,
         SyntaxKind.LogicalNotExpression,
         SyntaxKind.AnonymousObjectCreationExpression,
         SyntaxKind.JoinClause,
         SyntaxKind.FromClause,
         SyntaxKind.AddExpression,
         SyntaxKind.GreaterThanOrEqualExpression,
         SyntaxKind.ArrayCreationExpression,
         SyntaxKind.CoalesceExpression,
         SyntaxKind.ElementAccessExpression,
         SyntaxKind.MultiplyExpression,
         SyntaxKind.TrueLiteralExpression,
         SyntaxKind.NullLiteralExpression,
         SyntaxKind.BitwiseAndExpression,
         SyntaxKind.StringLiteralExpression}},
      {OperationKind.VariableDeclarationGroup, new HashSet<SyntaxKind> {
         SyntaxKind.LocalDeclarationStatement,
         SyntaxKind.VariableDeclaration}},
      {OperationKind.Switch, new HashSet<SyntaxKind> {
         SyntaxKind.SwitchStatement}},
      {OperationKind.Loop, new HashSet<SyntaxKind> {
         SyntaxKind.ForEachStatement,
         SyntaxKind.ForStatement,
         SyntaxKind.DoStatement,
         SyntaxKind.WhileStatement}},
      {OperationKind.Labeled, new HashSet<SyntaxKind> {
         }},
      {OperationKind.Branch, new HashSet<SyntaxKind> {
         SyntaxKind.ContinueStatement,
         SyntaxKind.BreakStatement,
         SyntaxKind.GotoCaseStatement}},
      {OperationKind.Empty, new HashSet<SyntaxKind> {
         SyntaxKind.EmptyStatement}},
      {OperationKind.Return, new HashSet<SyntaxKind> {
         SyntaxKind.ReturnStatement,
         SyntaxKind.IdentifierName,
         SyntaxKind.ObjectCreationExpression,
         SyntaxKind.InvocationExpression,
         SyntaxKind.SimpleMemberAccessExpression,
         SyntaxKind.EqualsExpression,
         SyntaxKind.IsExpression,
         SyntaxKind.LogicalAndExpression,
         SyntaxKind.Block,
         SyntaxKind.NotEqualsExpression,
         SyntaxKind.AsExpression,
         SyntaxKind.SimpleAssignmentExpression,
         SyntaxKind.LessThanExpression,
         SyntaxKind.ParenthesizedExpression,
         SyntaxKind.LogicalOrExpression,
         SyntaxKind.GreaterThanExpression,
         SyntaxKind.AddAssignmentExpression,
         SyntaxKind.ConditionalExpression,
         SyntaxKind.LessThanOrEqualExpression,
         SyntaxKind.CastExpression,
         SyntaxKind.LogicalNotExpression,
         SyntaxKind.AnonymousObjectCreationExpression,
         SyntaxKind.JoinClause,
         SyntaxKind.FromClause,
         SyntaxKind.AddExpression,
         SyntaxKind.GreaterThanOrEqualExpression,
         SyntaxKind.ArrayCreationExpression,
         SyntaxKind.CoalesceExpression,
         SyntaxKind.ElementAccessExpression,
         SyntaxKind.MultiplyExpression,
         SyntaxKind.TrueLiteralExpression,
         SyntaxKind.NullLiteralExpression,
         SyntaxKind.BitwiseAndExpression,
         SyntaxKind.StringLiteralExpression}},
      {OperationKind.YieldBreak, new HashSet<SyntaxKind> {
         SyntaxKind.YieldBreakStatement}},
      {OperationKind.Lock, new HashSet<SyntaxKind> {
         SyntaxKind.LockStatement}},
      {OperationKind.Try, new HashSet<SyntaxKind> {
         SyntaxKind.TryStatement}},
      {OperationKind.Using, new HashSet<SyntaxKind> {
         SyntaxKind.UsingStatement}},
      {OperationKind.YieldReturn, new HashSet<SyntaxKind> {
         SyntaxKind.YieldReturnStatement}},
      {OperationKind.ExpressionStatement, new HashSet<SyntaxKind> {
         SyntaxKind.ExpressionStatement,
         SyntaxKind.InvocationExpression,
         SyntaxKind.SimpleAssignmentExpression,
         SyntaxKind.PostIncrementExpression,
         SyntaxKind.AddAssignmentExpression,
         SyntaxKind.PreIncrementExpression,
         SyntaxKind.PostDecrementExpression}},
      {OperationKind.LocalFunction, new HashSet<SyntaxKind> {
         }},
      {OperationKind.Stop, new HashSet<SyntaxKind> {
         }},
      {OperationKind.End, new HashSet<SyntaxKind> {
         }},
      {OperationKind.RaiseEvent, new HashSet<SyntaxKind> {
         }},
      {OperationKind.Literal, new HashSet<SyntaxKind> {
         SyntaxKind.NumericLiteralExpression,
         SyntaxKind.StringLiteralExpression,
         SyntaxKind.FalseLiteralExpression,
         SyntaxKind.NullLiteralExpression,
         SyntaxKind.CharacterLiteralExpression,
         SyntaxKind.TrueLiteralExpression,
         SyntaxKind.ArrayCreationExpression,
         SyntaxKind.InvocationExpression,
         SyntaxKind.ImplicitArrayCreationExpression,
         SyntaxKind.ObjectCreationExpression,
         SyntaxKind.ArrayInitializerExpression,
         SyntaxKind.ThisConstructorInitializer,
         SyntaxKind.BaseConstructorInitializer}},
      {OperationKind.Conversion, new HashSet<SyntaxKind> {
         SyntaxKind.NullLiteralExpression,
         SyntaxKind.ObjectCreationExpression,
         SyntaxKind.InvocationExpression,
         SyntaxKind.SimpleMemberAccessExpression,
         SyntaxKind.DefaultExpression,
         SyntaxKind.IdentifierName,
         SyntaxKind.CastExpression,
         SyntaxKind.AsExpression,
         SyntaxKind.StringLiteralExpression,
         SyntaxKind.ConditionalExpression,
         SyntaxKind.ElementAccessExpression,
         SyntaxKind.SimpleAssignmentExpression,
         SyntaxKind.NumericLiteralExpression,
         SyntaxKind.ThisExpression,
         SyntaxKind.FalseLiteralExpression,
         SyntaxKind.CharacterLiteralExpression,
         SyntaxKind.PostIncrementExpression,
         SyntaxKind.MultiplyExpression,
         SyntaxKind.ParenthesizedLambdaExpression,
         SyntaxKind.FromClause,
         SyntaxKind.LogicalAndExpression,
         SyntaxKind.SimpleLambdaExpression,
         SyntaxKind.ArrayCreationExpression,
         SyntaxKind.LogicalOrExpression,
         SyntaxKind.NotEqualsExpression,
         SyntaxKind.LogicalNotExpression,
         SyntaxKind.LessThanExpression,
         SyntaxKind.EqualsExpression,
         SyntaxKind.GreaterThanExpression,
         SyntaxKind.QueryExpression,
         SyntaxKind.LessThanOrEqualExpression,
         SyntaxKind.OrderByClause,
         SyntaxKind.ParenthesizedExpression,
         SyntaxKind.AnonymousObjectCreationExpression,
         SyntaxKind.JoinClause,
         SyntaxKind.TrueLiteralExpression,
         SyntaxKind.UnaryMinusExpression,
         SyntaxKind.GreaterThanOrEqualExpression,
         SyntaxKind.AddExpression,
         SyntaxKind.CoalesceExpression,
         SyntaxKind.SubtractExpression,
         SyntaxKind.PreIncrementExpression,
         SyntaxKind.DivideExpression,
         SyntaxKind.ImplicitArrayCreationExpression,
         SyntaxKind.IsExpression,
         SyntaxKind.PostDecrementExpression,
         SyntaxKind.BitwiseOrExpression,
         SyntaxKind.TypeOfExpression}},
      {OperationKind.Invocation, new HashSet<SyntaxKind> {
         SyntaxKind.InvocationExpression,
         SyntaxKind.ThisConstructorInitializer,
         SyntaxKind.BaseConstructorInitializer,
         SyntaxKind.StringLiteralExpression,
         SyntaxKind.WhereClause,
         SyntaxKind.FromClause,
         SyntaxKind.ObjectCreationExpression,
         SyntaxKind.ComplexElementInitializerExpression,
         SyntaxKind.SimpleMemberAccessExpression,
         SyntaxKind.CastExpression,
         SyntaxKind.SelectClause,
         SyntaxKind.LetClause,
         SyntaxKind.DescendingOrdering,
         SyntaxKind.JoinClause,
         SyntaxKind.AscendingOrdering,
         SyntaxKind.GroupClause,
         SyntaxKind.ElementAccessExpression,
         SyntaxKind.NumericLiteralExpression,
         SyntaxKind.TypeOfExpression,
         SyntaxKind.IdentifierName,
         SyntaxKind.ConditionalExpression,
         SyntaxKind.AnonymousObjectCreationExpression}},
      {OperationKind.ArrayElementReference, new HashSet<SyntaxKind> {
         SyntaxKind.ElementAccessExpression}},
      {OperationKind.LocalReference, new HashSet<SyntaxKind> {
         SyntaxKind.IdentifierName}},
      {OperationKind.ParameterReference, new HashSet<SyntaxKind> {
         SyntaxKind.IdentifierName,
         SyntaxKind.LetClause,
         SyntaxKind.JoinClause,
         SyntaxKind.FromClause}},
      {OperationKind.FieldReference, new HashSet<SyntaxKind> {
         SyntaxKind.SimpleMemberAccessExpression,
         SyntaxKind.IdentifierName}},
      {OperationKind.MethodReference, new HashSet<SyntaxKind> {
         SyntaxKind.IdentifierName,
         SyntaxKind.SimpleMemberAccessExpression}},
      {OperationKind.PropertyReference, new HashSet<SyntaxKind> {
         SyntaxKind.IdentifierName,
         SyntaxKind.SimpleMemberAccessExpression,
         SyntaxKind.ElementAccessExpression,
         SyntaxKind.LetClause,
         SyntaxKind.InvocationExpression,
         SyntaxKind.JoinClause,
         SyntaxKind.FromClause}},
      {OperationKind.EventReference, new HashSet<SyntaxKind> {
         SyntaxKind.SimpleMemberAccessExpression,
         SyntaxKind.IdentifierName}},
      {OperationKind.UnaryOperator, new HashSet<SyntaxKind> {
         SyntaxKind.LogicalNotExpression,
         SyntaxKind.UnaryMinusExpression,
         SyntaxKind.BitwiseNotExpression}},
      {OperationKind.BinaryOperator, new HashSet<SyntaxKind> {
         SyntaxKind.EqualsExpression,
         SyntaxKind.BitwiseOrExpression,
         SyntaxKind.GreaterThanOrEqualExpression,
         SyntaxKind.AddExpression,
         SyntaxKind.MultiplyExpression,
         SyntaxKind.LogicalAndExpression,
         SyntaxKind.NotEqualsExpression,
         SyntaxKind.GreaterThanExpression,
         SyntaxKind.LogicalOrExpression,
         SyntaxKind.LessThanExpression,
         SyntaxKind.LessThanOrEqualExpression,
         SyntaxKind.SubtractExpression,
         SyntaxKind.LeftShiftExpression,
         SyntaxKind.RightShiftExpression,
         SyntaxKind.DivideExpression,
         SyntaxKind.ModuloExpression,
         SyntaxKind.ExclusiveOrExpression,
         SyntaxKind.BitwiseAndExpression}},
      {OperationKind.Conditional, new HashSet<SyntaxKind> {
         SyntaxKind.IfStatement,
         SyntaxKind.ConditionalExpression}},
      {OperationKind.Coalesce, new HashSet<SyntaxKind> {
         SyntaxKind.CoalesceExpression}},
      {OperationKind.AnonymousFunction, new HashSet<SyntaxKind> {
         SyntaxKind.SimpleLambdaExpression,
         SyntaxKind.ParenthesizedLambdaExpression,
         SyntaxKind.AnonymousMethodExpression,
         SyntaxKind.LogicalOrExpression,
         SyntaxKind.LogicalAndExpression,
         SyntaxKind.InvocationExpression,
         SyntaxKind.NotEqualsExpression,
         SyntaxKind.AnonymousObjectCreationExpression,
         SyntaxKind.EqualsExpression,
         SyntaxKind.IdentifierName,
         SyntaxKind.LogicalNotExpression,
         SyntaxKind.SimpleMemberAccessExpression,
         SyntaxKind.LessThanOrEqualExpression,
         SyntaxKind.JoinClause,
         SyntaxKind.FromClause,
         SyntaxKind.ParenthesizedExpression,
         SyntaxKind.AsExpression,
         SyntaxKind.ObjectCreationExpression,
         SyntaxKind.GreaterThanOrEqualExpression,
         SyntaxKind.ConditionalExpression,
         SyntaxKind.ArrayCreationExpression,
         SyntaxKind.CastExpression,
         SyntaxKind.ElementAccessExpression}},
      {OperationKind.ObjectCreation, new HashSet<SyntaxKind> {
         SyntaxKind.ObjectCreationExpression}},
      {OperationKind.TypeParameterObjectCreation, new HashSet<SyntaxKind> {
         SyntaxKind.ObjectCreationExpression}},
      {OperationKind.ArrayCreation, new HashSet<SyntaxKind> {
         SyntaxKind.ArrayCreationExpression,
         SyntaxKind.InvocationExpression,
         SyntaxKind.ImplicitArrayCreationExpression,
         SyntaxKind.ArrayInitializerExpression,
         SyntaxKind.ObjectCreationExpression}},
      {OperationKind.InstanceReference, new HashSet<SyntaxKind> {
         SyntaxKind.ThisExpression,
         SyntaxKind.IdentifierName,
         SyntaxKind.BaseExpression,
         SyntaxKind.GenericName,
         SyntaxKind.ThisConstructorInitializer,
         SyntaxKind.BaseConstructorInitializer,
         SyntaxKind.QualifiedName,
         SyntaxKind.LetClause,
         SyntaxKind.AnonymousObjectCreationExpression,
         SyntaxKind.JoinClause,
         SyntaxKind.FromClause}},
      {OperationKind.IsType, new HashSet<SyntaxKind> {
         SyntaxKind.IsExpression}},
      {OperationKind.Await, new HashSet<SyntaxKind> {
         SyntaxKind.AwaitExpression}},
      {OperationKind.SimpleAssignment, new HashSet<SyntaxKind> {
         SyntaxKind.AttributeArgument,
         SyntaxKind.SimpleAssignmentExpression,
         SyntaxKind.QueryBody,
         SyntaxKind.LetClause,
         SyntaxKind.AnonymousObjectMemberDeclarator}},
      {OperationKind.CompoundAssignment, new HashSet<SyntaxKind> {
         SyntaxKind.AddAssignmentExpression,
         SyntaxKind.DivideAssignmentExpression,
         SyntaxKind.MultiplyAssignmentExpression,
         SyntaxKind.OrAssignmentExpression,
         SyntaxKind.SubtractAssignmentExpression,
         SyntaxKind.AndAssignmentExpression,
         SyntaxKind.ExclusiveOrAssignmentExpression}},
      {OperationKind.Parenthesized, new HashSet<SyntaxKind> {
         }},
      {OperationKind.EventAssignment, new HashSet<SyntaxKind> {
         SyntaxKind.AddAssignmentExpression,
         SyntaxKind.SubtractAssignmentExpression}},
      {OperationKind.ConditionalAccess, new HashSet<SyntaxKind> {
         }},
      {OperationKind.ConditionalAccessInstance, new HashSet<SyntaxKind> {
         }},
      {OperationKind.InterpolatedString, new HashSet<SyntaxKind> {
         }},
      {OperationKind.AnonymousObjectCreation, new HashSet<SyntaxKind> {
         SyntaxKind.LetClause,
         SyntaxKind.AnonymousObjectCreationExpression,
         SyntaxKind.JoinClause,
         SyntaxKind.FromClause}},
      {OperationKind.ObjectOrCollectionInitializer, new HashSet<SyntaxKind> {
         SyntaxKind.ObjectInitializerExpression,
         SyntaxKind.CollectionInitializerExpression}},
      {OperationKind.MemberInitializer, new HashSet<SyntaxKind> {
         }},
      {OperationKind.NameOf, new HashSet<SyntaxKind> {
         }},
      {OperationKind.Tuple, new HashSet<SyntaxKind> {
         }},
      {OperationKind.DynamicObjectCreation, new HashSet<SyntaxKind> {
         }},
      {OperationKind.DynamicMemberReference, new HashSet<SyntaxKind> {
         SyntaxKind.SimpleMemberAccessExpression}},
      {OperationKind.DynamicInvocation, new HashSet<SyntaxKind> {
         SyntaxKind.InvocationExpression}},
      {OperationKind.DynamicIndexerAccess, new HashSet<SyntaxKind> {
         }},
      {OperationKind.TranslatedQuery, new HashSet<SyntaxKind> {
         SyntaxKind.QueryExpression}},
      {OperationKind.DelegateCreation, new HashSet<SyntaxKind> {
         SyntaxKind.SimpleLambdaExpression,
         SyntaxKind.ObjectCreationExpression,
         SyntaxKind.ParenthesizedLambdaExpression,
         SyntaxKind.AnonymousMethodExpression,
         SyntaxKind.IdentifierName,
         SyntaxKind.SimpleMemberAccessExpression,
         SyntaxKind.LogicalOrExpression,
         SyntaxKind.LogicalAndExpression,
         SyntaxKind.InvocationExpression,
         SyntaxKind.NotEqualsExpression,
         SyntaxKind.AnonymousObjectCreationExpression,
         SyntaxKind.JoinClause,
         SyntaxKind.EqualsExpression,
         SyntaxKind.FromClause,
         SyntaxKind.ParenthesizedExpression,
         SyntaxKind.LogicalNotExpression,
         SyntaxKind.CastExpression,
         SyntaxKind.LessThanOrEqualExpression,
         SyntaxKind.ElementAccessExpression,
         SyntaxKind.ConditionalExpression}},
      {OperationKind.DefaultValue, new HashSet<SyntaxKind> {
         SyntaxKind.DefaultExpression,
         SyntaxKind.InvocationExpression,
         SyntaxKind.ObjectCreationExpression,
         SyntaxKind.BaseConstructorInitializer}},
      {OperationKind.TypeOf, new HashSet<SyntaxKind> {
         SyntaxKind.TypeOfExpression}},
      {OperationKind.SizeOf, new HashSet<SyntaxKind> {
         }},
      {OperationKind.AddressOf, new HashSet<SyntaxKind> {
         }},
      {OperationKind.IsPattern, new HashSet<SyntaxKind> {
         }},
      {OperationKind.Increment, new HashSet<SyntaxKind> {
         SyntaxKind.PostIncrementExpression,
         SyntaxKind.PreIncrementExpression}},
      {OperationKind.Throw, new HashSet<SyntaxKind> {
         SyntaxKind.ThrowStatement}},
      {OperationKind.Decrement, new HashSet<SyntaxKind> {
         SyntaxKind.PostDecrementExpression,
         SyntaxKind.PreDecrementExpression}},
      {OperationKind.DeconstructionAssignment, new HashSet<SyntaxKind> {
         }},
      {OperationKind.DeclarationExpression, new HashSet<SyntaxKind> {
         }},
      {OperationKind.OmittedArgument, new HashSet<SyntaxKind> {
         }},
      {OperationKind.FieldInitializer, new HashSet<SyntaxKind> {
         SyntaxKind.EqualsValueClause}},
      {OperationKind.VariableInitializer, new HashSet<SyntaxKind> {
         SyntaxKind.EqualsValueClause}},
      {OperationKind.PropertyInitializer, new HashSet<SyntaxKind> {
         }},
      {OperationKind.ParameterInitializer, new HashSet<SyntaxKind> {
         SyntaxKind.EqualsValueClause}},
      {OperationKind.ArrayInitializer, new HashSet<SyntaxKind> {
         SyntaxKind.ArrayInitializerExpression,
         SyntaxKind.InvocationExpression,
         SyntaxKind.ObjectCreationExpression}},
      {OperationKind.VariableDeclarator, new HashSet<SyntaxKind> {
         SyntaxKind.VariableDeclarator,
         SyntaxKind.PredefinedType,
         SyntaxKind.IdentifierName,
         SyntaxKind.CatchDeclaration,
         SyntaxKind.QualifiedName,
         SyntaxKind.GenericName,
         SyntaxKind.NullableType,
         SyntaxKind.ArrayType}},
      {OperationKind.VariableDeclaration, new HashSet<SyntaxKind> {
         SyntaxKind.VariableDeclaration}},
      {OperationKind.Argument, new HashSet<SyntaxKind> {
         SyntaxKind.Argument,
         SyntaxKind.IdentifierName,
         SyntaxKind.InvocationExpression,
         SyntaxKind.CastExpression,
         SyntaxKind.ElementAccessExpression,
         SyntaxKind.SimpleMemberAccessExpression,
         SyntaxKind.ObjectCreationExpression,
         SyntaxKind.ThisExpression,
         SyntaxKind.StringLiteralExpression,
         SyntaxKind.FromClause,
         SyntaxKind.LogicalOrExpression,
         SyntaxKind.LogicalAndExpression,
         SyntaxKind.AddExpression,
         SyntaxKind.SubtractExpression,
         SyntaxKind.NumericLiteralExpression,
         SyntaxKind.WhereClause,
         SyntaxKind.LetClause,
         SyntaxKind.NotEqualsExpression,
         SyntaxKind.AnonymousObjectCreationExpression,
         SyntaxKind.QueryExpression,
         SyntaxKind.EqualsExpression,
         SyntaxKind.LogicalNotExpression,
         SyntaxKind.LessThanOrEqualExpression,
         SyntaxKind.JoinClause,
         SyntaxKind.OrderByClause,
         SyntaxKind.ParenthesizedExpression,
         SyntaxKind.AscendingOrdering,
         SyntaxKind.AsExpression,
         SyntaxKind.GreaterThanOrEqualExpression,
         SyntaxKind.GroupClause,
         SyntaxKind.ConditionalExpression,
         SyntaxKind.ArrayCreationExpression,
         SyntaxKind.NullLiteralExpression,
         SyntaxKind.MultiplyExpression,
         SyntaxKind.TypeOfExpression,
         SyntaxKind.CoalesceExpression,
         SyntaxKind.DescendingOrdering,
         SyntaxKind.ThisConstructorInitializer,
         SyntaxKind.BaseConstructorInitializer,
         SyntaxKind.DivideExpression,
         SyntaxKind.ImplicitArrayCreationExpression,
         SyntaxKind.FalseLiteralExpression,
         SyntaxKind.TrueLiteralExpression,
         SyntaxKind.SimpleLambdaExpression}},
      {OperationKind.CatchClause, new HashSet<SyntaxKind> {
         SyntaxKind.CatchClause}},
      {OperationKind.SwitchCase, new HashSet<SyntaxKind> {
         SyntaxKind.SwitchSection}},
      {OperationKind.CaseClause, new HashSet<SyntaxKind> {
         SyntaxKind.CaseSwitchLabel,
         SyntaxKind.DefaultSwitchLabel}},
      {OperationKind.InterpolatedStringText, new HashSet<SyntaxKind> {
         }},
      {OperationKind.Interpolation, new HashSet<SyntaxKind> {
         }},
      {OperationKind.ConstantPattern, new HashSet<SyntaxKind> {
         SyntaxKind.CaseSwitchLabel}},
      {OperationKind.DeclarationPattern, new HashSet<SyntaxKind> {
         }},
      {OperationKind.TupleBinaryOperator, new HashSet<SyntaxKind> {
         }},
      {OperationKind.MethodBody, new HashSet<SyntaxKind> {
         SyntaxKind.GetAccessorDeclaration,
         SyntaxKind.MethodDeclaration,
         SyntaxKind.ConversionOperatorDeclaration,
         SyntaxKind.SetAccessorDeclaration,
         SyntaxKind.OperatorDeclaration,
         SyntaxKind.RemoveAccessorDeclaration,
         SyntaxKind.AddAccessorDeclaration}},
      {OperationKind.ConstructorBody, new HashSet<SyntaxKind> {
         SyntaxKind.ConstructorDeclaration}},
      {OperationKind.Discard, new HashSet<SyntaxKind> {
         }},
      {OperationKind.FlowCapture, new HashSet<SyntaxKind> {
         }},
      {OperationKind.FlowCaptureReference, new HashSet<SyntaxKind> {
         }},
      {OperationKind.IsNull, new HashSet<SyntaxKind> {
         }},
      {OperationKind.CaughtException, new HashSet<SyntaxKind> {
         }},
      {OperationKind.StaticLocalInitializationSemaphore, new HashSet<SyntaxKind> {
         }},
      {OperationKind.FlowAnonymousFunction, new HashSet<SyntaxKind> {
         }},
      {OperationKind.CoalesceAssignment, new HashSet<SyntaxKind> {
         }},
      {OperationKind.Range, new HashSet<SyntaxKind> {
         }},
      {OperationKind.ReDim, new HashSet<SyntaxKind> {
         }},
      {OperationKind.ReDimClause, new HashSet<SyntaxKind> {
         }},
      {OperationKind.RecursivePattern, new HashSet<SyntaxKind> {
         }},
      {OperationKind.DiscardPattern, new HashSet<SyntaxKind> {
         }},
      {OperationKind.SwitchExpression, new HashSet<SyntaxKind> {
         }},
      {OperationKind.SwitchExpressionArm, new HashSet<SyntaxKind> {
         }},
      {OperationKind.PropertySubpattern, new HashSet<SyntaxKind> {
         }},
      }         
         
      };
      private void PrintOperationKindToSyntaxKindDictionary()
      {
         Dictionary<OperationKind, HashSet<SyntaxKind>> operationKindToSyntaxKind = new Dictionary<OperationKind, HashSet<SyntaxKind>>();
         foreach (var operationKind in OperationsKinds) {
            operationKindToSyntaxKind[operationKind] = new HashSet<SyntaxKind>();
         }

         foreach (var operationKindToSyntaxKinds in OperationToSyntaxKinds)
         {
            foreach (var operationKindDetails in operationKindToSyntaxKinds)
            {
               operationKindToSyntaxKind[operationKindDetails.Key].UnionWith(operationKindDetails.Value);
            }
         }

         foreach (var operationKindDetails in operationKindToSyntaxKind)
         {
            Console.WriteLine("{OperationKind." + operationKindDetails.Key + ", new HashSet<SyntaxKind> {");
            foreach (var syntaxKind in operationKindDetails.Value)
            {
               Console.WriteLine("   SyntaxKind.{0},", syntaxKind);
            }
            Console.WriteLine("}},");
         }
      }
#endif
   }

}
