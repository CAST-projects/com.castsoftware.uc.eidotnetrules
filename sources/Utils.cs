using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System.Text.RegularExpressions;


namespace CastDotNetExtension.Utils
{
    //https://johnkoerner.com/csharp/how-do-i-analyze-comments/
    public class CommentUtils
    {
        private CommentUtils()
        {

        }

        public static IEnumerable<SyntaxTrivia> GetComments(SemanticModel semanticModel, CancellationToken cancellationToken, Regex regex = null, int minimumLength = 0)
        {
            var root = semanticModel.SyntaxTree.GetRoot(cancellationToken) as CompilationUnitSyntax;
            var commentNodes = from node in root.DescendantTrivia()
                               where (node.IsKind(SyntaxKind.MultiLineCommentTrivia) ||
                               node.IsKind(SyntaxKind.SingleLineCommentTrivia)) &&
                               minimumLength <= node.ToString().Length &&
                               (null == regex || regex.IsMatch(node.ToString()))
                               //orderby node.SpanStart
                               select node;

            return commentNodes;
        }
    }



    internal static class CompilationExtension
    {

        public static void GetMethodSymbolsForSystemClass(this Compilation compilation, string classFullName, HashSet<string> methodNames, ref IAssemblySymbol assembly, ref HashSet<IMethodSymbol> methods, bool useFullName = true, int expectedCount = -1)
        {
            var klazz = compilation.GetTypeByMetadataName(classFullName);
            if (null != klazz && assembly != klazz.ContainingAssembly)
            {
                assembly = klazz.ContainingAssembly;
                methods = compilation.GetMethodSymbolsForSystemClass(klazz, methodNames, useFullName, expectedCount);
            }
        }

