using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.DotNet.CastDotNetExtension;

namespace CastDotNetExtension
{
    [CastRuleChecker]
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [RuleDescription(
        Id = "EI_AvoidHavingSameImplementationInAllBranchesOfConditionalStructure",
        Title = "Avoid having the same implementation in ALL BRANCHES of a conditional structure",
        MessageFormat = "Avoid having the same implementation in ALL BRANCHES of a conditional structure",
        Category = "Complexity - Algorithmic and Control Structure Complexity",
        DefaultSeverity = DiagnosticSeverity.Warning,
        CastProperty = "EIDotNetQualityRules.AvoidHavingSameImplementationInAllBranchesOfConditionalStructure"
    )]
    public class AvoidHavingSameImplementationInAllBranchesOfConditionalStructure : AbstractRuleChecker
    {

        /// <summary>
        /// Initialize the QR with the given context and register all the syntax nodes
        /// to listen during the visit and provide a specific callback for each one
        /// </summary>
        /// <param name="context"></param>
        public override void Init(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeIf, SyntaxKind.IfStatement);
            context.RegisterSyntaxNodeAction(AnalyzeSwitch, SyntaxKind.SwitchStatement);
        }

        private readonly object _lock = new object();


        private bool IsEquivalent(SyntaxNode if_then_statement, SyntaxNode if_else_statement)
        {
            if (if_then_statement == null || if_else_statement == null)
                return false;

            var block_syntax = if_then_statement as BlockSyntax;
            var block_syntax_else = if_else_statement as BlockSyntax;

            if (block_syntax == null && block_syntax_else == null)
            {
                return if_then_statement.IsEquivalentTo(if_else_statement);
            }

            if (block_syntax == null)
            {
                return block_syntax_else.Statements.Count == 1 ? if_then_statement.IsEquivalentTo(block_syntax_else.Statements.First()) : false;
            }

            if (block_syntax_else == null)
            {
                return block_syntax.Statements.Count == 1 ? if_else_statement.IsEquivalentTo(block_syntax.Statements.First()) : false;
            }

            var statements = block_syntax.Statements;
            var statements_else = block_syntax_else.Statements;

            if (statements.Count != statements_else.Count)
                return false;

            for (int i = 0; i < statements.Count; ++i)
            {
                if (!statements[i].IsEquivalentTo(statements_else[i]))
                    return false;
            }

            return true;
        }

