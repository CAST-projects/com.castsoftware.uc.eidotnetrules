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
using CastDotNetExtension.Utils;

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

      private HashSet<string> _recursiveMethods = new HashSet<string>();

      ~RecursionShouldNotBeInfinite()
      {
         
         long TotalTime = 0, TotalOps = 0, TotalProcessedOps = 0;
         Console.WriteLine("File,Time(ticks),Total Ops,Processed Ops");
         foreach (var filePerfDataQ in PerfData) {
            foreach (var filePerfData in filePerfDataQ.Value.ToArray()) {
               TotalTime += filePerfData.Time;
               TotalOps += filePerfData.Ops;
               TotalProcessedOps += filePerfData.ProcessedOps;
               Console.WriteLine("\"{0}\",{1},{2},{3}",
                  filePerfDataQ.Key, filePerfData.Time, filePerfData.Ops, filePerfData.ProcessedOps);
               foreach (var methodDetails in filePerfData.MethodToLine) {
                  Console.WriteLine("   Recursive Call: {0}: {1}", methodDetails.Key, methodDetails.Value);
               }
            }
         }
         Console.WriteLine("{0},{1},{2},{3}", "All", TotalTime / TimeSpan.TicksPerMillisecond, TotalOps, TotalProcessedOps);

         /*
         Console.WriteLine("Method Name,Call Count");
         foreach (var methodDetails in _methodToCallCount) {
            Console.WriteLine(methodDetails.Key + "," + methodDetails.Value);
         }

         Console.WriteLine("Method Kind,Count");
         foreach (var methodKindDetails in _methodKindToCount) {
            Console.WriteLine("{0},{1}", methodKindDetails.Key, methodKindDetails.Value);
         }
         */


         Console.WriteLine("Recursive Methods:\r\n{0}", string.Join("\r\n", _recursiveMethods));
      }

      private class FilePerfData {
         public long Time {get;private set;}
         public long Ops {get;private set;}
         public long ProcessedOps { get; private set; }
         public long GetDeclaredSymbolsCalls { get; private set; }
         public Dictionary<string, int> MethodToLine {get;private set;}
         public FilePerfData(long time, long ops, long processedOps, long getDeclaredSymbolCalls, Dictionary<string, int> methodToLine)
         {
            Time = time;
            Ops = ops;
            ProcessedOps = processedOps;
            GetDeclaredSymbolsCalls = getDeclaredSymbolCalls;
            MethodToLine = methodToLine;
         }
      }

      private ConcurrentDictionary<string, ConcurrentQueue<FilePerfData>> PerfData =
         new ConcurrentDictionary<string, ConcurrentQueue<FilePerfData>>();

      //private ConcurrentDictionary<string, int> _methodToCallCount =
      //   new ConcurrentDictionary<string, int>();

      //private ConcurrentDictionary<MethodKind, int> _methodKindToCount = new ConcurrentDictionary<MethodKind, int>();


      public override void HandleProjectOps(Compilation compilation, Dictionary<SemanticModel, Dictionary<OperationKind, IReadOnlyList<OperationDetails>>> allProjectOps)
      {
         try {
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            Dictionary<IMethodSymbol, Tuple<SyntaxNode, HashSet<SyntaxNode>>> methodsToAnalyze =
               new Dictionary<IMethodSymbol, Tuple<SyntaxNode, HashSet<SyntaxNode>>>();

            HashSet<IMethodSymbol> recursiveOnes = new HashSet<IMethodSymbol>();
            Dictionary<IMethodSymbol, SyntaxNode> methodToSyntax = new Dictionary<IMethodSymbol, SyntaxNode>();
            HashSet<IMethodSymbol> _noImplementationMethods = new HashSet<IMethodSymbol>();
            foreach (var semanticModelDetails in allProjectOps) {
               if (semanticModelDetails.Value.Any()) {
                  var invocationOps = semanticModelDetails.Value[OperationKind.Invocation];
                  foreach (var op in invocationOps) {
                     var invocationOp = op.Operation as IInvocationOperation;
                     if (!recursiveOnes.Contains(invocationOp.TargetMethod)) {
                        if (!_noImplementationMethods.Contains(invocationOp.TargetMethod)) {
                           SyntaxNode syntax = null;
                           if (!methodToSyntax.TryGetValue(invocationOp.TargetMethod, out syntax)) {
                              methodToSyntax[invocationOp.TargetMethod] = syntax = invocationOp.TargetMethod.GetImplemenationSyntax();
                           }
                           if (null != syntax) {
                              if (syntax.SyntaxTree == op.Operation.Syntax.SyntaxTree) {
                                 if (syntax.FullSpan.Contains(op.Operation.Syntax.FullSpan)) {
                                    //Tuple<SyntaxNode, HashSet<SyntaxNode>> methodData = null;
                                    //if (!methodsToAnalyze.TryGetValue(invocationOp.TargetMethod, out methodData)) {
                                    //   methodsToAnalyze[invocationOp.TargetMethod] = methodData = new Tuple<SyntaxNode, HashSet<SyntaxNode>>(syntax, new HashSet<SyntaxNode>(10));
                                    //}
                                    //methodData.Item2.Add(invocationOp.Syntax);
                                    _recursiveMethods.Add(invocationOp.TargetMethod.OriginalDefinition.ToString());
                                    recursiveOnes.Add(invocationOp.TargetMethod);
                                 }
                              }
                           } else {
                              _noImplementationMethods.Add(invocationOp.TargetMethod);
                           }
                        }
                     }
                  }
               }
            }

            long totalOps = 0, processedOps = 0;

            //foreach (var methodData in methodsToAnalyze) {
            //   totalOps += methodData.Value.Item2.Count;
            //   var syntax = methodData.Value.Item1;
            //   foreach (var opSyntax in methodData.Value.Item2) {
            //      processedOps++;
            //      if (syntax.Contains(opSyntax)) {
            //         _recursiveMethods.Add(methodData.Key.OriginalDefinition.ToString());
            //         break;
            //      }
            //   }
            //}
            watch.Stop();

            Dictionary<string, int> methodToCallCount = new Dictionary<string, int>();
            foreach (var methodData in methodsToAnalyze) {
               if (methodToCallCount.ContainsKey(methodData.Key.OriginalDefinition.ToString())) {
                  methodToCallCount[methodData.Key.OriginalDefinition.ToString()] += methodData.Value.Item2.Count;
               } else {
                  methodToCallCount[methodData.Key.OriginalDefinition.ToString()] = methodData.Value.Item2.Count;
               }
            }

            PerfData.GetOrAdd(compilation.Assembly.Name, (key) => new ConcurrentQueue<FilePerfData>()).Enqueue(
                  new FilePerfData(watch.ElapsedTicks, allProjectOps.Count, processedOps, 0, methodToCallCount)
                  );
         } catch (Exception e) {
            Log.Warn("Exception while analyzing all projects!", e);
         }
      }

      //public override void HandleSemanticModelOps(SemanticModel semanticModel,
      //      IReadOnlyDictionary<OperationKind, IReadOnlyList<OperationDetails>> ops, bool lastBatch)
      //{
      //   var watch = new System.Diagnostics.Stopwatch();
      //   watch.Start();
      //   var invocationOps = ops[OperationKind.Invocation];
      //   Dictionary<IMethodSymbol, List<SyntaxNode>> methodToCalls = new Dictionary<IMethodSymbol, List<SyntaxNode>>();
      //   IMethodSymbol iMethod = null;
      //   foreach (var op in invocationOps) {
      //      iMethod = (op.Operation as IInvocationOperation).TargetMethod;
      //      /*if (_methodToIsRecursive.ContainsKey(iMethod.OriginalDefinition.ToString()))*/ {
      //         if (!methodToCalls.ContainsKey(iMethod)) {
      //            methodToCalls[iMethod] = new List<SyntaxNode>(10);
      //         }
      //         methodToCalls[iMethod].Add(op.Operation.Syntax);
      //      }
      //   }

      //   int processedOps = 0;
      //   HashSet<string> recursiveMethods = new HashSet<string>();

      //   foreach (var methodDetails in methodToCalls) {
      //      var fullName = methodDetails.Key.OriginalDefinition.ToString();
      //      foreach (var syntaxRef in methodDetails.Key.DeclaringSyntaxReferences) {
      //         var syntax = syntaxRef.GetSyntax();
      //         var syntaxTree = syntax.SyntaxTree;
               
      //         foreach (var invocationSyntax in methodDetails.Value) {
      //            if (syntaxTree == invocationSyntax.SyntaxTree) {
      //               processedOps++;
      //               if (syntax.Contains(invocationSyntax)) {
      //                  recursiveMethods.Add(fullName);
      //                  break;
      //               }
      //            }
      //         }
      //      }
      //   }
         

      //   watch.Stop();

      //   PerfData[semanticModel.SyntaxTree.FilePath] =
      //      new FilePerfData(watch.ElapsedTicks, invocationOps.Count(), processedOps, 0, new Dictionary<string, int>());
      //   _recursiveMethods.UnionWith(recursiveMethods);
      //   //foreach (var methodDetails in methodToCalls) {
      //   //   _methodToCallCount[methodDetails.Key.OriginalDefinition.ToString()] =
      //   //      methodDetails.Value.Count;
      //   //}
      //}
   }
}

