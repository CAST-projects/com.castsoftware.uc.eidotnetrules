using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CastDotNetExtension.Utils;
using Roslyn.DotNet.CastDotNetExtension;
using Roslyn.Utilities;
using System.Threading;
using System.Collections.Immutable;


namespace CastDotNetExtension {
   [CastRuleChecker]
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   [RuleDescription(
       Id = "EI_AvoidCreatingNewInstanceOfSharedInstance",
       Title = "Avoid Creating New Instance Of Shared Instance",
       MessageFormat = "Avoid Creating New Instance Of Shared Instance",
       Category = "Programming Practices - OO Inheritance and Polymorphism",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.AvoidCreatingNewInstanceOfSharedInstance"
   )]
   public class AvoidCreatingNewInstanceOfSharedInstance : OperationsRetriever
   {

      private INamedTypeSymbol _partCreationPolicyAttribute = null;
      private INamedTypeSymbol _creationPolicy = null;
      private IFieldSymbol _shared = null;
      private INamedTypeSymbol _serviceContainer = null;
      private HashSet<IMethodSymbol> _addServiceMethods = null;

      private ConcurrentDictionary<ITypeSymbol, bool> _typeToShared =
         new ConcurrentDictionary<ITypeSymbol, bool>();

      private static readonly HashSet<SyntaxKind> SyntaxKinds = new HashSet<SyntaxKind> {
               SyntaxKind.InvocationExpression,
               SyntaxKind.ObjectCreationExpression,
            };


      public AvoidCreatingNewInstanceOfSharedInstance()
      {
         //Subscribe(this);
      }

      public override SyntaxKind[] Kinds(CompilationStartAnalysisContext context)
      {
         _partCreationPolicyAttribute = _creationPolicy = _serviceContainer = null;
         _addServiceMethods = null;
         _shared = null;

         _partCreationPolicyAttribute = context.Compilation.GetTypeByMetadataName("System.ComponentModel.Composition.PartCreationPolicyAttribute");
         if (null != _partCreationPolicyAttribute) {
            _creationPolicy = context.Compilation.GetTypeByMetadataName("System.ComponentModel.Composition.CreationPolicy");
            if (null != _creationPolicy) {
               _shared = _creationPolicy.GetMembers("Shared").FirstOrDefault() as IFieldSymbol;
               if (null != _shared) {
                  _serviceContainer = context.Compilation.GetTypeByMetadataName("System.ComponentModel.Design.ServiceContainer");
                  if (null != _serviceContainer) {
                     _addServiceMethods = _serviceContainer.GetMembers().OfType<IMethodSymbol>().Where(m => "AddService" == m.Name).ToHashSet();
                     if (_addServiceMethods.Any()) {
                        return SyntaxKinds.ToArray();
                     }
                  }
               }
            }
         }

         Log.InfoFormat("Could not get one or more symbols needed. {0} will be disabled for {1}.",
            GetRuleName(), context.Compilation.Assembly.Name);

         
         return new SyntaxKind [] {};
      }

      private bool IsShared(ITypeSymbol iType)
      {
         bool isShared = false;
         if (!_typeToShared.TryGetValue(iType, out isShared)) {
            var attrs = iType.GetAttributes();
            if (0 < attrs.Length) {
               var attr = attrs.FirstOrDefault(a => a.AttributeClass == _partCreationPolicyAttribute);
               if (null != attr && attr.ConstructorArguments.Any(c => c.Type == _creationPolicy && c.Value == _shared.ConstantValue)) {
                  isShared = true;
               }
            }
            _typeToShared[iType] = isShared;
         }
         return isShared;
      }