        public static HashSet<IMethodSymbol> GetMethodSymbolsForSystemClass(this Compilation compilation, INamedTypeSymbol klazz, HashSet<string> methodNames, bool useFullName = true, int expectedCount = -1)
        {
            HashSet<IMethodSymbol> methods = new HashSet<IMethodSymbol>();
            if (null != klazz)
            {
                foreach (var member in klazz.GetMembers())
                {
                    if (SymbolKind.Method == member.Kind)
                    {
                        if (methodNames.Contains(useFullName ? member.OriginalDefinition.ToString() : member.Name))
                        {
                            methods.Add(member as IMethodSymbol);
                            if (-1 != expectedCount && expectedCount == methods.Count)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            return methods;
        }

        public static IMethodSymbol GetMethodSymbolsByMangling(this Compilation compilation, string mangling)
        {
            IMethodSymbol method = null;

            var posMember = mangling.IndexOf("::");
            var typeName = "";
            if (posMember > 0)
            {
                typeName = mangling.Substring(0, posMember);
            }
            else
            {
                return method;
            }

            var type = compilation.GetTypeByMetadataName(typeName);
            if (type == null)
                return method;

            var fullname = mangling.Replace("::", ".");
            foreach (var member in type.GetMembers())
            {
                if (SymbolKind.Method == member.Kind)
                {
                    if (fullname.Contains(member.OriginalDefinition.ToString()))
                    {
                        method = member as IMethodSymbol;
                        break;
                    }
                }
            }
            return method;
        }

    }

    internal static class InvokeSyntaxExtensions
    {
        public static IMethodSymbol IsOneOfMethods(this SyntaxNodeAnalysisContext context, HashSet<IMethodSymbol> candidateMethods)
        {
            InvocationExpressionSyntax invocation = null;
            return context.IsOneOfMethods(candidateMethods, out invocation);
        }

        public static IMethodSymbol IsOneOfMethods(this SyntaxNodeAnalysisContext context, HashSet<IMethodSymbol> candidateMethods, out InvocationExpressionSyntax invocation)
        {
            invocation = context.Node as InvocationExpressionSyntax;
            if (null != invocation)
            {
                var method = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                if (candidateMethods.Contains(method))
                {
                    return method;
                }
            }
            return null;
        }


        public static bool HasArgumentOfType(this SyntaxNodeAnalysisContext context, HashSet<IMethodSymbol> candidateMethods, HashSet<INamedTypeSymbol> argumentTypes, int startArg = 0)
        {
            InvocationExpressionSyntax invocation = null;

            IMethodSymbol method = context.IsOneOfMethods(candidateMethods, out invocation);
            if (null != method && null != invocation)
            {
                int args = invocation.ArgumentList.Arguments.Count;
                if (startArg < args)
                {
                    for (int index = startArg; index < args; ++index)
                    {
                        var argument = invocation.ArgumentList.Arguments.ElementAt(index);
                        var typeInfo = context.SemanticModel.GetTypeInfo(argument);
                        if (argumentTypes.Contains(typeInfo.Type))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }

    public static class OperationExtensions
    {
        public static bool IsKind(this IOperation iOperation, HashSet<OperationKind> kinds)
        {
            return (null != iOperation && null != kinds && kinds.Contains(iOperation.Kind));
        }

        public static HashSet<ISymbol> GetInitializedSymbols(this IOperation iOperation)
        {
            HashSet<ISymbol> symbols = new HashSet<ISymbol>();
            if (null != iOperation)
            {
                switch (iOperation.Kind)
                {
                    case OperationKind.FieldInitializer:
                        {
                            var iFieldInitializer = iOperation as IFieldInitializerOperation;
                            symbols.UnionWith(iFieldInitializer.InitializedFields);
                            break;
                        }
                    case OperationKind.VariableInitializer:
                        {
                            if (null != iOperation.Parent && OperationKind.VariableDeclarator == iOperation.Parent.Kind)
                            {
                                if (null != iOperation.Parent.Parent && OperationKind.VariableDeclaration == iOperation.Parent.Parent.Kind)
                                {
                                    var iVariableDeclaration = iOperation.Parent.Parent as IVariableDeclarationOperation;
                                    foreach (var iVar in iVariableDeclaration.Declarators)
                                    {
                                        symbols.Add(iVar.Symbol);
                                    }
                                }
                            }

                            break;
                        }
                    case OperationKind.Conversion:
                    case OperationKind.SimpleAssignment:
                        ISimpleAssignmentOperation simpleAssignment = OperationKind.SimpleAssignment == iOperation.Kind ?
                           iOperation as ISimpleAssignmentOperation :
                              null != iOperation.Parent && OperationKind.SimpleAssignment == iOperation.Parent.Kind ?
                                 iOperation.Parent as ISimpleAssignmentOperation : null;
                        if (null != simpleAssignment)
                        {
                            ISymbol iSymbol = simpleAssignment.Target.GetReferenceTarget(false);
                            if (null != iSymbol)
                            {
                                symbols.Add(iSymbol);
                            }
                        }
                        break;
                }
            }
            return symbols;
        }

        public static ISymbol GetReferenceTarget(this IOperation iOperation, bool includeMethodTarget = true)
        {
            ISymbol iSymbol = null;
            if (null != iOperation)
            {
                switch (iOperation.Kind)
                {
                    case OperationKind.LocalReference:
                        iSymbol = (iOperation as ILocalReferenceOperation).Local;
                        break;
                    case OperationKind.FieldReference:
                        iSymbol = (iOperation as IFieldReferenceOperation).Field;
                        break;
                    case OperationKind.PropertyReference:
                        iSymbol = (iOperation as IPropertyReferenceOperation).Property;
                        break;
                    case OperationKind.MethodReference:
                        if (includeMethodTarget)
                        {
                            iSymbol = (iOperation as IMethodReferenceOperation).Method;
                        }
                        break;
                    default:
#if DEBUG
                  System.Console.WriteLine("Unhandled Target Type: {0}", iOperation.Kind);
#endif
                        break;
                }
            }
            return iSymbol;
        }

        public static ITypeSymbol GetThrownSymbol(this IThrowOperation iThrow)
        {
            ITypeSymbol thrown = null;
            if (1 == iThrow.Children.Count())
            {
                var firstOne = iThrow.Children.ElementAt(0);
                if (OperationKind.Conversion == firstOne.Kind)
                {
                    if (OperationKind.ObjectCreation == firstOne.Children.ElementAt(0).Kind)
                    {
                        thrown = (firstOne.Children.ElementAt(0) as IObjectCreationOperation).Type;
                    }
                }
            }
            return thrown;
        }



        public static ISymbol GetReturningSymbol(this IOperation iOperation, SemanticModel semanticModel)
        {
            ISymbol iSymbol = null;
            if (null != iOperation && null != iOperation.Parent)
            {
                if (OperationKind.Return == iOperation.Parent.Kind ||
                   (OperationKind.Conversion == iOperation.Parent.Kind && null != iOperation.Parent.Parent &&
                   OperationKind.Return == iOperation.Parent.Parent.Kind))
                {
                    iSymbol = semanticModel.GetEnclosingSymbol(iOperation.Syntax.SpanStart);
                }
            }
            return iSymbol;
        }

        public enum BooleanLiteralCondition
        {
            None,
            AlwaysTrue,
            AlwaysFalse,
        }

        public static BooleanLiteralCondition GetBooleanLiteralCondition(this IOperation op)
        {
            BooleanLiteralCondition literalCondition = BooleanLiteralCondition.None;
            if (null != op && OperationKind.Literal == op.Kind)
            {
                var literal = op.ConstantValue.Value.ToString();
                switch (literal)
                {
                    case "True":
                        literalCondition = BooleanLiteralCondition.AlwaysTrue;
                        break;
                    case "False":
                        literalCondition = BooleanLiteralCondition.AlwaysFalse;
                        break;
                }
            }
            return literalCondition;

        }


    }

    public static class SyntaxNodeExtensions
    {
        public static bool IsKind(this SyntaxNode node, HashSet<SyntaxKind> kinds)
        {
            return (null != node && null != kinds && kinds.Contains(node.Kind()));
        }
    }

    public static class SymbolExtensions
    {
        public static SyntaxNode GetImplemenationSyntax(this IMethodSymbol iMethod)
        {
            if (null != iMethod)
            {
                var declSynRefs = null != iMethod.PartialImplementationPart ?
                   iMethod.PartialImplementationPart.DeclaringSyntaxReferences :
                   iMethod.DeclaringSyntaxReferences;

                var declSynRef = null != declSynRefs && declSynRefs.Any() ? declSynRefs.ElementAt(0) : null;

                return (null != declSynRef ? declSynRef.GetSyntax() : null);

            }
            return null;
        }

    }

}
