using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.DotNet.CastDotNetExtension;


namespace CastDotNetExtension
{
    [CastRuleChecker]
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [RuleDescription(
        Id = "EI_UseLogicalORInsteadOfBitwiseORInBooleanContext",
        Title = "Use Logical OR and AND instead of Bitwise OR and AND in boolean context",
        MessageFormat = "Use Logical OR and AND instead of Bitwise OR and AND in boolean context",
        Category = "Programming Practices - Unexpected Behavior",
        DefaultSeverity = DiagnosticSeverity.Warning,
        CastProperty = "EIDotNetQualityRules.UseLogicalORInsteadOfBitwiseORInBooleanContext"
    )]
    public class UseLogicalORandANDInsteadOfBitwiseORandANDInBooleanContext : AbstractRuleChecker
    {

        /// <summary>
        /// Initialize the QR with the given context and register all the syntax nodes
        /// to listen during the visit and provide a specific callback for each one
        /// </summary>
        /// <param name="context"></param>
        public override void Init(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(OnCompilationStart);
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.BitwiseOrExpression);
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.BitwiseAndExpression);
        }

        private INamedTypeSymbol _booleanType = null;

        private void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            _booleanType = context.Compilation.GetTypeByMetadataName("System.Boolean") as INamedTypeSymbol;
        }

        private readonly object _lock = new object();
        private void Analyze(SyntaxNodeAnalysisContext context)
        {

            /*lock (_lock)*/
            {
                try
                {
                    var expr = context.Node as BinaryExpressionSyntax;
                    if (null != expr)
                    {
                        if (null != expr.Left && context.SemanticModel.GetTypeInfo(expr.Left).Type.Equals(_booleanType) ||
                        null != expr.Right && context.SemanticModel.GetTypeInfo(expr.Right).Type.Equals(_booleanType))
                        {
                            var pos = expr.SyntaxTree.GetMappedLineSpan(expr.Span);
                            //Log.Warn(pos);
                            AddViolation(context.ContainingSymbol, new[] { pos });
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Log.Warn(" Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath + ": " + context.Node.GetLocation().GetMappedLineSpan(), e);
                }
            }
        }

    }
}
