using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;


namespace CastDotNetExtension.Utils {
   //https://johnkoerner.com/csharp/how-do-i-analyze-comments/
   public class CommentUtils {
      public static IEnumerable<SyntaxTrivia> GetComments(SemanticModel semanticModel, CancellationToken cancellationToken, Regex regex = null, int minimumLength = 0) {
         var root = semanticModel.SyntaxTree.GetRoot(cancellationToken) as CompilationUnitSyntax;
         var commentNodes = from node in root.DescendantTrivia()
                            where (node.IsKind(SyntaxKind.MultiLineCommentTrivia) ||
                            node.IsKind(SyntaxKind.SingleLineCommentTrivia)) &&
                            minimumLength <= node.ToString().Length && 
                            ((null == regex || regex.IsMatch(node.ToString())))
                            //orderby node.SpanStart
                            select node;

         return commentNodes;
      }
   }



   internal static class CompilationExtension {

      public static void GetMethodSymbolsForSystemClass(this Compilation compilation, string classFullName, HashSet<string> methodNames, ref IAssemblySymbol assembly, ref HashSet<IMethodSymbol> methods, bool useFullName = true) {
         var klazz = compilation.GetTypeByMetadataName(classFullName) as INamedTypeSymbol;
         if (null != klazz && assembly != klazz.ContainingAssembly) {
            assembly = klazz.ContainingAssembly;
            methods = compilation.GetMethodSymbolsForSystemClass(klazz, methodNames, useFullName);
         }
      }

      public static HashSet<IMethodSymbol> GetMethodSymbolsForSystemClass(this Compilation compilation, INamedTypeSymbol klazz, HashSet<string> methodNames, bool useFullName = true) {
         HashSet<IMethodSymbol> methods = new HashSet<IMethodSymbol>();
         if (null != klazz) {
            methods.UnionWith(klazz.GetMembers().OfType<IMethodSymbol>().Where(m => methodNames.Contains(useFullName ? m.OriginalDefinition.ToString() : m.Name)));
         }
         return methods;
      }

   }

   internal static class InvokeSyntaxExtensions {
      public static IMethodSymbol IsOneOfMethods(this SyntaxNodeAnalysisContext context, HashSet<IMethodSymbol> candidateMethods) {
         InvocationExpressionSyntax invocation = null;
         return context.IsOneOfMethods(candidateMethods, out invocation);
      }

      public static IMethodSymbol IsOneOfMethods(this SyntaxNodeAnalysisContext context, HashSet<IMethodSymbol> candidateMethods, out InvocationExpressionSyntax invocation) {
         invocation = context.Node as InvocationExpressionSyntax;
         if (null != invocation) {
            var method = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (candidateMethods.Contains(method)) {
               return method;
            }
         }
         return null;
      }


      public static bool HasArgumentOfType(this SyntaxNodeAnalysisContext context, HashSet<IMethodSymbol> candidateMethods, HashSet<INamedTypeSymbol> argumentTypes, int startArg = 0) {
         InvocationExpressionSyntax invocation = null;

         IMethodSymbol method = context.IsOneOfMethods(candidateMethods, out invocation);
         if (null != method && null != invocation) {
            int args = invocation.ArgumentList.Arguments.Count;
            if (startArg < args) {
               for (int index = startArg; index < args; ++index) {
                  var argument = invocation.ArgumentList.Arguments.ElementAt(index);
                  var typeInfo = context.SemanticModel.GetTypeInfo(argument);
                  if (argumentTypes.Contains(typeInfo.Type)) {
                     return true;
                  }
               }
            }
         }
         return false;
      }
   }
}
