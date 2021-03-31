using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using CastDotNetExtension.Utils;
using Roslyn.DotNet.CastDotNetExtension;


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
   public class AvoidCreatingNewInstanceOfSharedInstance : AbstractOperationsAnalyzer
   {

      private INamedTypeSymbol _partCreationPolicyAttribute;
      private INamedTypeSymbol _creationPolicy;
      private IFieldSymbol _shared;
      private INamedTypeSymbol _serviceContainer;
      private HashSet<IMethodSymbol> _addServiceMethods;

      private readonly ConcurrentDictionary<ITypeSymbol, bool> _typeToShared =
         new ConcurrentDictionary<ITypeSymbol, bool>();

      private static readonly SyntaxKind[] SyntaxKinds = new[] {
               SyntaxKind.InvocationExpression,
               SyntaxKind.ObjectCreationExpression,
            };


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

                        return SyntaxKinds;
                     }
                  }
               }
            }
         }

         Log.InfoFormat(" Could not get one or more symbols needed. {0} will be disabled for {1}.",
            GetRuleName(), context.Compilation.Assembly.Name);
         
         return new SyntaxKind [] {};
      }

      public override void Init(AnalysisContext context)
      {
         _typeToShared.Clear();
         base.Init(context);
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

      public override void HandleSemanticModelOps(SemanticModel semanticModel,
            IReadOnlyDictionary<OperationKind, IReadOnlyList<OperationDetails>> ops, bool lastBatch)
      {
         try {
            var sharedObjCreationOps =
               ops[OperationKind.ObjectCreation].Where(o => IsShared((o.Operation as IObjectCreationOperation).Type)).ToHashSet();

            if (sharedObjCreationOps.Any()) {
               HashSet<ISymbol> refs = new HashSet<ISymbol>();
               foreach (var invocationDetails in ops[OperationKind.Invocation]) {
                  var iInvocation = invocationDetails.Operation as IInvocationOperation;
                  if (_addServiceMethods.Contains(iInvocation.TargetMethod)) {
                     var secondArgument = iInvocation.Arguments.ElementAt(1);
                     if (0 == sharedObjCreationOps.RemoveWhere(s => secondArgument.Syntax.Contains(s.Operation.Syntax))) {
                        if (sharedObjCreationOps.Any()) {
                           foreach (var op in secondArgument.Descendants()) {
                              ISymbol iSymbol = null;
                              iSymbol = OperationKind.Conversion == op.Kind ? op.Children.ElementAt(0).GetReferenceTarget() : op.GetReferenceTarget();
                              if (null != iSymbol) {
                                 refs.Add(iSymbol);
                              }
                           }
                        }
                     }
                  }
               }

               if (refs.Any()) {
                  List<KeyValuePair<ISymbol, IOperation>> violations = new List<KeyValuePair<ISymbol,IOperation>>();
                  sharedObjCreationOps.RemoveWhere(op => {
                     HashSet<ISymbol> targetSymbols = op.Operation.Parent.GetInitializedSymbols();
                     if (!targetSymbols.Any()) {
                        ISymbol iSymbol = op.Operation.GetReturningSymbol(semanticModel);
                        if (null != iSymbol) {
                           if (SymbolKind.Method == iSymbol.Kind && MethodKind.PropertyGet == (iSymbol as IMethodSymbol).MethodKind) {
                              iSymbol = (iSymbol as IMethodSymbol).AssociatedSymbol;
                           }
                           targetSymbols.Add(iSymbol);
                        }
                     }
                     if (targetSymbols.Any()) {
                        if (refs.Any(r => targetSymbols.Contains(r))) {
                           return true;
                        }
                     }
                     if (targetSymbols.Any()) {
                        violations.Add(new KeyValuePair<ISymbol, IOperation>(targetSymbols.ElementAt(0), op.Operation));
                     }
                     return false;
                  });

                  foreach (var violationDetails in violations) {
                     AddViolation(violationDetails.Key, new[] { violationDetails.Value.Syntax.GetLocation().GetMappedLineSpan()});
                  }
               }

            }
         } catch (Exception e) {
            Log.Warn(" Exception while processing operations for " + semanticModel.SyntaxTree.FilePath, e);
         }
      }
   }
}
