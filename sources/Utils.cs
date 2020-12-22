using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;


namespace CastDotNetExtension.Utils {
   public class Utils {
      public static List<SyntaxTrivia> GetComments(SemanticModel semanticModel, CancellationToken cancellationToken, Regex regex = null) {
         List<SyntaxTrivia> comments = new List<SyntaxTrivia>();
         var root = semanticModel.SyntaxTree.GetCompilationUnitRoot(cancellationToken) as CompilationUnitSyntax;
         var commentNodes = from node in root.DescendantTrivia()
                            where node.IsKind(SyntaxKind.MultiLineCommentTrivia) ||
                            node.IsKind(SyntaxKind.SingleLineCommentTrivia)
                            select node;

         if (commentNodes.Any()) {
            foreach (var node in commentNodes) {
               string commentText = "";
               switch (node.Kind()) {
                  case SyntaxKind.SingleLineCommentTrivia:
                     commentText = node.ToString().TrimStart('/');
                     break;
                  case SyntaxKind.MultiLineCommentTrivia:
                     var nodeText = node.ToString();
                     commentText = nodeText.Substring(2, nodeText.Length - 4);
                     break;
               }

               if (!String.IsNullOrWhiteSpace(commentText)) {
                  if (null == regex || 0 < regex.Matches(commentText).Count) {
                     comments.Add(node);
                  }
               }
            }
         }
         return comments;
      }
     
   }
}
