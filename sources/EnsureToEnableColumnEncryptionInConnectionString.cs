using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
       Id = "EI_EnsureToEnableColumnEncryptionInConnectionString",
       Title = "Ensure to enable column encryption in connection string",
       MessageFormat = "Ensure to enable column encryption in connection string",
       Category = "Secure Coding - Weak Security Features",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.EnsureToEnableColumnEncryptionInConnectionString"
   )]
    public class EnsureToEnableColumnEncryptionInConnectionString : AbstractRuleChecker
    {
        private static string _sqlConnection = "System.Data.SqlClient.SqlConnection";
        private static string _sqlConnectionStringBuilder = "System.Data.SqlClient.SqlConnectionStringBuilder";

        private static INamedTypeSymbol _sqlConnectionSymbol = null;
        private static INamedTypeSymbol _sqlConnectionStringBuilderSymbol = null;

        private static Regex _reg = new Regex(@"Column\s*Encryption\s*Setting\s*=\s*enabled");
        /// <summary>
        /// Initialize the QR with the given context and register all the syntax nodes
        /// to listen during the visit and provide a specific callback for each one
        /// </summary>
        /// <param name="context"></param>
        public override void Init(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(OnCompilationStart);
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ObjectCreationExpression);
        }

        private void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            _sqlConnectionSymbol = context.Compilation.GetTypeByMetadataName(_sqlConnection);
            _sqlConnectionStringBuilderSymbol = context.Compilation.GetTypeByMetadataName(_sqlConnectionStringBuilder);
        }

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            try
            {
                if (_sqlConnectionSymbol == null)
                    return;

                var model = context.SemanticModel;

                var objCreationType = model.GetTypeInfo(context.Node).Type as INamedTypeSymbol;
                if (objCreationType == null)
                    return;

                if (!SymbolEqualityComparer.Default.Equals(_sqlConnectionSymbol, objCreationType))
                    return;

                SyntaxNode containingMethod = context.Node.Parent;
                while(containingMethod != null && !containingMethod.IsKind(SyntaxKind.MethodDeclaration))
                {
                    containingMethod = containingMethod.Parent;
                }
                if (containingMethod == null)
                    return;

                var methodSymbol = model.GetDeclaredSymbol(containingMethod);
                if (methodSymbol == null)
                    return;

                var assignmentNodes = containingMethod.DescendantNodes().OfType<AssignmentExpressionSyntax>();

                foreach (var assignment in assignmentNodes)
                {
                    // Check if the right part of the assignment is "true"
                    var right = assignment.Right;
                    bool isTrueLiteralExpression = right.Kind() == SyntaxKind.TrueLiteralExpression;
                    bool isOneNumericalValue = false;
                    bool isEnableStringLiteralExpression = false;
                    if (right.Kind() == SyntaxKind.NumericLiteralExpression)
                    {
                        var num = right as LiteralExpressionSyntax;
                        if (num.Token.ValueText == "1")
                            isOneNumericalValue = true;
                    }
                    else if (right.Kind() == SyntaxKind.StringLiteralExpression)
                    {
                        var str = right as LiteralExpressionSyntax;
                        if (str.Token.ValueText.ToLower() == "enabled")
                            isEnableStringLiteralExpression = true;
                        else if(_reg.IsMatch(str.Token.ValueText))
                            isEnableStringLiteralExpression = true;
                    }

                    if(isEnableStringLiteralExpression 
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
                                    if (argument.Token.ValueText == "Column Encryption Setting")
                                    {
                                        return;
                                    }
                                }
                            }
                        }
                    }

                    if(right.Kind() == SyntaxKind.SimpleMemberAccessExpression)
                    {
                        var access = right as MemberAccessExpressionSyntax;
                        if(access.Expression.Kind() == SyntaxKind.IdentifierName &&
                            access.Name.Kind() == SyntaxKind.IdentifierName)
                        {
                            var accessExpression = access.Expression as IdentifierNameSyntax;
                            var accessName = access.Name as IdentifierNameSyntax;
                            if(accessExpression.Identifier.ValueText == "SqlConnectionColumnEncryptionSetting" &&
                                accessName.Identifier.ValueText == "Enabled")
                            {
                                return;
                            }
                        }
                    } 
                }

                var eqValNodes = containingMethod.DescendantNodes().OfType<EqualsValueClauseSyntax>();
                foreach(var eqValNode in eqValNodes)
                {
                    var eqVal = eqValNode as EqualsValueClauseSyntax;
                    if (eqVal.Value.Kind() == SyntaxKind.StringLiteralExpression)
                    {
                        var str = eqVal.Value as LiteralExpressionSyntax;
                        if (_reg.IsMatch(str.Token.ValueText))
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
