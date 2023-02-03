using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.DotNet.CastDotNetExtension;
using CastDotNetExtension.Utils;

namespace CastDotNetExtension
{
    [CastRuleChecker]
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [RuleDescription(
       Id = "EI_AvoidStoringPasswordInString",
       Title = "Avoid storing password in String",
       MessageFormat = "Avoid storing password in String",
       Category = "Secure Coding - API Abuse",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.AvoidStoringPasswordInString"
   )]
    public class AvoidStoringPasswordInString : AbstractRuleChecker
    {
        /// <summary>
        /// Initialize the QR with the given context and register all the syntax nodes
        /// to listen during the visit and provide a specific callback for each one
        /// </summary>
        /// <param name="context"></param>
        public override void Init(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.PropertyDeclaration);
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.VariableDeclarator);
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.Parameter);
        }

        // Small string should be full variable name not part of it to limit false positives
        private static readonly HashSet<string> _fullVariablePasswordName = new HashSet<string>
        {
         "pswd",
         "auth",
         "mima",
         "mdp",
        };

        private static readonly HashSet<string> _partialVariablePasswordName = new HashSet<string>
        {
         "password",
         "passwd",
         "passwrd",
         "psswrd",
         "watchword",
         "passphrase",
         "credentials",
         "passString",
         "loginPass",
         "passKey",
         "secretKey",
         "contrasena",
         "motdepasse",
         "mot_de_passe",
        };

        private SyntaxNodeAnalysisContext _context;
        private INamedTypeSymbol _stringTypeSymbol = null;

        private void InitializeSymbols(SyntaxNodeAnalysisContext context)
        {
            if (_stringTypeSymbol == null)
            {
                _stringTypeSymbol = context.Compilation.GetTypeByMetadataName("System.String") as INamedTypeSymbol;
            }
            
        }

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            /*lock (_lock)*/
            {
                try
                {
                    InitializeSymbols(context);
                    _context = context;
                    var node = context.Node;

                    if (_stringTypeSymbol == null || !IsVariableAString(node))
                        return;

                    var variableName = GetVariableName(node).ToLower();
                    if(IsPasswordName(variableName))
                    {
                        var pos = node.GetLocation().GetMappedLineSpan();
                        AddViolation(context.ContainingSymbol, new[] { pos });
                    }
                    
                }
                catch (Exception e)
                {
                    Log.Warn(" Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath + ": " + context.Node.GetLocation().GetMappedLineSpan(), e);
                }
            }
        }

        private bool IsPasswordName(string variableName)
        {
            bool isPasswordName = false;
            if (_fullVariablePasswordName.Contains(variableName))
            {
                isPasswordName = true;
            }
            else
            {
                foreach (var passName in _partialVariablePasswordName)
                {
                    if (variableName.Contains(passName))
                    {
                        isPasswordName = true;
                        break;
                    }
                }
            }
            return isPasswordName;
        }

        private bool IsVariableAString(SyntaxNode node)
        {
            var nodeSymbol = _context.SemanticModel.GetDeclaredSymbol(node);
            IFieldSymbol fieldSymbol = nodeSymbol as IFieldSymbol;
            if (fieldSymbol != null && fieldSymbol.Type.IsOrInheritsFrom(_stringTypeSymbol))
            {
                return true;
            }
            IPropertySymbol propSymbol = nodeSymbol as IPropertySymbol;
            if (propSymbol != null && propSymbol.Type.IsOrInheritsFrom(_stringTypeSymbol))
            {
                return true;
            }
            IParameterSymbol parmSymbol = nodeSymbol as IParameterSymbol;
            if (parmSymbol != null && parmSymbol.Type.IsOrInheritsFrom(_stringTypeSymbol))
            {
                return true;
            }
            ILocalSymbol localSymbol = nodeSymbol as ILocalSymbol;
            if (localSymbol != null && localSymbol.Type.IsOrInheritsFrom(_stringTypeSymbol))
            {
                return true;
            }
            return false;
        }

        private string GetVariableName(SyntaxNode node)
        {
            string variableName = string.Empty;
            switch(node.Kind())
            {
                case SyntaxKind.VariableDeclarator:
                    var declaratorNode = node as VariableDeclaratorSyntax;
                    variableName = declaratorNode.Identifier.ValueText;
                    break;

                case SyntaxKind.PropertyDeclaration:
                    var propertyNode = node as PropertyDeclarationSyntax;
                    variableName = propertyNode.Identifier.ValueText;
                    break;

                case SyntaxKind.Parameter:
                    var parameterNode = node as ParameterSyntax;
                    variableName = parameterNode.Identifier.ValueText;
                    break;

                default:
                    break;
            }
            return variableName;
        }

    }
}
