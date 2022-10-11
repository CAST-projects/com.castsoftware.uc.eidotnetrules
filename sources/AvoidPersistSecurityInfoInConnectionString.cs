using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.DotNet.CastDotNetExtension;
using System.Data.Common;
using CastDotNetExtension.Utils;

namespace CastDotNetExtension
{
    [CastRuleChecker]
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [RuleDescription(
       Id = "EI_AvoidPersistSecurityInfoInConnectionString",
       Title = "Avoid PersistSecurity Info In Connection String",
       MessageFormat = "Avoid PersistSecurity Info In Connection String",
       Category = "Secure Coding - Weak Security Features",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.AvoidPersistSecurityInfoInConnectionString"
   )]
    public class AvoidPersistSecurityInfoInConnectionString : AbstractRuleChecker
    {
        
        /// <summary>
        /// Initialize the QR with the given context and register all the syntax nodes
        /// to listen during the visit and provide a specific callback for each one
        /// </summary>
        /// <param name="context"></param>
        public override void Init(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
        {
            try
            {                
                var assignment = context.Node as AssignmentExpressionSyntax;
                if (assignment != null)
                {
                    // Check if the right part of the assignment is "true"
                    var right = assignment.Right;
                    if (right.Kind() != SyntaxKind.TrueLiteralExpression)
                        return;
                    // Check the left part of the assignment
                    var left = assignment.Left;
                    if (left is ElementAccessExpressionSyntax)
                    {
                        var elementAccess = left as ElementAccessExpressionSyntax;
                        var argList = elementAccess.ArgumentList as BracketedArgumentListSyntax;
                        if(argList!= null && argList.Arguments.Count == 1)
                        {
                            var argument = argList.Arguments.First().Expression as LiteralExpressionSyntax;
                            if(argument != null && argument.Kind() == SyntaxKind.StringLiteralExpression)
                            {
                                if(argument.Token.ValueText == "Persist Security Info")
                                {
                                    var pos = assignment.GetLocation().GetMappedLineSpan();
                                    AddViolation(context.ContainingSymbol, new[] { pos });
                                }
                            }
                        }
                        
                    }
                    else if(left is MemberAccessExpressionSyntax)
                    {
                        var memberAccess = left as MemberAccessExpressionSyntax;
                        var accessName = memberAccess.Name as IdentifierNameSyntax;
                        if(accessName != null)
                        {
                            if(accessName.Identifier.ValueText == "PersistSecurityInfo")
                            {
                                var pos = assignment.GetLocation().GetMappedLineSpan();
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

        private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            try
            {
                var invocation = context.Node as InvocationExpressionSyntax;
                if (invocation != null)
                {
                    var expression = invocation.Expression as MemberAccessExpressionSyntax;
                    if (expression == null)
                        return;

                    // Check the invoked method name is "Add"
                    var invoked = expression.Name as IdentifierNameSyntax;
                    if (invoked == null || invoked.Identifier.ValueText != "Add")
                        return;  

                    // Check the invoked method arguments
                    var argumentList = invocation.ArgumentList.Arguments;
                    if (argumentList.Count != 2) // exactly 2 arguments
                        return;
                    if (argumentList[1].Expression.Kind() != SyntaxKind.TrueLiteralExpression) // second argument must be "true"
                        return;
                    var firstArgument = argumentList[0].Expression as LiteralExpressionSyntax;
                    if (firstArgument.Token.ValueText == "PersistSecurityInfo" || firstArgument.Token.ValueText == "Persist Security Info")
                    {
                        var pos = invocation.GetLocation().GetMappedLineSpan();
                        AddViolation(context.ContainingSymbol, new[] { pos });
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
