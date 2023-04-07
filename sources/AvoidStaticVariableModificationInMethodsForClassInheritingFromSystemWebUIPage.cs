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
       Id = "EI_AvoidStaticVariableModificationInMethodsForClassInheritingFromSystemWebUIPage",
       Title = "Avoid static variable modification in methods for class inheriting from  System.Web.UI.Page",
       MessageFormat = "Avoid static variable modification in methods for class inheriting from  System.Web.UI.Page",
       Category = "Secure Coding - Time and State",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.AvoidStaticVariableModificationInMethodsForClassInheritingFromSystemWebUIPage"
   )]
    public class AvoidStaticVariableModificationInMethodsForClassInheritingFromSystemWebUIPage : AbstractRuleChecker
    {
        private static string _page = "System.Web.UI.Page";
        private static INamedTypeSymbol _pageSymbol = null;
        /// <summary>
        /// Initialize the QR with the given context and register all the syntax nodes
        /// to listen during the visit and provide a specific callback for each one
        /// </summary>
        /// <param name="context"></param>
        public override void Init(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(OnCompilationStart);
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ClassDeclaration);
        }

        private void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            _pageSymbol = context.Compilation.GetTypeByMetadataName(_page);
        }

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            try
            {
                if (_pageSymbol == null)
                    return;

                var classSymbol = context.ContainingSymbol as INamedTypeSymbol;
                if (classSymbol == null)
                    return;

                if (!classSymbol.IsOrInheritsFrom(_pageSymbol))
                    return;

                var descendantNodes = context.Node.DescendantNodes();
                var fieldDeclarations = descendantNodes.OfType<FieldDeclarationSyntax>();
                var staticFields = fieldDeclarations
                    .Where(_ => _.Modifiers.Select(x => x.ValueText).Contains("static"));
                if (!staticFields.Any())
                    return; // Not any static field
                var staticFieldsNames = new List<string>();
                foreach(var staticField in staticFields)
                {
                    foreach(var variableDeclarator in staticField.Declaration.Variables)
                    {
                        staticFieldsNames.Add(variableDeclarator.Identifier.ValueText);
                    }
                }

                var assignmentNodes = descendantNodes.OfType<AssignmentExpressionSyntax>();
                var assignmentStaticFields = new List<AssignmentExpressionSyntax>();
                foreach(var assignmentNode in assignmentNodes)
                {
                    var identifierName = assignmentNode.Left as IdentifierNameSyntax;
                    if(identifierName!=null)
                    {
                        if(staticFieldsNames.Contains(identifierName.Identifier.ValueText))
                        {
                            assignmentStaticFields.Add(assignmentNode);
                        }
                    }
                }


                foreach (var field in assignmentStaticFields)
                {
                    bool hasLock = false;
                    bool hasMonitor = false;
                    bool hasMutex = false;
                    SyntaxNode parent = field.Parent;
                    while(parent!=null && !(parent is MethodDeclarationSyntax))
                    {
                        if (parent is LockStatementSyntax)
                            hasLock = true;
                        parent = parent.Parent;
                    }
                    if (hasLock)
                        continue;
                    if (parent == null)
                        continue;
                    
                    foreach(var node in parent.DescendantNodes())
                    {
                        if (node == field)
                            break;

                        var invocationNode = node as InvocationExpressionSyntax;
                        if (invocationNode == null)
                            continue;
                        var accessMember = invocationNode.Expression as MemberAccessExpressionSyntax;
                        if (accessMember == null)
                            continue;
                        if (accessMember.Name.Identifier.ValueText == "WaitOne")
                            hasMutex = true;
                        else if(accessMember.Name.Identifier.ValueText == "Enter")
                        {
                            var identifier = accessMember.Expression as IdentifierNameSyntax;
                            if(identifier != null)
                            {
                                if (identifier.Identifier.ValueText == "Monitor")
                                    hasMonitor = true;
                            }
                        }
                    }

                    if (hasMonitor)
                        continue;
                    if (hasMutex)
                        continue;

                    var methodSymbol = context.SemanticModel.GetDeclaredSymbol(parent);
                    var pos = field.GetLocation().GetMappedLineSpan();
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
