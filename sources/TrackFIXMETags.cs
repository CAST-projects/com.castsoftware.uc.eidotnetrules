using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Text.RegularExpressions;
using Roslyn.DotNet.CastDotNetExtension;


namespace CastDotNetExtension {

   [CastRuleChecker]
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   [RuleDescription(
       Id = "EI_TrackFIXMETags",
       Title = "Track uses of FIXME tags",
       MessageFormat = "Track uses of FIXME tags",
       Category = "Documentation - Bad Comments",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.TrackFIXMETags"
   )]
   public class TrackFIXMETags : AbstractRuleChecker {
      private static readonly Regex FIXME = new Regex("(?i)^(//|/\\*)[ \t]*fixme\\b");

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         context.RegisterSemanticModelAction(AnalyzeCommentsUsingSemanticModel);
      }

      private readonly object _lock = new object();
      private void AnalyzeCommentsUsingSemanticModel(SemanticModelAnalysisContext context) {
         /*lock (_lock)*/ {
            try {
               if ("C#" == context.SemanticModel.Compilation.Language) {
                  foreach (var comment in Utils.CommentUtils.GetComments(context.SemanticModel, context.CancellationToken, FIXME, 7)) {
                     var pos = comment.GetLocation().GetMappedLineSpan();
                     ISymbol iSymbol = context.SemanticModel.GetEnclosingSymbol(comment.SpanStart);
                     if (null != iSymbol) {
                        AddViolation(iSymbol, new List<FileLinePositionSpan> { pos });
                     }
                  }
               }
            }
            catch (System.Exception e) {
               Log.Warn(" Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath, e);
            }
         }
      }
   }
}
