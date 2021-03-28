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
using log4net;

namespace CastDotNetExtension
{
   [CastRuleChecker]
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   [RuleDescription(
       Id = "EI_RecursionShouldNotBeInfinite",
       Title = "Recursion should not be infinite",
       MessageFormat = "Recursion should not be infinite",
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
         return new SyntaxKind[] { SyntaxKind.InvocationExpression, SyntaxKind.MethodDeclaration };
      }

      private HashSet<string> _recursiveMethods = new HashSet<string>();

      ~RecursionShouldNotBeInfinite()
      {
         
         long TotalTime = 0, TotalOps = 0, TotalProcessedOps = 0, RecursiveMethodCount = 0;
         Console.WriteLine("File,Time(ticks),Total Ops,Processed Ops,Recursive Methods");
         foreach (var filePerfDataQ in PerfData) {
            foreach (var filePerfData in filePerfDataQ.Value.ToArray()) {
               TotalTime += filePerfData.Time;
               TotalOps += filePerfData.Ops;
               TotalProcessedOps += filePerfData.ProcessedOps;
               RecursiveMethodCount += filePerfData.RecursiveMethodCount;
               Console.WriteLine("\"{0}\",{1},{2},{3},{4}",
                  filePerfDataQ.Key, filePerfData.Time, filePerfData.Ops, filePerfData.ProcessedOps,filePerfData.RecursiveMethodCount);
               foreach (var methodDetails in filePerfData.MethodToLine) {
                  Console.WriteLine("   Recursive Call: {0}: {1}", methodDetails.Key, methodDetails.Value);
               }
            }
         }
         Console.WriteLine("{0},{1} ms,{2},{3},{4}", "All", TotalTime / TimeSpan.TicksPerMillisecond, TotalOps, TotalProcessedOps,RecursiveMethodCount);

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
         public long RecursiveMethodCount { get; private set; }
         public Dictionary<string, int> MethodToLine {get;private set;}
         public FilePerfData(long time, long ops, long processedOps, long recursiveMethodCount, Dictionary<string, int> methodToLine)
         {
            Time = time;
            Ops = ops;
            ProcessedOps = processedOps;
            RecursiveMethodCount = recursiveMethodCount;
            MethodToLine = methodToLine;
         }
      }

      private ConcurrentDictionary<string, ConcurrentQueue<FilePerfData>> PerfData =
         new ConcurrentDictionary<string, ConcurrentQueue<FilePerfData>>();

      //private ConcurrentDictionary<string, int> _methodToCallCount =
      //   new ConcurrentDictionary<string, int>();

      //private ConcurrentDictionary<MethodKind, int> _methodKindToCount = new ConcurrentDictionary<MethodKind, int>();

      private class DefiniteCallDetector
      {
         private IOperation _targetBlockOp;
         private IMethodSymbol _targetMethod;
         private List<IOperation> _ops = new List<IOperation>();
         private List<ITryOperation> _tries = new List<ITryOperation>();
         private List<ILoopOperation> _loops = new List<ILoopOperation>();
         private INamedTypeSymbol _systemException;
         private Stack<OperationKind> _breakables = new Stack<OperationKind>();
         private ILog _log;

         private class DoneException : Exception
         {

         }

         public DefiniteCallDetector(IOperation targetBlockOp, IMethodSymbol targetMethod, INamedTypeSymbol systemException, ILog log)
         {
            _targetBlockOp = targetBlockOp;
            _targetMethod = targetMethod;
            _systemException = systemException;
            _log = log;
         }

         private IOperation RouteAndDetect(IOperation op, IMethodSymbol targetMethod, INamedTypeSymbol systemException)
         {
            bool added = false;
            try {
               if (!_ops.Any() || op != _ops.Last()) {
                  _ops.Add(op);
                  added = true;
               }
               //Console.WriteLine(op.Kind + ": " + op.Syntax.ToString());
               IOperation returnedOp = null;
               switch (op.Kind) {
                  case OperationKind.Try: {
                        var opTry = (ITryOperation)op;
                        _tries.Add(opTry);
                        if (null != systemException) {
                           if (null != _systemException) {
                              foreach (var catchOp in opTry.Catches) {
                                 if (systemException == catchOp.ExceptionType) {
                                    returnedOp = Detect(catchOp, targetMethod, systemException);
                                    break;
                                 }
                              }
                           }
                           if (null == returnedOp && null != opTry.Finally) {
                              returnedOp = Detect(opTry.Finally, targetMethod, systemException);
                           }

                           if (null == returnedOp ) {
                              _ops.Add(opTry);
                              returnedOp = Detect(opTry, targetMethod, systemException);
                           }
                        }
                        _tries.Remove(_tries.Last());
                        break;
                     }
                  case OperationKind.Return: {
                        returnedOp = Detect(op, targetMethod, systemException);
                        if (null == returnedOp) {
                           returnedOp = op;
                        }
                        break;
                     }
                  case OperationKind.Throw: {
                        var opThrow = (IThrowOperation)op;
                        var thrownType = opThrow.GetThrownSymbol();
                        if (_tries.Any() && null != _systemException && null != thrownType) {

                           for (int i = _tries.Count - 1; 0 <= i; --i) {
                              var opTry = _tries.ElementAt(i);
                              foreach (var opCatch in opTry.Catches) {
                                 if (null != opCatch.ExceptionType) {
                                    if (_systemException == opCatch.ExceptionType) {
                                       break;
                                    }

                                    var parent = thrownType;

                                    do {
                                       if (parent == opCatch.ExceptionType) {
                                          break;
                                       }
                                       parent = parent.BaseType;
                                    } while (null != parent);

                                    if (null != parent) {
                                       returnedOp = Detect(opCatch, targetMethod, systemException);
                                       break;
                                    }
                                 }
                              }
                           }
                           if (null == returnedOp) {
                              returnedOp = op;
                              break;
                           }
                        }
                        break;
                     }
                  case OperationKind.Invocation:
                     returnedOp = (_targetMethod == (op as IInvocationOperation).TargetMethod) ?
                        op : null;
                     break;
                  case OperationKind.Conditional: {
                        var count = op.Children.Count();
                        var literalCondition = (op as IConditionalOperation).Condition.GetBooleanLiteralCondition();

                        if (Utils.OperationExtensions.BooleanLiteralCondition.AlwaysFalse != literalCondition) {
                           bool hasElse = true;
                           if (Utils.OperationExtensions.BooleanLiteralCondition.AlwaysTrue != literalCondition) {
                              if (OperationKind.Conditional != op.Parent.Kind) {
                                 var ifOp = op;
                                 do {
                                    if (3 != ifOp.Children.Count()) {
                                       hasElse = false;
                                       break;
                                    } else {
                                       ifOp = ifOp.Children.ElementAt(2);
                                       if (null != ifOp && OperationKind.Conditional != ifOp.Kind) {
                                          hasElse = true;
                                          break;
                                       }
                                    }
                                 } while (null != ifOp);
                              }
                           }


                           int line = op.Syntax.GetLocation().GetMappedLineSpan().StartLinePosition.Line;
                           var parentOpKinds = new HashSet<OperationKind> { OperationKind.Switch, OperationKind.Loop, OperationKind.Try };
                           int lastCounterFoundCall = -1;
                           for (int i = 0; i < count; ++i) {
                              var child = op.Children.ElementAt(i);
                              IOperation returnedOpLocal = RouteAndDetect(child, targetMethod, systemException);

                              if (null != returnedOpLocal) {
                                 if (OperationKind.Invocation == returnedOpLocal.Kind /*&& hasElse && 0 != i*/) {
                                    if ((1 >= i || lastCounterFoundCall == i - 1) && ((0 != lastCounterFoundCall && hasElse) || 0 == i)) {
                                       returnedOp = returnedOpLocal;
                                       lastCounterFoundCall = i;
                                    }
                                 } else {
                                    returnedOp = returnedOpLocal;
                                    break;
                                 }
                              } else if (null != returnedOp && OperationKind.Invocation == returnedOp.Kind && 0 != lastCounterFoundCall) {
                                 returnedOp = null;
                              }
                           }
                        }
                        break;
                     }
                  case OperationKind.Switch:
                     _breakables.Push(op.Kind);
                     var iSwitch = op as ISwitchOperation;
                     if (null == (returnedOp = RouteAndDetect(iSwitch.Value, targetMethod, systemException))) {
                        if (iSwitch.Cases.Any(c => c.Clauses.Any(clause => CaseKind.Default == clause.CaseKind))) {
                           foreach (var acase in iSwitch.Cases) {
                              if (null == (returnedOp = Detect(acase, targetMethod, systemException))) {
                                 break;
                              }
                           }
                        }
                     }
                     _breakables.Pop();
                     break;
                  case OperationKind.Loop:
                     _breakables.Push(op.Kind);
                     var iLoop = op as ILoopOperation;
                     _loops.Add(iLoop);
                     if (LoopKind.While == iLoop.LoopKind) {
                        var iWhile = iLoop as IWhileLoopOperation;
                        var literalCondition = iWhile.Condition.GetBooleanLiteralCondition();
                        if (!iWhile.ConditionIsTop || Utils.OperationExtensions.BooleanLiteralCondition.AlwaysFalse != literalCondition) {
                           if (Utils.OperationExtensions.BooleanLiteralCondition.AlwaysTrue == literalCondition || null == (returnedOp = Detect(iWhile.Condition, targetMethod, systemException))) {
                              returnedOp = Detect(iWhile.Body, targetMethod, systemException);
                              if (op == returnedOp) {
                                 returnedOp = null;
                                 break;
                              }
                           }
                        }
                     }
                     _loops.Remove(_loops.Last());
                     _breakables.Pop();
                     break;

                  case OperationKind.Branch:
                  case OperationKind.Literal:
                     var breaks = new HashSet<SyntaxKind> {SyntaxKind.BreakKeyword, SyntaxKind.BreakStatement};
                     var continues = new HashSet<SyntaxKind> { SyntaxKind.ContinueKeyword, SyntaxKind.ContinueStatement };
                     var kind = op.Syntax.Kind();
                     if (OperationKind.Branch == op.Kind && SyntaxKind.GotoStatement == kind) {
                        returnedOp = op;
                     } else if (continues.Contains(kind) || (breaks.Contains(kind) && OperationKind.Switch != _breakables.Peek())) {
                        returnedOp = _loops.Last();
                     }
                     break;
                  //case OperationKind.Block:
                  //case OperationKind.Switch:
                  //case OperationKind.Loop:
                  //case OperationKind.ExpressionStatement:
                  //case OperationKind.Binary:
                  //case OperationKind.SimpleAssignment:
                  default:
                     returnedOp = Detect(op, targetMethod, systemException);
                     break;
               }

               return returnedOp;
            } finally {
               if (added) {
                  _ops.Remove(_ops.Last());
               }
            }
         }



         public IOperation Detect(IOperation targetBlockOp, IMethodSymbol targetMethod, INamedTypeSymbol systemException)
         {
            bool added = false;
            try {
               IOperation _returnedOp = null;
               if (!_ops.Any() || targetBlockOp != _ops.Last()) {
                  _ops.Add(targetBlockOp);
                  added = true;
               }
               foreach (var op in targetBlockOp.Children) {
                  if (null != _returnedOp) {
                     if (OperationKind.Branch == _returnedOp.Kind && _returnedOp.Syntax.IsKind(SyntaxKind.GotoStatement)) {
                        if (OperationKind.Labeled != op.Kind || (op as ILabeledOperation).Label.Name != (((GotoStatementSyntax)_returnedOp.Syntax).Expression as IdentifierNameSyntax).Identifier.Value.ToString()) {
                           //Console.WriteLine("Skipping: {0} Syntax: {1}", op.Kind, op.Syntax);
                           continue;
                        }
                     }
                  }

                  _returnedOp = RouteAndDetect(op, targetMethod, systemException);

                  if (null != _returnedOp) {
                     if (OperationKind.Invocation == op.Kind || _returnedOp != targetBlockOp) {
                        if (OperationKind.Throw == _returnedOp.Kind || OperationKind.Return == _returnedOp.Kind) {
                           throw new DoneException();
                        } else if (OperationKind.Branch == _returnedOp.Kind) {
                           if (_returnedOp.Syntax.IsKind(SyntaxKind.GotoStatement)) {
                              if (!((GotoStatementSyntax)_returnedOp.Syntax).Expression.IsKind(SyntaxKind.IdentifierName)) {
                                 _returnedOp = null;
                              } else {
                                 continue;
                              }
                           }
                        }
                        //Console.WriteLine("Breaking: Op Kind: {0} Syntax: {1}", _returnedOp.Kind, _returnedOp.Syntax);
                        break;
                     } else {
                        Console.WriteLine("Weird returned op: Kind: {0} Syntax: {1}", _returnedOp.Kind, _returnedOp.Syntax.ToString());
                     }
                  }
               }
               return _returnedOp;
            } finally {
               if (added) {
                  _ops.Remove(_ops.Last());
               }
            }
         }

         public bool Detect()
         {
            IOperation op = null;
            try {
               op = Detect(_targetBlockOp, _targetMethod, _systemException);
               if (_ops.Any()) {
                           
               }
            } catch (DoneException) {
               // NOP
            }
            return (null != op);
         }
      }


      public override void HandleProjectOps(Compilation compilation, Dictionary<SemanticModel, Dictionary<OperationKind, IReadOnlyList<OperationDetails>>> allProjectOps)
      {
         try {
            Dictionary<IMethodSymbol, Tuple<IInvocationOperation, MethodDeclarationSyntax, SemanticModel>> recursiveOnes =
               new Dictionary<IMethodSymbol, Tuple<IInvocationOperation, MethodDeclarationSyntax, SemanticModel>>();
            Dictionary<IMethodSymbol, SyntaxNode> methodToSyntax = new Dictionary<IMethodSymbol, SyntaxNode>();
            HashSet<IMethodSymbol> _noImplementationMethods = new HashSet<IMethodSymbol>();
            foreach (var semanticModelDetails in allProjectOps) {
               if (semanticModelDetails.Value.Any()) {
                  var invocationOps = semanticModelDetails.Value[OperationKind.Invocation];
                  foreach (var op in invocationOps) {
                     var invocationOp = op.Operation as IInvocationOperation;
                     if (!recursiveOnes.ContainsKey(invocationOp.TargetMethod)) {
                        if (!_noImplementationMethods.Contains(invocationOp.TargetMethod)) {
                           SyntaxNode syntax = null;
                           if (!methodToSyntax.TryGetValue(invocationOp.TargetMethod, out syntax)) {
                              methodToSyntax[invocationOp.TargetMethod] = syntax = invocationOp.TargetMethod.GetImplemenationSyntax();
                           }
                           if (null != syntax) {
                              if (syntax.SyntaxTree == op.Operation.Syntax.SyntaxTree) {
                                 if (syntax.FullSpan.Contains(op.Operation.Syntax.FullSpan)) {
                                    _recursiveMethods.Add(invocationOp.TargetMethod.OriginalDefinition.ToString());
                                    if (SyntaxKind.MethodDeclaration == syntax.Kind()) {
                                       recursiveOnes.Add(invocationOp.TargetMethod,
                                          new Tuple<IInvocationOperation, MethodDeclarationSyntax, SemanticModel>(invocationOp, syntax as MethodDeclarationSyntax, compilation.GetSemanticModel(syntax.SyntaxTree)));
                                    }
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

            long processedOps = 0;


            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            if (recursiveOnes.Any()) {
               var systemException = compilation.GetTypeByMetadataName("System.Exception");

               foreach (var recursiveOne in recursiveOnes) {
                  var iMethodBodyOp = recursiveOne.Value.Item1.Parent;
                  while (null != iMethodBodyOp && OperationKind.MethodBody != iMethodBodyOp.Kind) {
                     iMethodBodyOp = iMethodBodyOp.Parent;
                  }
                  if (null != iMethodBodyOp) {
                     bool hasDefiniteCall = new DefiniteCallDetector(iMethodBodyOp, recursiveOne.Value.Item1.TargetMethod, systemException, Log).Detect();
                     if (hasDefiniteCall) {
                        var syntax = recursiveOne.Value.Item1.TargetMethod.GetImplemenationSyntax();
                        if (null != syntax) {
                           //Console.WriteLine("Adding Violation: " + recursiveOne.Value.Item1.TargetMethod.Name);
                           AddViolation(recursiveOne.Value.Item1.TargetMethod, new FileLinePositionSpan[] { syntax.GetLocation().GetMappedLineSpan()});
                        }
                     }
                  }
               }
            }

            watch.Stop();

            Dictionary<string, int> methodToCallCount = new Dictionary<string, int>();

            PerfData.GetOrAdd(compilation.Assembly.Name, (key) => new ConcurrentQueue<FilePerfData>()).Enqueue(
                  new FilePerfData(watch.ElapsedTicks, allProjectOps.Count, processedOps, recursiveOnes.Count, methodToCallCount)
                  );
         } catch (Exception e) {
            Log.Warn("Exception while analyzing all projects!", e);
         }
      }
   }
}

