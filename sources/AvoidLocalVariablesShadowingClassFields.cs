﻿using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.DotNet.CastDotNetExtension;
using Roslyn.DotNet.Common;

namespace CastDotNetExtension
{
    [CastRuleChecker]
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [RuleDescription(
        Id = "EI_AvoidLocalVariablesShadowingClassFields",
        Title = "Local Variables Shadowing Class Fields",
        MessageFormat = "Avoid Local Variables Shadowing Class Fields",
        Category = "Programming Practices - Unexpected Behavior",
        DefaultSeverity = DiagnosticSeverity.Warning,
        CastProperty = "EIDotNetQualityRules.AvoidLocalVariablesShadowingClassFields"
    )]
    public class AvoidLocalVariablesShadowingClassFields : AbstractRuleChecker
    {

        private readonly Dictionary<string, Dictionary<string, ISymbol>> _klazzToMembers
           = new Dictionary<string, Dictionary<string, ISymbol>>();

        public AvoidLocalVariablesShadowingClassFields()
            : base(ViolationCreationMode.ViolationWithAdditionalBookmarks)
        {
        }

        /// <summary>
        /// Initialize the QR with the given context and register all the syntax nodes
        /// to listen during the visit and provide a specific callback for each one
        /// </summary>
        /// <param name="context"></param>
        public override void Init(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AddViolationIfLocalVariableViolates,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.VariableDeclarator,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.ForEachStatement, 
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.EndOfFileToken);
        }

        private readonly object _lock = new object();

        protected void AddViolationIfLocalVariableViolates(SyntaxNodeAnalysisContext context)
        {

            lock (_lock)
            {
                try
                {
                    if (SymbolKind.Method == context.ContainingSymbol.Kind)
                    {
                        var csharpNode = context.Node as Microsoft.CodeAnalysis.CSharp.Syntax.VariableDeclaratorSyntax;
                        string name = null;
                        if(csharpNode != null)
                        {
                            name = csharpNode.Identifier.ValueText;
                        }
                        else
                        {
                            var foreachNode = context.Node as Microsoft.CodeAnalysis.CSharp.Syntax.ForEachStatementSyntax;
                            if (foreachNode != null)
                            {
                                name = foreachNode.Identifier.ValueText;
                            }
                        }

                        if (null != name)
                        {
                            var type = context.ContainingSymbol.ContainingType;
                            if (null != type)
                            {
                                string fullname = type.OriginalDefinition.ToString();
                                Dictionary<string, ISymbol> fields = null;
                                if (!_klazzToMembers.TryGetValue(fullname, out fields))
                                {
                                    fields = new Dictionary<string, ISymbol>();
                                    _klazzToMembers[fullname] = fields;
                                    bool considerPrivateMembers = true;
                                    do
                                    {
                                        foreach (var member in type.GetMembers())
                                        {
                                            var field = member as IFieldSymbol;
                                            if (null != field)
                                            {
                                                if (SymbolKind.Field == field.Kind)
                                                {
                                                    if (considerPrivateMembers || Accessibility.Private != field.DeclaredAccessibility)
                                                    {
                                                        fields[field.Name] = field;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                var property = member as IPropertySymbol;
                                                if (null != property)
                                                {
                                                    if (SymbolKind.Property == property.Kind)
                                                    {
                                                        if (considerPrivateMembers || Accessibility.Private != property.DeclaredAccessibility)
                                                        {
                                                            fields[property.Name] = property;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        considerPrivateMembers = false;
                                        type = type.BaseType;
                                    } while (null != type && !type.ToString().Equals("object"));
                                }

                                if (fields.ContainsKey(name))
                                {
                                    FileLinePositionSpan pos = context.Node.GetLocation().GetMappedLineSpan();
                                    var node = context.Node as Microsoft.CodeAnalysis.CSharp.Syntax.ForEachStatementSyntax;
                                    if(node!=null)
                                    {
                                        pos = node.Identifier.GetLocation().GetMappedLineSpan();
                                    }
                                    var poses = new List<FileLinePositionSpan> { pos };
                                    if (!fields[name].Locations.IsDefaultOrEmpty)
                                    {
                                        poses.Add(fields[name].Locations[0].GetMappedLineSpan());
                                    }
                                    AddViolation(context, poses);
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

        public override void Reset()
        {
            lock (_lock)
            {
                try
                {
                    _klazzToMembers.Clear();
                    base.Reset();
                }
                catch (Exception e)
                {
                    Log.Warn("Exception during  AvoidLocalVariablesShadowingClassFields.Reset ", e);
                }
            }
        }
    }
}
