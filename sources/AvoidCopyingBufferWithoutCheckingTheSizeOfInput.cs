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
       Id = "EI_AvoidCopyingBufferWithoutCheckingTheSizeOfInput",
       Title = "Avoid copying buffer without checking the size of input",
       MessageFormat = "Avoid copying buffer without checking the size of input",
       Category = "Secure Coding - Input Validation",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.AvoidCopyingBufferWithoutCheckingTheSizeOfInput"
   )]
    public class AvoidCopyingBufferWithoutCheckingTheSizeOfInput: AbstractRuleChecker
    {
        /// <summary>
        /// Initialize the QR with the given context and register all the syntax nodes
        /// to listen during the visit and provide a specific callback for each one
        /// </summary>
        /// <param name="context"></param>
        public override void Init(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(OnCompilationStart);
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
        }

        private INamedTypeSymbol _bufferSymbol = null;

        private void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            _bufferSymbol = context.Compilation.GetTypeByMetadataName("System.Buffer") as INamedTypeSymbol;
        }

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            /*lock (_lock)*/
            {
                try
                {
                    if (_bufferSymbol == null)
                        return;
                    
                    var invocationNode = context.Node as InvocationExpressionSyntax;
                    if (invocationNode == null)
                        return;

                    var expressionNode = invocationNode.Expression as MemberAccessExpressionSyntax;
                    if (expressionNode == null)
                        return;

                    var invocationIdentifier = expressionNode.Name.Identifier.ValueText;
                    if (invocationIdentifier != "BlockCopy" && invocationIdentifier != "MemoryCopy")
                        return;

                    var expressionTypeSymbol = context.SemanticModel.GetTypeInfo(expressionNode.Expression).Type;
                    if (expressionTypeSymbol == null || !expressionTypeSymbol.IsOrInheritsFrom(_bufferSymbol))
                        return;
                    
                    int argNum = 0;
                    if(invocationIdentifier == "BlockCopy")
                        argNum = 5;
                    else
                        argNum = 4;

                    var arguments = invocationNode.ArgumentList.Arguments;
                    if (arguments.Count < argNum)
                        return;

                    var bufferLen = arguments[argNum-1].Expression as IdentifierNameSyntax;
                    if (bufferLen == null)
                        return;
                    var bufLength = bufferLen.Identifier.ValueText;

                    var methodSymbol = context.ContainingSymbol;
                    if (methodSymbol.Kind != SymbolKind.Method)
                        return;

                    var methodNode = methodSymbol.DeclaringSyntaxReferences.First().GetSyntax();
                    var ifComparison = methodNode
                        .DescendantNodes()
                        .OfType<IfStatementSyntax>()
                        .SelectMany(_ => _.DescendantNodes().OfType<IdentifierNameSyntax>())
                        .Select(_ => _.Identifier.ValueText);

                    if(!ifComparison.Contains(bufLength))
                    {
                        var pos = invocationNode.GetLocation().GetMappedLineSpan();
                        AddViolation(methodSymbol, new[] { pos });
                    }
                    
                }
                catch (Exception e)
                {
                    Log.Warn(" Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath + ": " + context.Node.GetLocation().GetMappedLineSpan(), e);
                }
            }
        }
    }
}
