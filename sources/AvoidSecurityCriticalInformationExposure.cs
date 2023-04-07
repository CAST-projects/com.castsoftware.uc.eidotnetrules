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
using Roslyn.DotNet.Common;

namespace CastDotNetExtension
{
    [CastRuleChecker]
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [RuleDescription(
       Id = "EI_AvoidSecurityCriticalInformationExposure",
       Title = "Avoid security-critical information exposure",
       MessageFormat = "Avoid security-critical information exposure",
       Category = "Secure Coding - Weak Security Features",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.AvoidSecurityCriticalInformationExposure"
   )]
    public class AvoidSecurityCriticalInformationExposure : AbstractRuleChecker
    {
        private string _attributeName = "System.Security.SecurityCriticalAttribute";
        
        private static readonly HashSet<string> _loggingMethodNames = new HashSet<string>
        {
         "WriteLine",
         "Write",
         "Log",
         "Info",
         "InfoFormat",
         "Warn",
         "WarnFormat",
         "Error",
         "ErrorFormat",
         "Debug",
         "DebugFormat",
        };

        public AvoidSecurityCriticalInformationExposure():
            base (ViolationCreationMode.ViolationWithAdditionalBookmarks)
        {
            
        }
        /// <summary>
        /// Initialize the QR with the given context and register all the syntax nodes
        /// to listen during the visit and provide a specific callback for each one
        /// </summary>
        /// <param name="context"></param>
        public override void Init(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);  
        }

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            try
            {
                var invocation = context.Node as InvocationExpressionSyntax;
                if (invocation == null)
                    return;

                var expression = invocation.Expression as MemberAccessExpressionSyntax;
                if (expression == null)
                    return;

                if (!_loggingMethodNames.Contains(expression.Name.Identifier.ValueText))
                    return;

                // Check the invoked method arguments
                var argumentList = invocation.ArgumentList.Arguments;
                foreach(var arg in argumentList)
                {
                    var identifierList = arg.DescendantNodes().OfType<IdentifierNameSyntax>();
                    foreach(var identifier in identifierList)
                    {
                        var identifierSymb = context.SemanticModel.GetSymbolInfo(identifier).Symbol;
                        if(identifierSymb != null)
                        {
                            if(identifierSymb.Kind == SymbolKind.Property)
                            {
                                var propertySymbol = identifierSymb as IPropertySymbol;
                                identifierSymb = propertySymbol.GetMethod;
                                if (identifierSymb == null)
                                    continue;
                            }
                            var attributes = identifierSymb.GetAttributes();
                            foreach(var attribute in attributes)
                            {
                                var attributeName = attribute.ToString();
                                if(attributeName == _attributeName)
                                {
                                    var pos = invocation.GetLocation().GetMappedLineSpan();
                                    AddViolation(context.ContainingSymbol, new[] { pos });
                                }
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
