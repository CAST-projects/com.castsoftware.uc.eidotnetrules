using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;

namespace CastDotNetExtension {
   [CastRuleChecker]
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   [RuleDescription(
       Id = "EI_TrackTODOTags",
       Title = "Track TODO Tags",
       MessageFormat = "Track TODO Tags",
       Category = "Comments - TODO",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "DotNetQualityRules.TrackTODOTags"
   )]
   public class TrackTODOTags : AbstractRuleChecker {
      private static readonly Regex TODO = new Regex("(?i)^[ \t]*todo\\b");
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

      private void AnalyzeCommentsUsingSemanticModel(SemanticModelAnalysisContext context) {

         foreach (var comment in Utils.CommentUtils.GetComments(context.SemanticModel, context.CancellationToken, TODO)) {
            var pos = comment.GetLocation().GetMappedLineSpan();
            //Console.WriteLine("Pos: " + pos.ToString());
            ISymbol iSymbol = context.SemanticModel.GetEnclosingSymbol(comment.SpanStart);
            if (null != iSymbol) {
               AddViolation(iSymbol, new List<FileLinePositionSpan>() { pos });
            }
         }
      }

   }
}
