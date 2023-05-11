using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
       Id = "EI_AvoidExposedDangerousMethod",
       Title = "Avoid exposed dangerous method",
       MessageFormat = "Avoid exposed dangerous method",
       Category = "Secure Coding - Weak Security Features",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.AvoidExposedDangerousMethod"
   )]
    public class AvoidExposedDangerousMethod : AbstractRuleChecker
    {
        private static string _DbCommandName = "System.Data.Common.DbCommand";
        private static INamedTypeSymbol _DbCommandSymbol = null;
        private static HashSet<string> _executeQueryMethods = new HashSet<string>
        {
            "ExecuteReader", "ExecuteReaderAsync", "ExecuteScalar", "ExecuteScalarAsync",
            "ExecuteXmlReader", "ExecuteXmlReaderAsync", "BeginExecuteReader",
            "BeginExecuteXml", "ExecuteDbDataReader", "ExecuteDbDataReaderAsync"
        };

        private static Regex _dropQuery = new Regex(@"drop\s*(database|table|column|view)", RegexOptions.None, TimeSpan.FromSeconds(120));
        private static Regex _deleteQuery = new Regex(@"delete\s*from", RegexOptions.None, TimeSpan.FromSeconds(120));

        private IEnumerable<VariableDeclaratorSyntax> _declaratorNodes = null;
        private IEnumerable<AssignmentExpressionSyntax> _assignmentNodes = null;
        private SyntaxNodeAnalysisContext _currentcontext;

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

        private void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            _DbCommandSymbol = context.Compilation.GetTypeByMetadataName(_DbCommandName);
        }

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            try
            {
                _currentcontext = context;
                bool hasViolation = false;
                var containingSymbol = context.ContainingSymbol as IMethodSymbol;
                if (containingSymbol == null)
                    return;
                if (containingSymbol.DeclaredAccessibility != Accessibility.Public)
                    return; // Not a public method so no possible violation

                var invocationNode = context.Node as InvocationExpressionSyntax;
                if (invocationNode == null)
                    return;
                var accessMember = invocationNode.Expression as MemberAccessExpressionSyntax;
                if (accessMember == null)
                    return;
                if (!_executeQueryMethods.Contains(accessMember.Name.Identifier.ValueText))
                    return; // not an execution of a query

                var containingMethod = containingSymbol.DeclaringSyntaxReferences.FirstOrDefault();
                if (containingMethod == null)
                    return;
                var containingMethodNode = containingMethod.GetSyntax();

                _declaratorNodes = containingMethodNode.DescendantNodes().OfType<VariableDeclaratorSyntax>();
                _assignmentNodes = containingMethodNode.DescendantNodes().OfType<AssignmentExpressionSyntax>();

                var dbCommandVar = accessMember.Expression as IdentifierNameSyntax;
                if (dbCommandVar != null)
                { 

                    var symbType = context.SemanticModel.GetTypeInfo(dbCommandVar).Type;
                    if (symbType == null || !symbType.IsOrInheritsFrom(_DbCommandSymbol))
                        return; // Not a DbCommand

                    var dbCommandVarDeclarator = _declaratorNodes.Where(_ => _.Identifier.ValueText == dbCommandVar.Identifier.ValueText).FirstOrDefault();
                    if (dbCommandVarDeclarator!=null 
                        && dbCommandVarDeclarator.Initializer != null 
                        && !(dbCommandVarDeclarator.Initializer.Value is LiteralExpressionSyntax))
                    {
                        hasViolation = AnalyseDeclarator(dbCommandVarDeclarator, true);
                    }
                    else
                    {
                        var dbCommandAssignment = _assignmentNodes.Where(_ => 
                                {
                                    var left = _.Left as IdentifierNameSyntax;
                                    if (left == null)
                                        return false;
                                    return left.Identifier.ValueText == dbCommandVar.Identifier.ValueText;
                                }
                            ).FirstOrDefault();
                        if (dbCommandAssignment!= null && dbCommandAssignment.Right is ObjectCreationExpressionSyntax)
                        {
                            var objectCreationNode = dbCommandAssignment.Right as ObjectCreationExpressionSyntax;
                            hasViolation = AnalyseObjectCreation(objectCreationNode, true);
                        }
                    }
                }
                else
                {
                    var objectCreationNode = accessMember.Expression.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().FirstOrDefault();
                    if (objectCreationNode != null)
                    {
                        var symbType = context.SemanticModel.GetTypeInfo(objectCreationNode).Type;
                        if (symbType == null || !symbType.IsOrInheritsFrom(_DbCommandSymbol))
                            return; // Not a DbCommand

                        hasViolation = AnalyseObjectCreation(objectCreationNode, true);
                    }
                }
                
                if (hasViolation)
                {
                    var pos = context.Node.GetLocation().GetMappedLineSpan();
                    AddViolation(context.ContainingSymbol, new[] { pos });
                }
            }
            catch (Exception e)
            {
                Log.Warn(" Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath + ": " + context.Node.GetLocation().GetMappedLineSpan(), e);
            }
        }

        private bool AnalyseDeclarator(VariableDeclaratorSyntax declaratorNode, bool isDbCommand = false)
        {
            if (isDbCommand && declaratorNode.Initializer != null)
            {
                var objectCreationNode = declaratorNode.Initializer.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().FirstOrDefault();
                if (objectCreationNode != null)
                {
                    return AnalyseObjectCreation(objectCreationNode, true);
                }
            }
            else
            {
                if (declaratorNode.Initializer != null && DispatchDescendantNodes(declaratorNode.Initializer.DescendantNodes()))
                    return true;
            }
            return false;
        }

        private bool DispatchDescendantNodes(IEnumerable<SyntaxNode> descendantNodes)
        {
            foreach (var descendant in descendantNodes)
            {
                if (descendant is LiteralExpressionSyntax)
                {
                    if (AnalyseLiteralExpression(descendant as LiteralExpressionSyntax))
                        return true;
                }
                else if (descendant is IdentifierNameSyntax)
                {
                    if (AnalyseIdentifierName(descendant as IdentifierNameSyntax))
                        return true;
                }
                else if (descendant is ObjectCreationExpressionSyntax)
                {
                    if (AnalyseObjectCreation(descendant as ObjectCreationExpressionSyntax))
                        return true;
                }
            }
            return false;
        }

        private bool AnalyseObjectCreation(ObjectCreationExpressionSyntax objectCreationNode, bool isDbCommand=false)
        {
            if (isDbCommand)
            {
                var arg = objectCreationNode.ArgumentList.Arguments.FirstOrDefault();
                if (arg != null)
                {
                    if (DispatchDescendantNodes(arg.DescendantNodesAndSelf()))
                        return true;
                }
            }
            else
            {
                foreach(var args in objectCreationNode.ArgumentList.Arguments)
                {
                    if (DispatchDescendantNodes(args.DescendantNodesAndSelf()))
                        return true;
                }
            }
            return false;
        }

        private bool AnalyseIdentifierName(IdentifierNameSyntax identifierNode)
        {
            var declaration = _declaratorNodes.Where(_ => _.Identifier.ValueText == identifierNode.Identifier.ValueText).FirstOrDefault();
            if (declaration != null)
            {
                if (AnalyseDeclarator(declaration))
                    return true;
            }
            var assignments = _assignmentNodes.Where(_ =>
                    {
                        var left = _.Left as IdentifierNameSyntax;
                        if (left == null)
                            return false;
                        return left.Identifier.ValueText == identifierNode.Identifier.ValueText;
                    }
                );
            foreach (var assignment in assignments)
            {
                if (DispatchDescendantNodes(assignment.Right.DescendantNodesAndSelf()))
                    return true;
            }
            return false;
        }

        private bool AnalyseLiteralExpression(LiteralExpressionSyntax literalNode)
        {
            if (literalNode.Kind()==SyntaxKind.StringLiteralExpression)
            {
                if (_dropQuery.IsMatch(literalNode.ToString().ToLower()) 
                    || _deleteQuery.IsMatch(literalNode.ToString().ToLower()))
                    return true;
            }
            return false;
        }

    }
}