      public override void HandleSemanticModelOps(SemanticModelAnalysisContext context,
            OPs ops)
      {
         var sharedObjCreationOps = 
            ops[OperationKind.ObjectCreation].Where(o => IsShared((o.Operation as IObjectCreationOperation).Type));
         if (sharedObjCreationOps.Any()) {

            List<IArgumentOperation> arguments = new List<IArgumentOperation>();
            foreach (var op in sharedObjCreationOps) {
               //if (OperationKind.Return == op.Parent.Kind ||
               //   (OperationKind.Conversion == op.Parent.Kind &&
               //   null != op.Parent.Parent && OperationKind.Return == op.Parent.Parent.Kind)) {

               //   var method = op.SemanticModel.GetEnclosingSymbol(op.Syntax.SpanStart) as IMethodSymbol;
               //   if (null != method) {
               //      if (MethodKind.LambdaMethod == method.MethodKind) {
               //         var parent = op.Parent.Parent;
               //         while (null != parent && OperationKind.Argument != parent.Kind) {
               //            parent = parent.Parent;
               //         }
               //         if (null != parent) {
               //            if (!_addServiceMethods.Contains((parent.Parent as IInvocationOperation).TargetMethod)) {
               //               arguments.Add(parent as IArgumentOperation);
               //            }
               //         }
               //      }
               //   }
               //} else {
                  //if (OperationKind.Conversion == op.Parent.Kind) {
                  //   if (null != op.Parent.Parent && OperationKind.Argument == op.Parent.Parent.Kind) {
                  //      if (!_addServiceMethods.Contains((op.Parent.Parent.Parent as IInvocationOperation).TargetMethod)) {
                  //         arguments.Add(op.Parent.Parent as IArgumentOperation);
                  //      }
                  //   }
                  //}
                  //Console.WriteLine(op.Parent.Kind);
            //   }
            }
            //var addServiceCalls = 
            //   ops[OperationKind.Invocation].Where(o => _addServiceMethods.Contains((o.Operation as IInvocationOperation).TargetMethod));
         }
      }


      private static readonly OperationKind[] OperationKinds =  {
                                                          OperationKind.Invocation, 
                                                          OperationKind.DynamicInvocation,
                                                          OperationKind.NameOf,

                                                          OperationKind.ObjectCreation, 
                                                          OperationKind.DynamicObjectCreation, 

                                                          OperationKind.DelegateCreation, 
                                                          OperationKind.TypeParameterObjectCreation,

                                                          OperationKind.Invalid, 

                                                          OperationKind.End,
                                                          //OperationKind.ArrayCreation, 
                                                          //OperationKind.AnonymousObjectCreation,
                                                       };

      private ConcurrentDictionary<string, ConcurrentQueue<IOperation>> _operations =
         new ConcurrentDictionary<string, ConcurrentQueue<IOperation>>();


      private class OpDetails
      {
         public List<IOperation> ObjCreationOps { get; private set; }
         public List<IOperation> InvocationOps { get; private set; }
         public Dictionary<SyntaxNode, HashSet<OperationKind>> OtherOps { get; private set; }
         public int Count { get; private set; }
         public bool LastNodeFound { get; private set; }
         public int TotalOps { get; private set; }
         private HashSet<SyntaxNode> _nodes;

         public OpDetails(HashSet<SyntaxNode> nodes)
         {
            ObjCreationOps = new List<IOperation>();
            InvocationOps = new List<IOperation>();
            OtherOps = new Dictionary<SyntaxNode, HashSet<OperationKind>>();
            _nodes = nodes;
            Count = _nodes.Count;
            LastNodeFound = Count == 0;
            TotalOps = 0;
         }

         public void AddOperation(IOperation op)
         {
            List<IOperation> ops = null;
            switch (op.Kind) {
               case OperationKind.Invocation:
                  ops = InvocationOps;
                  break;
               case OperationKind.ObjectCreation:
                  ops = ObjCreationOps;
                  break;
               case OperationKind.Invalid:
               case OperationKind.DynamicObjectCreation:
               case OperationKind.DelegateCreation:
               case OperationKind.TypeParameterObjectCreation:
               case OperationKind.DynamicInvocation:
               case OperationKind.NameOf:
                  _nodes.Remove(op.Syntax);
                  break;
               default:
                  break;
            }
            if (null != ops) {
               _nodes.Remove(op.Syntax);
               TotalOps++;
            }
            LastNodeFound = !_nodes.Any();
         }
      }

