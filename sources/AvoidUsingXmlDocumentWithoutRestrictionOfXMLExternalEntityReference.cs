using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Text.RegularExpressions;
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
       Id = "EI_AvoidUsingXmlDocumentWithoutRestrictionOfXMLExternalEntityReference",
       Title = "Avoid using XmlDocument without restriction of XML External Entity Reference",
       MessageFormat = "Avoid using XmlDocument without restriction of XML External Entity Reference",
       Category = "Secure Coding - Input Validation",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.AvoidUsingXmlDocumentWithoutRestrictionOfXMLExternalEntityReference"
   )]
    public class AvoidUsingXmlDocumentWithoutRestrictionOfXMLExternalEntityReference : AbstractRuleChecker
    {
        
        private string _XmlDocument = "XmlDocument";
        private string _XmlResolver = "XmlResolver";

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
                

                var descendantNodes = context.Node.DescendantNodes();
                var objectCreationNodes = descendantNodes.OfType<ObjectCreationExpressionSyntax>();
                var possibleViolatingNodes = new Dictionary<string, ObjectCreationExpressionSyntax>();
                foreach (var node in objectCreationNodes)
                {
                    var identifierName = node.Type as IdentifierNameSyntax;
                    if (identifierName == null)
                        continue;
                   
                    if (identifierName.Identifier.ValueText == _XmlDocument)
                    {
                        string varIdentifierName;
                        if (node.GetAssociatedVariableName(out varIdentifierName))
                            {
                            possibleViolatingNodes[varIdentifierName] = node;
                        }
                    }
                }

                var memberAccessNodes = descendantNodes
                    .OfType<MemberAccessExpressionSyntax>()
                    .Where(_ => _.Parent.IsKind(SyntaxKind.SimpleAssignmentExpression));
                var modifyingXmlResolverNodes = new Dictionary<string, MemberAccessExpressionSyntax>();
                
                foreach (var memberAccessNode in memberAccessNodes)
                {
                    if(memberAccessNode.Name.Identifier.ValueText == _XmlResolver)
                    {
                        var expression = memberAccessNode.Expression as IdentifierNameSyntax;
                        if (expression == null)
                            continue;
                        if(possibleViolatingNodes.ContainsKey(expression.Identifier.ValueText))
                        {
                            modifyingXmlResolverNodes[expression.Identifier.ValueText] = memberAccessNode;
                        }
                    }
                }
                
                if (FrameworkVersion.currentNetFrameworkKind == NetFrameworkKind.NetFramework 
                    && FrameworkVersion.currentNetFrameworkVersion != null 
                    && FrameworkVersion.currentNetFrameworkVersion < new Version("4.5.2"))
                {
                    // violation if XmlResolver is not explicitly set to null
                    foreach (var variableName in possibleViolatingNodes.Keys)
                    {
                        if (!modifyingXmlResolverNodes.ContainsKey(variableName))
                        {
                            var pos = possibleViolatingNodes[variableName].GetLocation().GetMappedLineSpan();
                            AddViolation(context.ContainingSymbol, new[] { pos });
                        }
                        else
                        {
                            var parent = modifyingXmlResolverNodes[variableName].Parent as AssignmentExpressionSyntax;
                            if(parent != null && parent.Right.IsKind(SyntaxKind.NullLiteralExpression))
                            {
                                continue; //XmlResolver is explicitly set to null
                            }
                            var pos = possibleViolatingNodes[variableName].GetLocation().GetMappedLineSpan();
                            AddViolation(context.ContainingSymbol, new[] { pos });
                        }
                            
                    }
                }
                else
                {
                    // violation if XmlResolver is explicitly set
                    foreach (var variableName in possibleViolatingNodes.Keys)
                    {
                        if (modifyingXmlResolverNodes.ContainsKey(variableName))
                        {
                            var parent = modifyingXmlResolverNodes[variableName].Parent as AssignmentExpressionSyntax;
                            if (parent != null && !parent.Right.IsKind(SyntaxKind.NullLiteralExpression))
                            {
                                //XmlResolver is explicitly set
                                var pos = possibleViolatingNodes[variableName].GetLocation().GetMappedLineSpan();
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
