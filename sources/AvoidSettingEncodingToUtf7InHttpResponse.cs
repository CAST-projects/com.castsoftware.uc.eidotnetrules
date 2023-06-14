using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.DotNet.CastDotNetExtension;
using CastDotNetExtension.Utils;

namespace CastDotNetExtension
{

    [CastRuleChecker]
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [RuleDescription(
       Id = "EI_AvoidSettingEncodingToUtf7InHttpResponse",
       Title = "Avoid setting encoding to UTF-7 in HTTP Response",
       MessageFormat = "Avoid setting encoding to UTF-7 in HTTP Response",
       Category = "Secure Coding - Input Validation",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.AvoidSettingEncodingToUtf7InHttpResponse"
   )]
    public class AvoidSettingEncodingToUtf7InHttpResponse : AbstractRuleChecker
    {
        private static HashSet<string> _baseControllers = new HashSet<string>()
        {
            "System.Web.Mvc.Controller",
            "Microsoft.AspNetCore.Mvc.Controller",
            "System.Web.Mvc.AsyncController"
        };
        private static HashSet<INamedTypeSymbol> _controllerSymbols = new HashSet<INamedTypeSymbol>();

        /// <summary>
        /// Initialize the QR with the given context and register all the syntax nodes
        /// to listen during the visit and provide a specific callback for each one
        /// </summary>
        /// <param name="context"></param>
        public override void Init(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(OnCompilationStart);
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.SimpleAssignmentExpression);
        }

        private void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            context.Compilation.GetSymbolsForClasses(_baseControllers, ref _controllerSymbols);
        }

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            try
            {
                var node = context.Node as AssignmentExpressionSyntax; ;
                if (node == null)
                    return;
                var containingSymbol = context.ContainingSymbol;

                var parentSymbol = containingSymbol.ContainingSymbol;
                while (parentSymbol != null && !(parentSymbol is INamedTypeSymbol))
                {
                    parentSymbol = parentSymbol.ContainingSymbol;
                }

                if (parentSymbol != null)
                {
                    var classSymbol = parentSymbol as INamedTypeSymbol;
                    // Check that the class is a Controller
                    if (!_controllerSymbols.Where(_ => classSymbol.IsOrInheritsFrom(_)).Any())
                        return;
                }

                var right = node.Right as LiteralExpressionSyntax;
                if (right == null || !right.IsKind(SyntaxKind.StringLiteralExpression))
                    return;

                if (right.Token.ValueText.ToUpper() != "UTF-7")
                    return;

                var left = node.Left as MemberAccessExpressionSyntax;
                if (left == null)
                    return;

                if (left.Name.Identifier.ValueText != "Charset")
                    return;

                var expressionType = context.SemanticModel.GetTypeInfo(left.Expression).Type;
                if (expressionType.Name == "HttpResponse" 
                    || expressionType.Name == "HttpResponseBase")
                {
                    var pos = node.GetLocation().GetMappedLineSpan();
                    AddViolation(containingSymbol, new[] { pos });
                }

            }
            catch (Exception e)
            {
                Log.Warn(" Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath + ": " + context.Node.GetLocation().GetMappedLineSpan(), e);
            }
        }

    }
}
