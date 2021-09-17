using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.DotNet.CastDotNetExtension;
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
       Category = "Complexity - Algorithmic and Control Structure Complexity",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.RecursionShouldNotBeInfinite"
   )]
   public class RecursionShouldNotBeInfinite : AbstractOperationsAnalyzer
   {
       public override void Init(AnalysisContext context)
       {
           try
           {
               SubscriberSink.Instance.RegisterCompilationStartAction(context);
               context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.PropertyDeclaration);
           }
           catch (Exception e)
           {
               Log.Warn("Exception while Initing", e);
           }
       }

      public override SyntaxKind[] Kinds(CompilationStartAnalysisContext context)
      {
          return new[] { SyntaxKind.InvocationExpression, SyntaxKind.MethodDeclaration };
      }

      private class DefiniteCallDetector
      {
         private readonly IOperation _targetBlockOp;
         private readonly IMethodSymbol _targetMethod;
         private readonly List<IOperation> _ops = new List<IOperation>();
         private readonly List<ITryOperation> _tries = new List<ITryOperation>();
         private readonly List<ILoopOperation> _loops = new List<ILoopOperation>();
         private readonly INamedTypeSymbol _systemException;
         private readonly Stack<OperationKind> _breakables = new Stack<OperationKind>();
         private readonly ILog _log;

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
                        returnedOp = Detect(op, targetMethod, systemException) ?? op;
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
                           }
                        }
                        break;
                     }
                  case OperationKind.Invocation:
                     returnedOp = (_targetMethod == (op as IInvocationOperation).TargetMethod) ?
                        op : null;
                     if(returnedOp==null)
                         returnedOp = Detect(op, targetMethod, systemException);
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
                                    } 
                                    ifOp = ifOp.Children.ElementAt(2);
                                    if (null != ifOp && OperationKind.Conditional != ifOp.Kind) {
                                       break;
                                    }
                                    
                                 } while (null != ifOp);
                              }
                           }


                           //int line = op.Syntax.GetLocation().GetMappedLineSpan().StartLinePosition.Line;
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

         private IOperation Detect(IOperation targetBlockOp, IMethodSymbol targetMethod, INamedTypeSymbol systemException)
         {
            bool added = false;
            try {
               IOperation returnedOp = null;
               if (!_ops.Any() || targetBlockOp != _ops.Last()) {
                  _ops.Add(targetBlockOp);
                  added = true;
               }
               foreach (var op in targetBlockOp.Children) {
                  if (null != returnedOp) {
                     if (OperationKind.Branch == returnedOp.Kind && returnedOp.Syntax.IsKind(SyntaxKind.GotoStatement)) {
                        if (OperationKind.Labeled != op.Kind || (op as ILabeledOperation).Label.Name != (((GotoStatementSyntax)returnedOp.Syntax).Expression as IdentifierNameSyntax).Identifier.Value.ToString()) {
                           continue;
                        }
                     } else if (op != returnedOp) {
                        continue;
                     }
                  }

                  returnedOp = RouteAndDetect(op, targetMethod, systemException);

                  if (null != returnedOp) {
                     if (OperationKind.Invocation == op.Kind || returnedOp != targetBlockOp)
                     {
                        switch (returnedOp.Kind)
                        {
                           case OperationKind.Throw:
                           case OperationKind.Return:
                              throw new DoneException();
                           case OperationKind.Branch:
                           {
                              if (returnedOp.Syntax.IsKind(SyntaxKind.GotoStatement)) {
                                 if (!((GotoStatementSyntax)returnedOp.Syntax).Expression.IsKind(SyntaxKind.IdentifierName)) {
                                    returnedOp = null;
                                 } else {
                                    continue;
                                 }
                              }

                              break;
                           }
                        }

                        //Console.WriteLine("Breaking: Op Kind: {0} Syntax: {1}", _returnedOp.Kind, _returnedOp.Syntax);
                        break;
                     } else {
                        _log.DebugFormat("Weird returned op: Kind: {0} Syntax: {1}", returnedOp.Kind, returnedOp.Syntax);
                     }
                  }
               }
               return returnedOp;
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
          //Log.InfoFormat("Run registered callback for rule: {0}", GetRuleName());
         try {
            Dictionary<IMethodSymbol, Tuple<IInvocationOperation, MethodDeclarationSyntax, SemanticModel>> recursiveOnes =
               new Dictionary<IMethodSymbol, Tuple<IInvocationOperation, MethodDeclarationSyntax, SemanticModel>>();
            Dictionary<IMethodSymbol, SyntaxNode> methodToSyntax = new Dictionary<IMethodSymbol, SyntaxNode>();
            HashSet<IMethodSymbol> noImplementationMethods = new HashSet<IMethodSymbol>();
            foreach (var semanticModelDetails in allProjectOps) {
               if (semanticModelDetails.Value.Any()) {
                  var invocationOps = semanticModelDetails.Value[OperationKind.Invocation];
                  foreach (var op in invocationOps) {
                     var invocationOp = op.Operation as IInvocationOperation;
                     if (!recursiveOnes.ContainsKey(invocationOp.TargetMethod)) {
                        if (!noImplementationMethods.Contains(invocationOp.TargetMethod)) {
                           SyntaxNode syntax = null;
                           if (!methodToSyntax.TryGetValue(invocationOp.TargetMethod, out syntax)) {
                              methodToSyntax[invocationOp.TargetMethod] = syntax = invocationOp.TargetMethod.GetImplemenationSyntax();
                           }
                           if (null != syntax) {
                              if (syntax.SyntaxTree == op.Operation.Syntax.SyntaxTree) {
                                 if (syntax.FullSpan.Contains(op.Operation.Syntax.FullSpan)) {
                                    if (SyntaxKind.MethodDeclaration == syntax.Kind()) {
                                       recursiveOnes.Add(invocationOp.TargetMethod,
                                          new Tuple<IInvocationOperation, MethodDeclarationSyntax, SemanticModel>(invocationOp, syntax as MethodDeclarationSyntax, compilation.GetSemanticModel(syntax.SyntaxTree)));
                                    }
                                 }
                              }
                           } else {
                              noImplementationMethods.Add(invocationOp.TargetMethod);
                           }
                        }
                     }
                  }
               }
            }


            if (recursiveOnes.Any()) {
               var systemException = compilation.GetTypeByMetadataName("System.Exception");

               foreach (var recursiveOne in recursiveOnes) {
                  var iMethodBodyOp = recursiveOne.Value.Item1.Parent;
                  while (null != iMethodBodyOp && OperationKind.MethodBody != iMethodBodyOp.Kind) {
                     iMethodBodyOp = iMethodBodyOp.Parent;
                  }
                  if (null != iMethodBodyOp) {
                     Log.DebugFormat("Detecting definite recursive call for ", recursiveOne.Value.Item1.TargetMethod.OriginalDefinition.ToString());
                     bool hasDefiniteCall = new DefiniteCallDetector(iMethodBodyOp, recursiveOne.Value.Item1.TargetMethod, systemException, Log).Detect();
                     if (hasDefiniteCall) {
                        var syntax = recursiveOne.Value.Item1.TargetMethod.GetImplemenationSyntax();
                        if (null != syntax) {
                           //Console.WriteLine("Adding Violation: " + recursiveOne.Value.Item1.TargetMethod.Name);
                           AddViolation(recursiveOne.Value.Item1.TargetMethod, new[] { syntax.GetLocation().GetMappedLineSpan()});
                        }
                     }
                  }
               }
            }
         } catch (Exception e) {
            Log.Warn("Exception while analyzing all projects!", e);
         }
         //Log.InfoFormat("END Run registered callback for rule: {0}", GetRuleName());
      }

      private readonly object _lock = new object();
      private void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
      {
          //Log.InfoFormat("Run registered callback for rule: {0}", GetRuleName());
          //lock (_lock)
          {
              try
              {
                  var node = context.Node as PropertyDeclarationSyntax;
                  if(node != null)
                  {
                      string propertyName = node.Identifier.ValueText;
                      var propertySymbol = context.ContainingSymbol;
                      if (propertySymbol != null)
                      {
                          var model = context.SemanticModel;
                          if(model!=null)
                          {
                              foreach (var accessor in node.AccessorList.Accessors)
                              {
                                  if (accessor.IsKind(SyntaxKind.SetAccessorDeclaration) && accessor.Body != null)
                                  {
                                      var assignmentsList = accessor.Body.DescendantNodes().OfType<AssignmentExpressionSyntax>();
                                      foreach (var assignment in assignmentsList)
                                      {
                                          var left = assignment.Left;
                                          IdentifierNameSyntax identifierName = left as IdentifierNameSyntax;
                                          if (identifierName == null)
                                          {
                                              var access = left as MemberAccessExpressionSyntax;
                                              if (access != null)
                                              {
                                                  identifierName = access.Name as IdentifierNameSyntax;
                                              }
                                          }

                                          if (identifierName != null && propertyName.Equals(identifierName.Identifier.ValueText))
                                          {
                                              var symbInf = model.GetSymbolInfo(identifierName);
                                              var accessorSymbol = model.GetDeclaredSymbol(accessor);

                                              if (symbInf.Symbol != null && accessorSymbol != null)
                                              {
                                                  if (propertySymbol.Equals(symbInf.Symbol))
                                                  {
                                                      var pos = assignment.GetLocation().GetMappedLineSpan();
                                                      AddViolation(accessorSymbol, new[] { pos });
                                                  }
                                              }
                                          }

                                      }
                                  }
                                  else if (accessor.IsKind(SyntaxKind.GetAccessorDeclaration) && accessor.Body != null)
                                  {
                                      var returnsList = accessor.Body.DescendantNodes().OfType<ReturnStatementSyntax>();
                                      foreach (var returrn in returnsList)
                                      {
                                          var expression = returrn.Expression;
                                          IdentifierNameSyntax identifierName = expression as IdentifierNameSyntax;
                                          if (identifierName == null)
                                          {
                                              var access = expression as MemberAccessExpressionSyntax;
                                              if (access != null)
                                              {
                                                  identifierName = access.Name as IdentifierNameSyntax;
                                              }
                                          }

                                          if (identifierName != null && propertyName.Equals(identifierName.Identifier.ValueText))
                                          {
                                              var symbInf = model.GetSymbolInfo(identifierName);
                                              var accessorSymbol = model.GetDeclaredSymbol(accessor);

                                              if (symbInf.Symbol != null && accessorSymbol != null)
                                              {
                                                  if (propertySymbol.Equals(symbInf.Symbol))
                                                  {
                                                      var pos = returrn.GetLocation().GetMappedLineSpan();
                                                      AddViolation(accessorSymbol, new[] { pos });
                                                  }
                                              }
                                          }
                                      }
                                  }
                              }
                          }
                      }
                  }
              }
              catch (Exception e)
              {
                  Log.Warn(" Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath + ": " + context.Node.GetLocation().GetMappedLineSpan(), e);
              }
          }
          //Log.InfoFormat("END Run registered callback for rule: {0}", GetRuleName());
      }
   }
}

