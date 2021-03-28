using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Collections.Generic;
using System;
using Roslyn.DotNet.CastDotNetExtension;

namespace CastDotNetExtension
{
    [CastRuleChecker]
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [RuleDescription(
        Id = "EI_AvoidClassesWithTooManyConstructors",
        Title = "Number of constructors > 4",
        MessageFormat = "Avoid classes with a number of constructors > X (parameter value X= 4)",
        Category = "Maintainability",
        DefaultSeverity = DiagnosticSeverity.Warning,        
        CastProperty = "EIDotNetQualityRules.avoidClassesWithTooManyConstructorsAnalyzer"
    )]
    public class AvoidClassesWithTooManyConstructorsAnalyzer : AbstractRuleChecker
    {

        /// <summary>
        /// Initialize the QR with the given context and register all the syntax nodes
        /// to listen during the visit and provide a specific callback for each one
        /// </summary>
        /// <param name="context"></param>
        public override void Init(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ClassDeclaration);
        }

        private readonly object _lock = new object();
        private void Analyze(SyntaxNodeAnalysisContext context)
        {
           /*lock (_lock)*/ {
              try {
                 var classDeclarationNode = context.Node as ClassDeclarationSyntax;

                 var constructorDeclarations = classDeclarationNode.DescendantNodes().OfType<ConstructorDeclarationSyntax>().ToList();

                 if (constructorDeclarations.Count > 4) {
                    foreach (var constructorDeclaration in constructorDeclarations) {
                       AddViolation(context, new List<FileLinePositionSpan> { constructorDeclaration.GetLocation().GetMappedLineSpan() });
                    }
                 }
              }
              catch (Exception e) {
                 Log.Warn(" Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath + ": " + context.Node.GetLocation().GetMappedLineSpan(), e);
              }
           }
        }
    }
}