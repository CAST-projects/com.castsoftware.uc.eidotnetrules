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
       Id = "EI_TrackTODOTags",
       Title = "Track TODO Tags",
       MessageFormat = "Track TODO Tags",
       Category = "Comments - TODO",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.TrackTODOTags"
   )]
   public class TrackTODOTags : AbstractRuleChecker {
      private static readonly Regex TODO = new Regex("(?i)^(//|/\\*)[ \t]*todo\\b");
      public TrackTODOTags() {
      }

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
               foreach (var comment in Utils.CommentUtils.GetComments(context.SemanticModel, context.CancellationToken, TODO, 6)) {
                  ISymbol iSymbol = context.SemanticModel.GetEnclosingSymbol(comment.SpanStart);
                  var pos = comment.GetLocation().GetMappedLineSpan();
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