      private class Data
      {
         public long Count { get; private set; }
         public Dictionary<OperationKind, string> OpKinds { get; private set; }
         public Data()
         {
            OpKinds = new Dictionary<OperationKind, string>();
         }
         public void Add(IOperation op)
         {
            OpKinds[op.Kind] = op.Syntax.ToString();
            Count++;
         }
      }


      private static ConcurrentDictionary<SyntaxKind, Data>
         SyntaxKindToOperationKind = new ConcurrentDictionary<SyntaxKind, Data>();


      //public AvoidCreatingNewInstanceOfSharedInstance()
      //{
      //   if (!SyntaxKindToOperationKind.Any()) {
      //      SyntaxKindToOperationKind[SyntaxKind.InvocationExpression] = new Data();
      //      SyntaxKindToOperationKind[SyntaxKind.ObjectCreationExpression] = new Data();
      //   }
      //}

      //~AvoidCreatingNewInstanceOfSharedInstance()
      //{
      //   Console.WriteLine("==========SyntaxKind To OperationKind Mapping========");
      //   Console.WriteLine("InvocationExpression: Count: {0}",
      //      SyntaxKindToOperationKind[SyntaxKind.InvocationExpression].Count);
      //   foreach (var opKind in SyntaxKindToOperationKind[SyntaxKind.InvocationExpression].OpKinds) {
      //      Console.WriteLine("   {0}: {1}", opKind.Key, opKind.Value);
      //   }

      //   Console.WriteLine("ObjectCreationExpression: Count: {0}",
      //      SyntaxKindToOperationKind[SyntaxKind.ObjectCreationExpression].Count);
      //   foreach (var opKind in SyntaxKindToOperationKind[SyntaxKind.ObjectCreationExpression].OpKinds) {
      //      Console.WriteLine("   {0}: {1}", opKind.Key, opKind.Value);
      //   }

      //}


      private void OnOperation(OperationAnalysisContext context)
      {

         //SyntaxKind kind = context.Operation.Syntax.Kind();
         //if (kind == SyntaxKind.InvocationExpression ||
         //   kind == SyntaxKind.ObjectCreationExpression) {
         //   Data data = SyntaxKindToOperationKind[kind];
         //   lock (data) {
         //      data.Add(context.Operation);
         //   }
         //}

         var q = _operations.GetOrAdd(context.Operation.Syntax.SyntaxTree.FilePath, (key) => new ConcurrentQueue<IOperation>());
         q.Enqueue(context.Operation);
      }

