using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.DotNet.CastDotNetExtension;
using CastDotNetExtension.Utils;
using System.Collections.Concurrent;
using log4net;
using Roslyn.DotNet.Utils;

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
   public class AvoidCreatingExceptionWithoutThrowingThem : AbstractOperationsAnalyzer
   {

      private static readonly SyntaxKind[] SyntaxKinds = new[] {
               SyntaxKind.ThrowStatement,
               SyntaxKind.ThrowExpression,
               SyntaxKind.ObjectCreationExpression,
            };

      public override SyntaxKind[] Kinds(CompilationStartAnalysisContext context)
      {
         try {
            Context.TypeToIsException.Clear();
            Context.SystemException = context.Compilation.GetTypeByMetadataName("System.Exception");
            Context.Interface_Exception = context.Compilation.GetTypeByMetadataName("System.Runtime.InteropServices._Exception");
            if (null == Context.Interface_Exception) {
               Log.WarnFormat(" Could not get type for System.Runtime.InteropServices._Exception while analyzing {0}.",
                  context.Compilation.AssemblyName);
            }
            if (null != Context.SystemException) {
               return SyntaxKinds.ToArray();
            }
            Log.WarnFormat(" Could not get type for System.Exception while analyzing {0}. QR \"{1}\" will be disabled for this project.",
               context.Compilation.AssemblyName, GetRuleName());
            
         } catch (Exception e) {
            Log.Warn(" Exception while analyzing " + context.Compilation.AssemblyName, e);
         }
         return new SyntaxKind[] { };

      }


      public override void HandleSemanticModelOps(SemanticModel semanticModel,
            IReadOnlyDictionary<OperationKind, IReadOnlyList<OperationDetails>> ops, bool lastBatch)
      {
          Log.InfoFormat("[TCR dbg] Run registered callback for rule: {0}", GetRuleName());
          Log.InfoFormat("[TCR dbg] begin HandleSemanticModelOps method");
         try {
            IReadOnlyList<OperationDetails> objCreationOps = ops[OperationKind.ObjectCreation];
            Log.InfoFormat("[TCR dbg] AvoidCreatingExceptionWithoutThrowingThem flag 1");
            if (objCreationOps.Any()) {
                Log.InfoFormat("[TCR dbg] AvoidCreatingExceptionWithoutThrowingThem flag 2");
               Context ctx = new Context(semanticModel.Compilation, semanticModel);
               ProcessObjectCreationOps(objCreationOps, ctx);
               Log.InfoFormat("[TCR dbg] AvoidCreatingExceptionWithoutThrowingThem flag 3");
               if (lastBatch) {
                  if (ctx.ExceptionVars.Any()) {
                     ctx.Throws = ops[OperationKind.Throw];
                     Log.InfoFormat("[TCR dbg] AvoidCreatingExceptionWithoutThrowingThem flag 4");
                     ProcessViolations(ctx);
                  }
               }
            }
         } catch (Exception e) {
            Log.Warn(" Exception while processing operations for " + semanticModel.SyntaxTree.FilePath, e);
         }
         Log.InfoFormat("[TCR dbg] end HandleSemanticModelOps method");
         Log.InfoFormat("[TCR dbg] END Run registered callback for rule: {0}", GetRuleName());
      }

      private class Context
      {
         public static readonly ConcurrentDictionary<INamedTypeSymbol, bool> TypeToIsException = new ConcurrentDictionary<INamedTypeSymbol, bool>();
         public static INamedTypeSymbol SystemException;
         public static INamedTypeSymbol Interface_Exception;

         public readonly HashSet<IOperation> ExcludedThrows = new HashSet<IOperation>();
         public IReadOnlyList<OperationDetails> Throws = new List<OperationDetails>();
         public readonly HashSet<ISymbol> ExceptionVars = new HashSet<ISymbol>();
         public readonly Dictionary<ISymbol, HashSet<FileLinePositionSpan>> Symbol2ViolatingNodes = new Dictionary<ISymbol, HashSet<FileLinePositionSpan>>();

         public readonly SemanticModel SemanticModel;
         //public static long ConversionCount = 0;
         //public static long StoredCount = 0;

         public Context(Compilation compilation, SemanticModel semanticModel)
         {
            SemanticModel = semanticModel;
         }
      }

      private void ProcessViolations(Context ctx)
      {
          Log.InfoFormat("[TCR dbg] begin AvoidCreatingExceptionWithoutThrowingThem.Context.ProcessViolations method");
         foreach (var aThrowDetails in ctx.Throws) {
            var aThrow = aThrowDetails.Operation;
            if (!ctx.ExcludedThrows.Contains(aThrow) && 
               1 == aThrow.Children.Count() && OperationKind.Conversion == aThrow.Children.ElementAt(0).Kind) {
               if (1 == aThrow.Children.ElementAt(0).Children.Count()) {
                  ISymbol iSymbol = null;
                  switch (aThrow.Children.ElementAt(0).Children.ElementAt(0).Kind) {
                     case OperationKind.FieldReference: {
                           var iFieldReference = aThrow.Children.ElementAt(0).Children.ElementAt(0) as IFieldReferenceOperation;
                           iSymbol = iFieldReference.Field;
                           break;
                        }
                     case OperationKind.LocalReference: {
                           var iLocalReference = aThrow.Children.ElementAt(0).Children.ElementAt(0) as ILocalReferenceOperation;
                           iSymbol = iLocalReference.Local;
                           break;
                        }
                  }

                  if (null != iSymbol) {
                     ctx.ExceptionVars.Remove(iSymbol);
                  }
               }
            }
         }
         Log.InfoFormat("[TCR dbg] AvoidCreatingExceptionWithoutThrowingThem.Context flag 5");
         foreach (var exceptionVar in ctx.ExceptionVars) {
            ISymbol violatingSymbol = exceptionVar is IFieldSymbol ? exceptionVar : exceptionVar.ContainingSymbol;
            var pos = exceptionVar.Locations.FirstOrDefault().GetMappedLineSpan();
            AddViolation(violatingSymbol, new[] { pos });
         }
         Log.InfoFormat("[TCR dbg] AvoidCreatingExceptionWithoutThrowingThem.Context flag 6");
         foreach (var iSymbol in ctx.Symbol2ViolatingNodes.Keys) {
            foreach (var pos in ctx.Symbol2ViolatingNodes[iSymbol]) {
               AddViolation(iSymbol, new[] { pos });
            }
         }
         Log.InfoFormat("[TCR dbg] end AvoidCreatingExceptionWithoutThrowingThem.Context.ProcessViolations method");
      }

      private static bool IsException(INamedTypeSymbol iTypeIn)
      {
          ILog Log = new LogWrapper("CAST.Analyzer.DotNet");
          Log.InfoFormat("[TCR dbg] begin AvoidCreatingExceptionWithoutThrowingThem.Context.IsException method");
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
                   Log.InfoFormat("[TCR dbg] AvoidCreatingExceptionWithoutThrowingThem.Context flag 6");
                  //Interlocked.Increment(ref Context.ConversionCount);
                  var baseType = iTypeIn.BaseType;
                  while (null != baseType && Context.SystemException != baseType) {
                     baseType = baseType.BaseType;
                     Log.InfoFormat("[TCR dbg] AvoidCreatingExceptionWithoutThrowingThem.Context flag 7");
                  }
                  isException = null != baseType;
                  Context.TypeToIsException[iTypeIn] = isException;
               }
            }
         }
         Log.InfoFormat("[TCR dbg] end AvoidCreatingExceptionWithoutThrowingThem.Context.IsException method");
         return isException;
      }

      private static void ProcessObjectCreationOps(IReadOnlyList<OperationDetails> objCreationOps, Context ctx)
      {
          ILog Log = new LogWrapper("CAST.Analyzer.DotNet");
          Log.InfoFormat("[TCR dbg] begin AvoidCreatingExceptionWithoutThrowingThem.Context.ProcessObjectCreationOps method");
         foreach (var opDetail in objCreationOps) {
            var objCreationOperation = opDetail.Operation;
            var throwOp = null != objCreationOperation.Parent && OperationKind.Throw == objCreationOperation.Parent.Kind ?
               objCreationOperation.Parent :
               OperationKind.Conversion == objCreationOperation.Parent.Kind && null != objCreationOperation.Parent.Parent &&
               OperationKind.Throw == objCreationOperation.Parent.Parent.Kind ? objCreationOperation.Parent.Parent : null;
            Log.InfoFormat("[TCR dbg] AvoidCreatingExceptionWithoutThrowingThem.Context flag 8");
            if (null == throwOp) {
               //var line = objCreationOperation.Syntax.GetLocation().GetMappedLineSpan().StartLinePosition.Line;
               if (objCreationOperation.Type is INamedTypeSymbol) {
                  if (IsException(objCreationOperation.Type as INamedTypeSymbol)) {
                     if (null != objCreationOperation.Parent) {
                        if (OperationKind.ExpressionStatement == objCreationOperation.Parent.Kind) {
                           var symbol = ctx.SemanticModel.GetEnclosingSymbol(objCreationOperation.Parent.Syntax.GetLocation().SourceSpan.Start);
                           if (null != symbol) {
                               Log.InfoFormat("[TCR dbg] AvoidCreatingExceptionWithoutThrowingThem.Context flag 9");
                              HashSet<FileLinePositionSpan> positions = null;
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
            Log.InfoFormat("[TCR dbg] AvoidCreatingExceptionWithoutThrowingThem.Context flag 10");
         }
         Log.InfoFormat("[TCR dbg] end AvoidCreatingExceptionWithoutThrowingThem.Context.ProcessObjectCreationOps method");
      }
   }
}
