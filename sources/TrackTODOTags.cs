using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Text.RegularExpressions;
using Roslyn.DotNet.CastDotNetExtension;

namespace CastDotNetExtension {
   [CastRuleChecker]
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   [RuleDescription(
       Id = "EI_TrackTODOTags",
       Title = "Track TODO Tags",
       MessageFormat = "Track TODO Tags",
       Category = "Documentation - Bad Comments",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.TrackTODOTags"
   )]
   public class TrackTODOTags : AbstractRuleChecker {
      private static readonly Regex TODO = new Regex("(?i)^(//|/\\*)[ \t]*todo\\b");

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         //TODO: register for events
         context.RegisterSemanticModelAction(this.AnalyzeCommentsUsingSemanticModel);
      }

      private object _lock = new object();
      private void AnalyzeCommentsUsingSemanticModel(SemanticModelAnalysisContext context) {
         lock (_lock) {
            try {
               if ("C#" == context.SemanticModel.Compilation.Language) {
                  foreach (var comment in Utils.CommentUtils.GetComments(context.SemanticModel, context.CancellationToken, TODO, 6)) {
                     ISymbol iSymbol = context.SemanticModel.GetEnclosingSymbol(comment.SpanStart);
                     var pos = comment.GetLocation().GetMappedLineSpan();
                     if (null != iSymbol) {
                        AddViolation(iSymbol, new List<FileLinePositionSpan>() { pos });
                     }
                  }
               }
            }
            catch (System.Exception e) {
               Log.Warn("Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath, e);
            }
         }
      }

   }
}
