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
       Id = "EI_EnsureToSetBothEncryptAndTrustServerCertificateToTrueWhenConnectingToSqlServer",
       Title = "Ensure to set both 'Encrypt' and 'TrustServerCertificate' to true when connecting to a SQL Server",
       MessageFormat = "Ensure to set both 'Encrypt' and 'TrustServerCertificate' to true when connecting to a SQL Server",
       Category = "Secure Coding - Weak Security Features",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.EnsureToSetBothEncryptAndTrustServerCertificateToTrueWhenConnectingToSqlServer"
   )]
    public class EnsureToSetBothEncryptAndTrustServerCertificateToTrueWhenConnectingToSqlServer : AbstractRuleChecker
    {
        private static string _sqlConnection = "System.Data.SqlClient.SqlConnection";
        private static string _sqlConnectionStringBuilder = "System.Data.SqlClient.SqlConnectionStringBuilder";
        private static Regex _regEncrypt = new Regex(@"Encrypt=true", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10));
        private static Regex _regCertificate = new Regex(@"TrustServerCertificate=true", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10));

        /// <summary>
        /// Initialize the QR with the given context and register all the syntax nodes
        /// to listen during the visit and provide a specific callback for each one
        /// </summary>
        /// <param name="context"></param>
        public override void Init(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ObjectCreationExpression);
        }

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            try
            {
                var model = context.SemanticModel;

                var objectCreationNode = context.Node as ObjectCreationExpressionSyntax;
                if (objectCreationNode != null)
                {
                    if (objectCreationNode.ArgumentList == null
                        || objectCreationNode.ArgumentList.Arguments.Count == 0) // no argument
                        return;
                    var conStringSymbInfo = model.GetSymbolInfo(objectCreationNode.ArgumentList.Arguments.First().Expression);
                    if (conStringSymbInfo.Symbol != null)
                    {
                        if (conStringSymbInfo.Symbol.Kind == SymbolKind.Field)
                            return;
                    }
                }

                var objCreationType = model.GetTypeInfo(context.Node).Type as INamedTypeSymbol;
                if (objCreationType == null)
                    return;

                if (_sqlConnection != objCreationType.ToString())
                    return;

                SyntaxNode containingMethod = context.Node.Parent;
                while (containingMethod != null && !containingMethod.IsKind(SyntaxKind.MethodDeclaration))
                {
                    containingMethod = containingMethod.Parent;
                }
                if (containingMethod == null)
                    return;

                var methodSymbol = model.GetDeclaredSymbol(containingMethod);
                if (methodSymbol == null)
                    return;

                var assignmentNodes = containingMethod.DescendantNodes().OfType<AssignmentExpressionSyntax>();
                var isEncryptTrue = false;
                var isCertificateTrue = false;
                foreach (var assignment in assignmentNodes)
                {
                    // Check if the right part of the assignment is "true"
                    var right = assignment.Right;
                    bool isTrueLiteralExpression = right.Kind() == SyntaxKind.TrueLiteralExpression;
                    bool isOneNumericalValue = false;
                    bool isTrueStringLiteralExpression = false;
                    if (right.Kind() == SyntaxKind.NumericLiteralExpression)
                    {
                        var num = right as LiteralExpressionSyntax;
                        if (num.Token.ValueText == "1")
                            isOneNumericalValue = true;
                    }
                    else if (right.Kind() == SyntaxKind.StringLiteralExpression)
                    {
                        var str = right as LiteralExpressionSyntax;
                        if (str.Token.ValueText.ToLower() == "true")
                            isTrueStringLiteralExpression = true;
                        else if (_regEncrypt.IsMatch(str.Token.ValueText) 
                            || _regCertificate.IsMatch(str.Token.ValueText))
                            isTrueStringLiteralExpression = true;
                    }

                    if (isTrueStringLiteralExpression
                        || isOneNumericalValue
                        || isTrueLiteralExpression)
                    {
                        // Check the left part of the assignment
                        var left = assignment.Left;
                        if (left is ElementAccessExpressionSyntax)
                        {
                            var elementAccess = left as ElementAccessExpressionSyntax;
                            var argList = elementAccess.ArgumentList as BracketedArgumentListSyntax;
                            if (argList != null && argList.Arguments.Count == 1)
                            {
                                var argument = argList.Arguments.First().Expression as LiteralExpressionSyntax;
                                if (argument != null && argument.Kind() == SyntaxKind.StringLiteralExpression)
                                {
                                    if (argument.Token.ValueText == "Encrypt")
                                    {
                                        isEncryptTrue = true;
                                        continue;
                                    }
                                    else if (argument.Token.ValueText == "TrustServerCertificate")
                                    {
                                        isCertificateTrue = true;
                                        continue;
                                    }
                                }
                            }
                        }
                        else if (left is MemberAccessExpressionSyntax)
                        {
                            var memberAccess = left as MemberAccessExpressionSyntax;
                            var nameMember = memberAccess.Name as IdentifierNameSyntax;
                            if(nameMember != null)
                            {
                                if(nameMember.Identifier.ValueText == "Encrypt")
                                {
                                    isEncryptTrue = true;
                                    continue;
                                }
                                else if (nameMember.Identifier.ValueText == "TrustServerCertificate")
                                {
                                    isCertificateTrue = true;
                                    continue;
                                }
                            }
                        }
                    }

                   
                }

                if (isEncryptTrue && isCertificateTrue)
                    return;//  both 'Encrypt' and 'TrustServerCertificate' are set to true so no violation

                var eqValNodes = containingMethod.DescendantNodes().OfType<EqualsValueClauseSyntax>();
                foreach (var eqValNode in eqValNodes)
                {
                    var eqVal = eqValNode as EqualsValueClauseSyntax;
                    if (eqVal.Value.Kind() == SyntaxKind.StringLiteralExpression)
                    {
                        var str = eqVal.Value as LiteralExpressionSyntax;
                        if (_regEncrypt.IsMatch(str.Token.ValueText) && _regCertificate.IsMatch(str.Token.ValueText))
                            return;
                    }
                }

                var pos = context.Node.GetLocation().GetMappedLineSpan();
                AddViolation(methodSymbol, new[] { pos });

            }
            catch (Exception e)
            {
                Log.Warn(" Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath + ": " + context.Node.GetLocation().GetMappedLineSpan(), e);
            }
        }
    }
}
