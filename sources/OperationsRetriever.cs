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

   public interface IOpProcessor
   {
      SyntaxKind[] Kinds(CompilationStartAnalysisContext context);
      void HandleSemanticModelOps(SemanticModelAnalysisContext context,
         Dictionary<OperationKind, List<IOperation>> ops);
   }

   public abstract class OperationsRetriever : AbstractRuleChecker, IOpProcessor
   {

      public abstract SyntaxKind[] Kinds(CompilationStartAnalysisContext context);
      public abstract void HandleSemanticModelOps(SemanticModelAnalysisContext context,
         Dictionary<OperationKind, List<IOperation>> ops);


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

      private class OperationDetails
      {
         public IOperation Operation { get; private set; }
         public ControlFlowGraph ControlFlowGraph { get; private set; }
         public ISymbol ContainingSymbol { get; private set; }
         public OperationDetails(IOperation operation, ControlFlowGraph controlFlowGraph, ISymbol containingSymbol)
         {
            Operation = operation;
            ControlFlowGraph = ControlFlowGraph;
            ContainingSymbol = containingSymbol;
         }
      }

      private class SubscriberSink
      {
         private static SubscriberSink ObjSubscriberSink = new SubscriberSink();

         private HashSet<SyntaxKind> _kinds = new HashSet<SyntaxKind>();
         private ConcurrentDictionary<string, ConcurrentQueue<IOperation>> _operations =
            new ConcurrentDictionary<string, ConcurrentQueue<IOperation>>();
         private HashSet<OperationKind> _opKinds = new HashSet<OperationKind>();
         private Compilation CurrentCompilation { get; set; }

         public ILog Log;
         internal static object Lock = new object();

         private List<PerfDatumOps> _perfDataOps = new List<PerfDatumOps>();
         private List<PerfDatumOps> _perfDataAssembly = new List<PerfDatumOps>();

         public HashSet<OpsProcessor> OpsProcessors { get; private set; }
         private SubscriberSink()
         {
            OpsProcessors = new HashSet<OpsProcessor>();
         }

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


         public void CompiliationStart(CompilationStartAnalysisContext context)
         {
            //var watch = new System.Diagnostics.Stopwatch();
            //watch.Start();
            lock (Lock) {
               try {
                  //Log.WarnFormat("Assembly: {0}", context.Compilation.Assembly.Name);
                  if (null == CurrentCompilation || CurrentCompilation.Assembly != context.Compilation.Assembly) {
                     CurrentCompilation = context.Compilation;
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
            //lock (_perfDataAssembly) {
            //   var assemblyPerf = _perfDataAssembly.FirstOrDefault(p => p.FilePath == CurrentCompilation.Assembly.Name);
            //   if (null != assemblyPerf) {
            //      _perfDataAssembly.Remove(assemblyPerf);
            //      assemblyPerf = new PerfDatumOps(CurrentCompilation.Assembly.Name, assemblyPerf.Time + watch.ElapsedTicks, assemblyPerf.Ops + 1);
            //   } else {
            //      assemblyPerf = new PerfDatumOps(CurrentCompilation.Assembly.Name, watch.ElapsedTicks, 1);
            //   }
            //   _perfDataAssembly.Add(assemblyPerf);
            //}
         }

         private void OnOperation(OperationAnalysisContext context)
         {
            
            var q = _operations.GetOrAdd(context.Operation.Syntax.SyntaxTree.FilePath, (key) => new ConcurrentQueue<IOperation>());
            q.Enqueue(context.Operation);
         }

         private void OnSemanticModelAnalysisEnd(SemanticModelAnalysisContext context)
         {
            try {
               //var watch = new System.Diagnostics.Stopwatch();
               //watch.Start();
               //Console.WriteLine("Starting Semantic Model Analysis For: {0}", context.SemanticModel.SyntaxTree.FilePath);
               var nodes =
                     context.SemanticModel.SyntaxTree.GetRoot().DescendantNodes().Where(n => _kinds.Contains(n.Kind())).ToHashSet();
               //int nodeCount = nodes.Count;

               if (nodes.Any()) {

                  //Console.WriteLine("Have nodes, count: {0}", nodes.Count);
                  Dictionary<OperationKind, List<IOperation>> ops =
                     new Dictionary<OperationKind, List<IOperation>>();
                  foreach (var opKind in _opKinds) {
                     ops[opKind] = new List<IOperation>(25);
                  }

                  ConcurrentQueue<IOperation> operations = null;
                  IOperation operation = null;

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
                        ops[operation.Kind].Add(operation);
                        nodes.Remove(operation.Syntax);
                        if (!nodes.Any()) {
                           break;
                        }
                     }
                     if (null == operation) {
                        //Log.Warn("operation was null; trying again! File: " + context.SemanticModel.SyntaxTree.FilePath);
                        continue;
                     }
                     if (/*OperationKind.End == operation.Kind ||*/ !nodes.Any()) {
                        break;
                     }
                  }

                  List<Task> handlerTasks = new List<Task>();

                  foreach (var opsProcessor in OpsProcessors) {
                     if (opsProcessor.IsActive) {
                        handlerTasks.Add(Task.Run(() => opsProcessor.OpProcessor.HandleSemanticModelOps(context, ops)));
                     }
                  }
                  Task.WaitAll(handlerTasks.ToArray());
                  //Log.WarnFormat("Operation Null Count: {0} File {1}", operationNullCount, context.SemanticModel.SyntaxTree.FilePath);
               }
               //watch.Stop();
               
               
               //lock (_perfDataOps) {
               //   _perfDataOps.Add(new PerfDatumOps(context.SemanticModel.SyntaxTree.FilePath, watch.ElapsedTicks, nodeCount));
               //}
            } catch (Exception e) {
               Log.Warn("Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath, e);
            }
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
   }
}
