using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Roslyn.DotNet.CastDotNetExtension;
using Roslyn.DotNet.Common;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Operations;


namespace CastDotNetExtension
{
   [CastRuleChecker]
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   [RuleDescription(
       Id = "TODO: Add prefix_RecursionShouldNotBeInfinite",
       Title = "TODO: Add Title",
       MessageFormat = "TODO: Add Message",
       Category = "TODO: Add Category",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.RecursionShouldNotBeInfinite"
   )]
   public class RecursionShouldNotBeInfinite : AbstractOperationsAnalyzer, IOpProcessor
   {

      public RecursionShouldNotBeInfinite() {
      }

      public override SyntaxKind[] Kinds(CompilationStartAnalysisContext context)
      {
         return new SyntaxKind[] { SyntaxKind.InvocationExpression };
      }

      ~RecursionShouldNotBeInfinite()
      {
         long TotalTime = 0, TotalOps = 0, TotalProcessedOps = 0;
         Console.WriteLine("File,Time(ticks),Total Ops,Processed Ops");
         foreach (var filePerfData in PerfData) {
            TotalTime += filePerfData.Value.Time;
            TotalOps += filePerfData.Value.Ops;
            TotalProcessedOps += filePerfData.Value.ProcessedOps;
            Console.WriteLine("\"{0}\",{1},{2},{3}",
               filePerfData.Key, filePerfData.Value.Time, filePerfData.Value.Ops, filePerfData.Value.ProcessedOps);
            foreach (var methodDetails in filePerfData.Value.MethodToLine) {
               Console.WriteLine("   Recursive Call: {0}: {1}", methodDetails.Key, methodDetails.Value);
            }
         }
         Console.WriteLine("{0},{1},{2},{3}", "All", TotalTime / TimeSpan.TicksPerMillisecond, TotalOps, TotalProcessedOps);

         //Console.WriteLine(string.Join("\r\n", _methodKinds.ToHashSet()));
      }

      private class FilePerfData {
         public long Time {get;private set;}
         public int Ops {get;private set;}
         public int ProcessedOps { get; private set; }
         public int GetDeclaredSymbolsCalls { get; private set; }
         public Dictionary<string, int> MethodToLine {get;private set;}
         public FilePerfData(long time, int ops, int processedOps, int getDeclaredSymbolCalls, Dictionary<string, int> methodToLine)
         {
            Time = time;
            Ops = ops;
            ProcessedOps = processedOps;
            GetDeclaredSymbolsCalls = getDeclaredSymbolCalls;
            MethodToLine = methodToLine;
         }
      }

      private ConcurrentDictionary<string, FilePerfData> PerfData = 
         new ConcurrentDictionary<string, FilePerfData>();

      private ConcurrentQueue<MethodKind> _methodKinds = new ConcurrentQueue<MethodKind>();
      public override void HandleSemanticModelOps(SemanticModel semanticModel,
            IReadOnlyDictionary<OperationKind, IReadOnlyList<OperationDetails>> ops, bool lastBatch)
      {
         var watch = new System.Diagnostics.Stopwatch();
         watch.Start();
         var invocationOps = ops[OperationKind.Invocation];
         Dictionary<string, int> methodToRecursiveCallLine = new Dictionary<string, int>();
         int processedOps = 0;

         foreach (var op in invocationOps) {
            var invocationOp = op.Operation as IInvocationOperation;
            if (!methodToRecursiveCallLine.ContainsKey(invocationOp.TargetMethod.OriginalDefinition.ToString())) {
               processedOps++;
               foreach (var declSynRef in invocationOp.TargetMethod.DeclaringSyntaxReferences) {

                  //if (syntax.FullSpan.Contains(invocationOp.Syntax.FullSpan) && 
                  //   
                  //_methodKinds.Enqueue(invocationOp.TargetMethod.MethodKind);
                  var syntax = declSynRef.GetSyntax();
                  if (syntax.SyntaxTree.FilePath == invocationOp.Syntax.SyntaxTree.FilePath) {
                     //if (syntax.Contains(invocationOp.Syntax)) {
                     if (syntax.FullSpan.Contains(invocationOp.Syntax.FullSpan)) {
                        methodToRecursiveCallLine[invocationOp.TargetMethod.OriginalDefinition.ToString()] =
                           invocationOp.Syntax.GetLocation().GetMappedLineSpan().StartLinePosition.Line;
                        break;
                     }
                  }
               }
            }
         }

         watch.Stop();

         PerfData[semanticModel.SyntaxTree.FilePath] =
            new FilePerfData(watch.ElapsedTicks, invocationOps.Count(), processedOps, 0, methodToRecursiveCallLine);
      }
   }
}