      private void OnSemanticModelAnalysisEnd(SemanticModelAnalysisContext context)
      {
         try {
            //Console.WriteLine(context.SemanticModel.SyntaxTree.FilePath + ": AvoidCreatingNewInstanceOfShared: " + Thread.CurrentThread.ManagedThreadId);
            //Log.WarnFormat("Analyzing Semantic Model Of {0}", context.SemanticModel.SyntaxTree.FilePath);
            //var watch = new System.Diagnostics.Stopwatch();
            //watch.Start();
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
                     //{
                     //   //Console.WriteLine("Kind: {0} Syntax: {1} Remaining: {2} File: {3} Last Node Found: {4}",
                     //   //   operation.Kind, operation.Syntax, nodes.Count, context.SemanticModel.SyntaxTree.FilePath, opDetails.LastNodeFound);
                     //   if (1 == nodes.Count) {
                     //      var op = context.SemanticModel.GetOperation(nodes.ElementAt(0));
                     //      Console.WriteLine(nodes.ElementAt(0).Kind() + ": " + nodes.ElementAt(0) + ": " + (null != op ? op.Kind.ToString() : "null"));
                     //   }
                     //}

                     if (opDetails.LastNodeFound) {
                        break;
                     }
                  }
                  if (null == operation) {
                     //Log.Warn("operation was null; trying again! File: " + context.SemanticModel.SyntaxTree.FilePath);
                     continue;
                  }
                  if (OperationKind.End == operation.Kind || opDetails.LastNodeFound) {
                     break;
                  }
               }
               //watch.Stop();
               //if (opDetails.OtherOps.Any()) {
               //   foreach (var op in opDetails.InvocationOps) {
               //      if (opDetails.OtherOps.Keys.Contains(op.Syntax)) {
               //         opDetails.OtherOps.Remove(op.Syntax);
               //      }
               //   }
               //   foreach (var otherOpDetails in opDetails.OtherOps) {
               //      Console.Write("Syntax Not Invocation: {0} ", otherOpDetails.Key);
               //      foreach (var opKind in otherOpDetails.Value) {
               //         Console.Write("{0}, ", opKind);
               //      }
               //      Console.WriteLine();
               //   }
               //}
               //Log.InfoFormat("Performance Data - Total: {0} Object Creation: {1} Invocation: {2} Time: {3}",
               //   opDetails.Count, opDetails.ObjCreationOps.Count, opDetails.InvocationOps.Count, watch.ElapsedMilliseconds);
            }
         }  catch (Exception e) {
            Log.Warn("Exception while analyzing semantic model " + context.SemanticModel.SyntaxTree.FilePath, e);
         }
      }
      private void OnStartCompilation(CompilationStartAnalysisContext context)
      {


         //var partCreationPolicy = context.Compilation.GetTypeByMetadataName("System.ComponentModel.Composition.PartCreationPolicyAttribute");
         //var creationPolicy = context.Compilation.GetTypeByMetadataName("System.ComponentModel.Composition.CreationPolicy") as INamedTypeSymbol;
         //if (null != creationPolicy) {
         //   var members = creationPolicy.GetMembers("Shared");
         //}
         //var creationPolicyShared = context.Compilation. ("System.ComponentModel.Composition.CreationPolicy.Shared");
         //_operations.Clear();
         //_addServiceMethods = null;
         //var serviceContainer = context.Compilation.GetTypeByMetadataName("System.ComponentModel.Design.ServiceContainer");
         //if (null != serviceContainer) {
         //   _addServiceMethods = serviceContainer.GetMembers().OfType<IMethodSymbol>().Where(m => "AddService" == m.Name).ToHashSet();
         //}

         //if (null != _addServiceMethods && _addServiceMethods.Any()) {
         //   context.RegisterSemanticModelAction(HandleSemanticModelAnalysisEnd);
         //} else {
         //   Log.InfoFormat("Could not get symbol for System.ComponentModel.Design.ServiceContainer.AddService. {0} will be disabled for {1}.",
         //      GetRuleName(), context.Compilation.Assembly.Name);
         //}

      }

      protected void VisitClassDeclaration(SyntaxNode node, Compilation compilation, ref HashSet<string> sharedSymbols) {
         var declarationSyntax = node as TypeDeclarationSyntax;
         IList<TypeAttributes.ITypeAttribute> typeAttributes = new List<TypeAttributes.ITypeAttribute>();
         typeAttributes = TypeAttributes.Get(declarationSyntax, typeAttributes, new[] { TypeAttributes.AttributeType.PartCreationPolicy });

         if (null != typeAttributes && typeAttributes.Any()) {
            sharedSymbols.Add(declarationSyntax.Identifier.ValueText);
         }
      }

      protected void VisitObjectCreation(SyntaxNode node, Compilation compilation,
         ref HashSet<string> sharedSymbols,
         ref Dictionary<string, Tuple<SyntaxNode, SyntaxNode, ISymbol>> allCreators,
         SemanticModel semanticModel) {

         var objectCreationSyntax = node as ObjectCreationExpressionSyntax;
         var typename = objectCreationSyntax.Type.ToString();
         if (IsTypeRelevant(typename, ref sharedSymbols)) {
            SyntaxNode parentNode = null;
            string name = SyntaxNode2SubjectName.Get(node, delegate(SyntaxNode parent) {
               parentNode = parent;
               if (null != parent) {
                  if (SyntaxKind.InvocationExpression == parent.Kind()) {
                     var invocation = parent as InvocationExpressionSyntax;
                     if (null != semanticModel) {
                        IMethodSymbol invokedMethod = semanticModel.GetSymbolInfo(parent).Symbol as IMethodSymbol;
                        if (null != invokedMethod) {
                           if (IsAddServiceMethod(invokedMethod)) {
                              return false;
                           }
                        }
                     }
                  }
               }

               return true;
            });

            if (null != name && SyntaxNode2SubjectName.LAMBDA != name && null != parentNode) {
               ISymbol iSymbol = semanticModel.GetEnclosingSymbol(node.GetLocation().GetMappedLineSpan().Span.Start.Line);
               if (null != iSymbol) {
                  AddCreator(name, parentNode, node, iSymbol, ref allCreators);
               }
            }
         }
      }


      protected void VisitInvocationExpression(SyntaxNode node, Compilation compilation, ref HashSet<string> creatorOrVariable) {

         var invokeExpr = node as InvocationExpressionSyntax;
         
         if (null != invokeExpr) {
            var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);
            if (null != semanticModel) {
               var iSymbol = semanticModel.GetSymbolInfo(invokeExpr.Expression).Symbol;
               var invokedMethod = iSymbol as IMethodSymbol;
               if (invokedMethod != null) {
                  if (MethodKind.Ordinary == invokedMethod.MethodKind) {
                     if (IsAddServiceMethod(invokedMethod)) {
                        if (2 <= invokeExpr.ArgumentList.Arguments.Count) {
                           var argument = invokeExpr.ArgumentList.Arguments[1];
                           var identifierNameSyntax = GetIdentifierNameSyntax(argument);
                           if (null != identifierNameSyntax) {
                              string name = GetCreatorOrVariableName(identifierNameSyntax, semanticModel);
                              if (null != name) {
                                 AddCreatorOrVariable(name, ref creatorOrVariable);
                              }
                           }
                        }
                     }
                  }
               }
            }
         }
      }

      private static string GetCreatorOrVariableName(IdentifierNameSyntax identifierNameSyntax, SemanticModel semanticModel)
      {
         string name = null;
         if (null != identifierNameSyntax) {
            ISymbol iSymbol = semanticModel.GetSymbolInfo(identifierNameSyntax).Symbol;
            if (iSymbol is IMethodSymbol) {
               var creator = iSymbol as IMethodSymbol;
               name = creator.Name;
               if (MethodKind.PropertyGet == creator.MethodKind) {
                  name = creator.Name.Substring(4);
               }
            }
            else {
               name = identifierNameSyntax.Identifier.ValueText;
            }
         }
         return name;
      }

      protected IdentifierNameSyntax GetIdentifierNameSyntax(ArgumentSyntax argument) {
         if (null != argument) {
            var objectCreationSyntax = argument.Expression as ObjectCreationExpressionSyntax;
            if (null == objectCreationSyntax) {
               IdentifierNameSyntax identifierNameSyntax = argument.Expression as IdentifierNameSyntax;
               return identifierNameSyntax;
            }
         }
         return null;
      }

      private static bool IsAddServiceMethod(IMethodSymbol method)
      {
         var originalDefinition = method.OriginalDefinition.ToString();
         if (originalDefinition.StartsWith("System.ComponentModel.Design.ServiceContainer.AddService")) {
            return true;
         }
         return false;
      }

      //private bool IsAddServiceMethod(IMethodSymbol method)
      //{
      //   if (_addServiceMethods.Contains(method)) {
      //      return true;
      //   }
      //   return false;
      //}

      private static void WriteLine(string msg)
      {
         //System.Console.WriteLine(msg);
      }

      private static bool IsTypeRelevant(string typename, ref HashSet<string> sharedSymbols) {
         WriteLine("IsTypeRelevant: " + typename);
         return sharedSymbols.Contains(typename);
      }

      private static void AddCreatorOrVariable(string creatorOrVariable, ref HashSet<string> creatorOrVariables) {
         creatorOrVariables.Add(creatorOrVariable);
         WriteLine("AddCreatorOrVariable: " + creatorOrVariable);
      }

      private static void AddCreator(string creator, SyntaxNode creatorContainerSyntax, SyntaxNode creatorSyntax, ISymbol iSymbol,
         ref Dictionary<string, Tuple<SyntaxNode, SyntaxNode, ISymbol>> allCreators) {
         allCreators[creator] = new Tuple<SyntaxNode, SyntaxNode, ISymbol>(creatorContainerSyntax, creatorSyntax, iSymbol);
         WriteLine("AddCreator: " + creator);
      }

      private void HandleSemanticModelAnalysisEnd(SemanticModelAnalysisContext context)
      {
         try {
            if ("C#" == context.SemanticModel.Compilation.Language) {

               HashSet<string> sharedSymbols = new HashSet<string>();
               IEnumerable<SyntaxNode> classDeclarations = context.SemanticModel.SyntaxTree.GetRoot().DescendantNodes().Where(n => n.IsKind(SyntaxKind.ClassDeclaration));

               foreach (var classDeclaration in classDeclarations) {
                  VisitClassDeclaration(classDeclaration, context.SemanticModel.Compilation, ref sharedSymbols);
               }

               if (!sharedSymbols.Any()) {
                  Log.Debug("No Shared Symbols Found");
               } else {
                  HashSet<SyntaxKind> syntaxKinds = new HashSet<SyntaxKind> {
                        SyntaxKind.InvocationExpression, SyntaxKind.ParenthesizedLambdaExpression, SyntaxKind.SimpleLambdaExpression, SyntaxKind.ObjectCreationExpression
                     };

                  IEnumerable<SyntaxNode> nodes = context.SemanticModel.SyntaxTree.GetRoot().DescendantNodes().Where(n => syntaxKinds.Contains(n.Kind()));

                  HashSet<string> creatorOrVariables = new HashSet<string>();
                  Dictionary<string, Tuple<SyntaxNode, SyntaxNode, ISymbol>> allCreators = new Dictionary<string, Tuple<SyntaxNode, SyntaxNode, ISymbol>>();

                  foreach (var node in nodes) {
                     if (node is InvocationExpressionSyntax) {
                        VisitInvocationExpression(node, context.SemanticModel.Compilation, ref creatorOrVariables);
                     } else if (node is ObjectCreationExpressionSyntax) {
                        VisitObjectCreation(node, context.SemanticModel.Compilation, ref sharedSymbols, ref allCreators, context.SemanticModel);
                     }
                  }

                  foreach (var creator in allCreators.Keys) {
                     Tuple<SyntaxNode, SyntaxNode, ISymbol> location = allCreators[creator];
                     if (!creatorOrVariables.Contains(creator)) {
                        if (null != location.Item3) {
                           var pos = location.Item2.GetLocation().GetMappedLineSpan();
                           //Console.WriteLine(location.Item3 + ": " + pos);
                           AddViolation(location.Item3, new FileLinePositionSpan[] { pos });
                        }
                     }
                  }
               }
            }
         } catch (Exception e) {
            Log.Warn("Exception while analyzing semantic model " + context.SemanticModel.SyntaxTree.FilePath, e);
         }
      }

      private OperationKind[] OperationsKinds = {
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

   }
}
