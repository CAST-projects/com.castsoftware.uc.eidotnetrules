using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;
using Roslyn.DotNet.CastDotNetExtension;
using Roslyn.DotNet.Common;


namespace CastDotNetExtension {

   [CastRuleChecker]
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   [RuleDescription(
       Id = "EI_TrackFIXMETags",
       Title = "Track uses of FIXME tags",
       MessageFormat = "Track uses of FIXME tags",
       Category = "Documentation",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.TrackFIXMETags"
   )]
   public class TrackFIXMETags : AbstractRuleChecker {
      private static readonly Regex FIXME = new Regex("(?i)^(//|/\\*)[ \t]*fixme\\b");
      public TrackFIXMETags() {

      }

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) {
         context.RegisterSemanticModelAction(this.AnalyzeCommentsUsingSemanticModel);
      }

      private object _lock = new object();
      private void AnalyzeCommentsUsingSemanticModel(SemanticModelAnalysisContext context) {
         lock (_lock) {
            try {
               foreach (var comment in Utils.CommentUtils.GetComments(context.SemanticModel, context.CancellationToken, FIXME, 7)) {
                  var pos = comment.GetLocation().GetMappedLineSpan();
                  ISymbol iSymbol = context.SemanticModel.GetEnclosingSymbol(comment.SpanStart);
                  if (null != iSymbol) {
                     AddViolation(iSymbol, new List<FileLinePositionSpan>() { pos });
                  }
               }
            }
            catch (System.Exception e) {
               Log.Warn(e.Message);
               Log.Warn(e.StackTrace);
            }

         }
      }
   }
}