        private void AnalyzeIf(SyntaxNodeAnalysisContext context)
        {
            //Log.InfoFormat("Run registered callback for rule: {0}", GetRuleName());
            //lock (_lock)
            {
                try
                { 
                    var if_statement = context.Node as IfStatementSyntax;

                    if (if_statement.Parent.IsKind(SyntaxKind.ElseClause))
                        return;

                    var if_then_statement = if_statement.Statement;

                    // Two lists should have always the same size.
                    var list_block_if = new List<SyntaxNode>() { if_then_statement };
                    var list_additional_bookmarks = new List<List<SyntaxNode>>() { new List<SyntaxNode>() {} };

                    var if_else = if_statement.Else;
                    if (if_else == null)
                        return;

                    var if_else_statement = if_else.Statement;

                    int i;
                    bool is_found_equivalent = true;
                    bool is_last_else = false;
                    var former_if_statement = if_statement;
                    if_statement = if_else_statement as IfStatementSyntax;

                    if (if_statement == null && if_else_statement!=null) // case simple if(){} else{}
                    {
                        if (!IsEquivalent(former_if_statement.Statement, if_else_statement))
                        {
                            is_found_equivalent = false;
                        }
                    }

                    while (if_statement != null && !is_last_else)
                    {
                        if (!IsEquivalent(former_if_statement.Statement, if_statement.Statement))
                        {
                            is_found_equivalent = false;
                            break;
                        }
                        if_else = if_statement.Else;
                        if (if_else == null)
                            is_last_else = true;
                        else
                            if_else_statement = if_else.Statement;
                        former_if_statement = if_statement;
                        if_statement = if_else_statement as IfStatementSyntax;
                        if (if_statement == null && if_else_statement != null) 
                        {
                            if (!IsEquivalent(former_if_statement.Statement, if_else_statement))
                            {
                                is_found_equivalent = false;
                            }
                        }
                    }



                    if (is_found_equivalent && !is_last_else)
                    {
                        var pos = context.Node.GetLocation().GetMappedLineSpan();
                        AddViolation(context.ContainingSymbol, new[] { pos });
                    }
                    //while (if_statement != null && !is_last_else)
                    //{
                    //    if_then_statement = if_statement.Statement;
                    //    is_found_equivalent = false;
                    //    i = 0;
                    //    while (i <= list_block_if.Count - 1 && !is_found_equivalent)
                    //    {
                    //        if (IsEquivalent(list_block_if[i],if_then_statement))
                    //        {
                    //            list_additional_bookmarks[i].Add(if_then_statement);
                    //            is_found_equivalent = true;
                    //        }
                    //        ++i;
                    //    }

                    //    if (!is_found_equivalent)
                    //    {
                    //        list_block_if.Add(if_then_statement);
                    //        list_additional_bookmarks.Add(new List<SyntaxNode>() { });
                    //    }

                    //    if_else = if_statement.Else;
                    //    if (if_else == null)
                    //        is_last_else = true;
                    //    else
                    //        if_else_statement = if_else.Statement;
                    //    if_statement = if_else_statement as IfStatementSyntax;
                    //}

                    //i = 0;
                    //is_found_equivalent = false;
                    //while (i <= list_block_if.Count - 1 && !is_found_equivalent)
                    //{
                    //    if (IsEquivalent(list_block_if[i], if_else_statement))
                    //    {
                    //        list_additional_bookmarks[i].Add(if_else_statement);
                    //        is_found_equivalent = true;
                    //    }
                    //    ++i;
                    //}
                    
                    //for (i = 0; i <= list_block_if.Count - 1; ++i)
                    //{
                    //    if (list_additional_bookmarks[i] != null && list_additional_bookmarks[i].Any())
                    //    {
                    //        //AddViolatingNode(context.ContainingSymbol, list_block_if[i], list_additional_bookmarks[i]);
                    //        var pos = context.Node.GetLocation().GetMappedLineSpan();
                    //        AddViolation(context.ContainingSymbol, new[] { pos });
                    //    }
                    //}
                }
                catch (Exception e)
                {
                    Log.Warn(" Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath + ": " + context.Node.GetLocation().GetMappedLineSpan(), e);
                }
            }
            //Log.InfoFormat("END Run registered callback for rule: {0}", GetRuleName());
        }

        private void AnalyzeSwitch(SyntaxNodeAnalysisContext context)
        {
            //Log.InfoFormat("Run registered callback for rule: {0}", GetRuleName());
            //lock (_lock)
            {
                try
                {
                    var switch_statement = context.Node as Microsoft.CodeAnalysis.CSharp.Syntax.SwitchStatementSyntax;

                    var sections = switch_statement.Sections;
                    if (sections.Any() == false)
                        return;

                    var labels = sections[sections.Count - 1].Labels;
                    var additional_bookmark = new List<SyntaxNode>() { };
                    
                    for (int i = labels.Count - 1; i >= 0; --i)
                    {
                        if (labels[i].IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.DefaultSwitchLabel))
                        {
                            for (int j = 0; j < sections.Count - 1; ++j)
                            {
                                var statements = sections[j].Statements;
                                var statements_default = sections[sections.Count - 1].Statements;

                                if (!statements.Any() || statements.Count != statements_default.Count)
                                    return;

                                int k = 0;
                                bool equals = true;
                                while (k <= statements.Count - 1 && equals)
                                {
                                    var first_statment = statements[k];
                                    var second_statement = statements_default[k];
                                    equals = first_statment.IsEquivalentTo(second_statement);
                                    ++k;
                                }

                                if (!equals)
                                   return;
                            }

                            var pos = switch_statement.GetLocation().GetMappedLineSpan();
                            AddViolation(context.ContainingSymbol, new[] { pos });
                            return;
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Warn(" Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath + ": " + context.Node.GetLocation().GetMappedLineSpan(), e);
                }
            }
            //Log.InfoFormat("END Run registered callback for rule: {0}", GetRuleName());
        }
    }
}
