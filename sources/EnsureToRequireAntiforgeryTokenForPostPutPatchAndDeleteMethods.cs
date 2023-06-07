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
       Id = "EI_EnsureToRequireAntiforgeryTokenForPostPutPatchAndDeleteMethods",
       Title = "Ensure to require anti-forgery token for POST, PUT, PATCH and DELETE methods",
       MessageFormat = "Ensure to require anti-forgery token for POST, PUT, PATCH and DELETE methods",
       Category = "Secure Coding - Weak Security Features",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.EnsureToRequireAntiforgeryTokenForPostPutPatchAndDeleteMethods"
   )]
    public class EnsureToRequireAntiforgeryTokenForPostPutPatchAndDeleteMethods : AbstractRuleChecker
    {
        private static HashSet<string> _baseControllers = new HashSet<string>()
        {
            "System.Web.Mvc.Controller",
            "Microsoft.AspNetCore.Mvc.Controller",
            "System.Web.Mvc.AsyncController"
        };

        private static HashSet<string> _antiforgeryTokenAttributes = new HashSet<string>()
        {
            "ValidateAntiForgeryTokenAttribute",
            "AutoValidateAntiforgeryTokenAttribute",
            "IgnoreAntiforgeryTokenAttribute"
        };

        private static HashSet<string> _httpMessages = new HashSet<string>()
        {
            "HttpPostAttribute",
            "HttpPutAttribute",
            "HttpDeleteAttribute",
            "HttpPatchAttribute",
            "HttpPostAttribute",
            "HttpPutAttribute",
            "HttpDeleteAttribute",
            "HttpPatchAttribute"
        };

        private static HashSet<string> _httpRequestProperties = new HashSet<string>()
        {
            "Cookies",
            "Headers",
            "Form",
            "QueryString",
            "Params"
        };

        private static HashSet<INamedTypeSymbol> _controllerSymbols = new HashSet<INamedTypeSymbol>();
        private bool _isAntiforgeryTokenGloballyRequested = false;
        /// <summary>
        /// Initialize the QR with the given context and register all the syntax nodes
        /// to listen during the visit and provide a specific callback for each one
        /// </summary>
        /// <param name="context"></param>
        public override void Init(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(OnCompilationStart);
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.MethodDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeGlobalSettings, SyntaxKind.InvocationExpression);
        }

        private void OnCompilationStart(CompilationStartAnalysisContext context)
        { 
            context.Compilation.GetSymbolsForClasses(_baseControllers, ref _controllerSymbols);
            //_DbCommandSymbol = context.Compilation.GetTypeByMetadataName(_DbCommandName);
        }

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            try
            {
                var node = context.Node;

                var containingSymbol = context.ContainingSymbol;

                var parentSymbol = containingSymbol.ContainingSymbol;
                while (parentSymbol != null && !(parentSymbol is INamedTypeSymbol))
                {
                    parentSymbol = parentSymbol.ContainingSymbol;
                }
                
                if (parentSymbol != null)
                {
                    var classSymbol = parentSymbol as INamedTypeSymbol;
                    // Check that the class is a Controller
                    if (!_controllerSymbols.Where(_ => classSymbol.IsOrInheritsFrom(_)).Any())
                        return;
                }

                bool hasHttpToken = false;
                var attributesList = containingSymbol.GetAttributes();
                foreach(var attribute in attributesList)
                {
                    var attName = attribute.AttributeClass.MetadataName;
                    if (_antiforgeryTokenAttributes.Contains(attName))
                        return;

                    if (_httpMessages.Contains(attName))
                        hasHttpToken = true;
                }

                

                var parentAttributesList = parentSymbol.GetAttributes();
                foreach (var attribute in parentAttributesList)
                {
                    var attName = attribute.AttributeClass.MetadataName;
                    if (_antiforgeryTokenAttributes.Contains(attName))
                        return;
                }

                bool useRequestGetter = false;
                var memberAccessNodes = node.DescendantNodes()
                                .OfType<ElementAccessExpressionSyntax>()
                                .Select(_ => _.Expression as MemberAccessExpressionSyntax)
                                .Where(_ => _ != null);

                foreach (var memberAccessNode in memberAccessNodes)
                {
                    if (_httpRequestProperties.Contains(memberAccessNode.Name.Identifier.ValueText))
                    {
                        var expressionType = context.SemanticModel.GetTypeInfo(memberAccessNode.Expression).Type;
                        if (expressionType.Name == "HttpRequest")
                        {
                            useRequestGetter = true;
                            break;
                        }
                    }
                }

                if(hasHttpToken || useRequestGetter)
                {
                    var methodNode = node as MethodDeclarationSyntax;
                    var pos = methodNode.Identifier.GetLocation().GetMappedLineSpan();
                    AddViolation(containingSymbol, new[] { pos });
                    return;
                }


            }
            catch (Exception e)
            {
                Log.Warn(" Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath + ": " + context.Node.GetLocation().GetMappedLineSpan(), e);
            }
        }


        private void AnalyzeGlobalSettings(SyntaxNodeAnalysisContext context)
        {
            try
            {
                var node = context.Node as InvocationExpressionSyntax;
                if (node == null)
                    return;
                var containingSymbol = context.ContainingSymbol;

                var memberAccess = node.Expression as MemberAccessExpressionSyntax;
                if (memberAccess == null)
                    return;

                if (memberAccess.Name.Identifier.ValueText != "AddControllersWithViews")
                    return;

                var creationNodes = node.ArgumentList.DescendantNodes().OfType<ObjectCreationExpressionSyntax>();

                foreach (var creationNode in creationNodes)
                {
                    var nodeType = creationNode.Type as IdentifierNameSyntax;
                    if (nodeType == null)
                        continue;
                    if (_antiforgeryTokenAttributes.Contains(nodeType.Identifier.ValueText))
                    {
                        _isAntiforgeryTokenGloballyRequested = true;
                        break;
                    }
                }

            }
            catch (Exception e)
            {
                Log.Warn(" Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath + ": " + context.Node.GetLocation().GetMappedLineSpan(), e);
            }
        }

        public override void FinalizeViolations()
        {
            if (_isAntiforgeryTokenGloballyRequested)
            {
                Violations.Clear();
            }
            _isAntiforgeryTokenGloballyRequested = false;
        }
    }
}
