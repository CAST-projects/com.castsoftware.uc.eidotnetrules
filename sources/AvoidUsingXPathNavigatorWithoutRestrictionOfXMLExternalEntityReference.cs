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
       Id = "EI_AvoidUsingXPathNavigatorWithoutRestrictionOfXMLExternalEntityReference",
       Title = "Avoid using XPathNavigator without restriction of XML External Entity Reference",
       MessageFormat = "Avoid using XPathNavigator without restriction of XML External Entity Reference",
       Category = "Secure Coding - Input Validation",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.AvoidUsingXPathNavigatorWithoutRestrictionOfXMLExternalEntityReference"
   )]
    public class AvoidUsingXPathNavigatorWithoutRestrictionOfXMLExternalEntityReference : AbstractRuleChecker
    {
        private const string _XPathNavigator = "XPathNavigator";
        private const string _XPathDocument = "XPathDocument";
        private const string _XmlReader = "XmlReader";
        private const string _CreateNavigator = "CreateNavigator";

        /// <summary>
        /// Initialize the QR with the given context and register all the syntax nodes
        /// to listen during the visit and provide a specific callback for each one
        /// </summary>
        /// <param name="context"></param>
        public override void Init(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.MethodDeclaration);
        }



        private static readonly object _lock = new object();
        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            try
            {
                lock (_lock)
                {
                    FrameworkVersion.InitializedNetFramworkVersion(context);
                    if (FrameworkVersion.currentNetFrameworkKind == NetFrameworkKind.NetFramework && FrameworkVersion.currentNetFrameworkVersion != null)
                        Log.DebugFormat(".Net Framework version found : {0}", FrameworkVersion.currentNetFrameworkVersion.ToString());
                }

                if (FrameworkVersion.currentNetFrameworkKind != NetFrameworkKind.NetFramework)
                    return;

                if (FrameworkVersion.currentNetFrameworkVersion != null
                    && FrameworkVersion.currentNetFrameworkVersion >= new Version("4.5.2"))
                    return;

                var descendantNodes = context.Node.DescendantNodes();
                var invocationNodes = descendantNodes.OfType<InvocationExpressionSyntax>();

                var createNavigatorMemberAccessNodes = invocationNodes
                    .Select(_ => _.Expression as MemberAccessExpressionSyntax)
                    .Where(_ => _ != null && _.Name.Identifier.ValueText == _CreateNavigator)
                    .Where(_ => _.Expression is IdentifierNameSyntax);
                var model = context.SemanticModel;
                foreach (var node in createNavigatorMemberAccessNodes)
                {
                    // Find XpathDocument instance
                    var symbol = model.GetSymbolInfo(node.Expression).Symbol;
                    if (symbol == null)
                        continue;
                    
                    var varSyntaxReferences = symbol.DeclaringSyntaxReferences;
                    foreach (var syntaxReference in varSyntaxReferences)
                    {
                        var varSyntax = syntaxReference.GetSyntax() as VariableDeclaratorSyntax;
                        if (varSyntax == null)
                            continue;

                        var objCreation = varSyntax.Initializer.Value as ObjectCreationExpressionSyntax;
                        if (objCreation == null)
                            continue;

                        var identifierName = objCreation.Type as IdentifierNameSyntax;
                        if (identifierName == null || identifierName.Identifier.ValueText != _XPathDocument)
                            continue;

                        var argument = objCreation.ArgumentList.Arguments.First().Expression;
                        if (argument is LiteralExpressionSyntax)
                        {
                            var pos = node.Parent.GetLocation().GetMappedLineSpan();
                            AddViolation(context.ContainingSymbol, new[] { pos });
                        }
                        else if (argument is IdentifierNameSyntax) // check for safe parser
                        {
                            var argSymbol = model.GetSymbolInfo(argument).Symbol;
                            if (argSymbol == null)
                                continue;

                            bool isArgASafeParser = false;
                            var argSyntaxReferences = argSymbol.DeclaringSyntaxReferences;
                            foreach (var argSyntaxReference in argSyntaxReferences)
                            {
                                var varArgSyntax = argSyntaxReference.GetSyntax() as VariableDeclaratorSyntax;
                                if (varArgSyntax == null)
                                    continue;

                                var varInvocationExpression = varArgSyntax.Initializer.Value as InvocationExpressionSyntax;
                                if (varInvocationExpression == null)
                                    continue;

                                var varAccessMember = varInvocationExpression.Expression as MemberAccessExpressionSyntax;
                                if (varAccessMember == null)
                                    continue;

                                var varIdentifierName = varAccessMember.Expression as IdentifierNameSyntax;
                                if (varIdentifierName != null && varIdentifierName.Identifier.ValueText == _XmlReader)
                                {
                                    isArgASafeParser = true;
                                    break;
                                }
                            }

                            if (!isArgASafeParser)
                            {
                                var pos = node.Parent.GetLocation().GetMappedLineSpan();
                                AddViolation(context.ContainingSymbol, new[] { pos });
                            }
                        }
                    }

                    
                }
               
            }
            catch (Exception e)
            {
                Log.Warn(" Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath + ": " + context.Node.GetLocation().GetMappedLineSpan(), e);
            }
        }
    }
}
