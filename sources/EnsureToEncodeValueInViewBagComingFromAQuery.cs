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
       Id = "EI_EnsureToEncodeValueInViewBagComingFromAQuery",
       Title = "Ensure to encode value in ViewBag coming from a query",
       MessageFormat = "Ensure to encode value in ViewBag coming from a query",
       Category = "Secure Coding - Weak Security Features",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.EnsureToEncodeValueInViewBagComingFromAQuery"
   )]
    public class EnsureToEncodeValueInViewBagComingFromAQuery : AbstractRuleChecker
    {
        private static HashSet<string> _baseControllers = new HashSet<string>()
        {
            "System.Web.Mvc.Controller",
            "Microsoft.AspNetCore.Mvc.Controller",
            "System.Web.Mvc.AsyncController"
        };
        private static HashSet<INamedTypeSymbol> _controllerSymbols = new HashSet<INamedTypeSymbol>();
        private static INamedTypeSymbol _dbDataReader = null;
        private SyntaxNodeAnalysisContext _context;
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
            _dbDataReader = context.Compilation.GetTypeByMetadataName("System.Data.Common.DbDataReader") as INamedTypeSymbol;
        }

        private bool IsRightResultFromQuery(ElementAccessExpressionSyntax eltAccess)
        {
            var expressionSymbolInfo = _context.SemanticModel.GetTypeInfo(eltAccess.Expression);
            var expressionSymbol = expressionSymbolInfo.Type;
            if (expressionSymbol == null)
                return false;
            if (expressionSymbol.IsOrInheritsFrom(_dbDataReader))
                return true;
            return false;
        }

        private bool IsRightResultFromQuery(InvocationExpressionSyntax invoc)
        {
            var symbolInfo = _context.SemanticModel.GetSymbolInfo(invoc.Expression);
            var symb = symbolInfo.Symbol;
            if (symb == null)
                return false;
            var expressionSymbol = symb.ContainingType;
            if (expressionSymbol.IsOrInheritsFrom(_dbDataReader))
                return true;
            return false;
        }

        private bool IsRightResultFromQuery(IdentifierNameSyntax identifName)
        {
            var symbol = _context.SemanticModel.GetSymbolInfo(identifName).Symbol;
            if (symbol == null)
                return false;
            foreach (var syntaxRef in symbol.DeclaringSyntaxReferences)
            {
                var node = syntaxRef.GetSyntax();
                var declaratorNode = node as VariableDeclaratorSyntax;
                if (declaratorNode == null)
                    continue;

                var initializer = declaratorNode.Initializer as EqualsValueClauseSyntax;
                if (initializer == null)
                    continue;
                bool isQueryRes = false;
                switch (initializer.Value.Kind())
                {
                    case SyntaxKind.ElementAccessExpression:
                        var eltAcc = initializer.Value as ElementAccessExpressionSyntax;
                        isQueryRes = IsRightResultFromQuery(eltAcc);
                        break;
                    case SyntaxKind.InvocationExpression:
                        var invoc = initializer.Value as InvocationExpressionSyntax;
                        isQueryRes = IsRightResultFromQuery(invoc);
                        break;
                    case SyntaxKind.IdentifierName:
                        var ident = initializer.Value as IdentifierNameSyntax;
                        isQueryRes = IsRightResultFromQuery(ident);
                        break;
                    default:
                        break;
                }

                if (isQueryRes)
                    return true;
            }
            return false;
        }

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            try
            {
                _context = context;
                var node = context.Node as AssignmentExpressionSyntax;
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

                var left = node.Left as MemberAccessExpressionSyntax;
                if (left == null)
                    return;

                var leftExpression = left.Expression as IdentifierNameSyntax;
                if (leftExpression == null 
                    || leftExpression.Identifier.ValueText != "ViewBag")
                    return;

                bool isQueryResult = false;


                switch(node.Right.Kind())
                {
                    case SyntaxKind.ElementAccessExpression:
                        var eltAcc = node.Right as ElementAccessExpressionSyntax;
                        isQueryResult = IsRightResultFromQuery(eltAcc);
                        break;
                    case SyntaxKind.InvocationExpression:
                        var invoc = node.Right as InvocationExpressionSyntax;
                        isQueryResult = IsRightResultFromQuery(invoc);
                        break;
                    case SyntaxKind.IdentifierName:
                        var ident = node.Right as IdentifierNameSyntax;
                        isQueryResult = IsRightResultFromQuery(ident);
                        break;
                    default:
                        break;
                }

                if (!isQueryResult)
                    return;
                
                var pos = node.GetLocation().GetMappedLineSpan();
                AddViolation(containingSymbol, new[] { pos });

            }
            catch (Exception e)
            {
                Log.Warn(" Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath + ": " + context.Node.GetLocation().GetMappedLineSpan(), e);
            }
        }
    }
}
