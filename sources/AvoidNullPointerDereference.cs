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
        Id = "EI_AvoidNullPointerDereference",
        Title = "Avoid Null Pointer Dereference",
        MessageFormat = "Null pointers should not be dereferenced",
        Category = "Efficiency - Performance",
        DefaultSeverity = DiagnosticSeverity.Warning,
        CastProperty = "EIDotNetQualityRules.AvoidNullPointerDereference"
    )]
    public class AvoidNullPointerDereference : AbstractRuleChecker
    {
        


        public SyntaxNodeAnalysisContext _currentContext;

        /// <summary>
        /// Initialize the QR with the given context and register all the syntax nodes
        /// to listen during the visit and provide a specific callback for each one
        /// </summary>
        /// <param name="context"></param>
        public override void Init(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.MethodDeclaration);
            //context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.IfStatement);
        }

        private readonly object _lock = new object();
        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            lock (_lock)
            {
                try
                {
                    _currentContext = context;
                    var node = context.Node as MethodDeclarationSyntax;
                    Scope methodScope = new Scope(node, this);
                    methodScope.AnalyzeScope();
                }
                catch (Exception e)
                {
                    Log.Warn(" Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath + ": " + context.Node.GetLocation().GetMappedLineSpan(), e);
                }
            }
        }


        public enum ScopeType
        {
            MethodeScope,
            ConditionalScope,
            SwitchScope,
            ForScope,
            ForeachScope,
            WhileScope,
            DoWhileScope,
            BlockALoneScope
        };

        public class Scope
        {
            public SyntaxNode _scopeNode;
            public Scope _parentScope;
            public AvoidNullPointerDereference _checker;
            public List<Scope> _childrenScope = new List<Scope>();
            public Dictionary<ISymbol, bool> _varSetAtNullInScope = new Dictionary<ISymbol, bool>();
            public Dictionary<ISymbol, bool> _varSetAtNullInAncestorScopes;
            public Dictionary<ISymbol, bool> _conditionVar = new Dictionary<ISymbol, bool>();
            
            // this constructor MUST only be invoked one time for the scope of the method
            public Scope(SyntaxNode node, AvoidNullPointerDereference checker) 
            {
                _scopeNode = node;
                _parentScope = null;
                _checker = checker;
                _varSetAtNullInAncestorScopes = new Dictionary<ISymbol,bool>();
            }

            public Scope(SyntaxNode node, Scope parentScope)
            {
                _scopeNode = node;
                _parentScope = parentScope;
                _checker = parentScope._checker;
                _varSetAtNullInAncestorScopes = _parentScope._varSetAtNullInAncestorScopes.Concat(parentScope._varSetAtNullInScope)
                                                                .ToDictionary(x => x.Key, x => x.Value);
            }

            private Scope AddChildScope(SyntaxNode childNode)
            {
                Scope child = new Scope(childNode, this);
                _childrenScope.Add(child);
                return child;
            }

            public void CheckCondition(SyntaxNode node, bool elseBlock=false)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.LogicalAndExpression:
                        var andNode = node as BinaryExpressionSyntax;
                        if(andNode != null)
                        {
                            CheckCondition(andNode.Left, elseBlock);
                            CheckCondition(andNode.Right, elseBlock);
                        }
                        break;
                    case SyntaxKind.EqualsExpression:
                        var equalNode = node as BinaryExpressionSyntax;
                        if(equalNode!=null)
                        {
                            if(equalNode.Right.IsKind(SyntaxKind.NullLiteralExpression))
                            {
                                var symbInf = _checker._currentContext.SemanticModel.GetSymbolInfo(equalNode.Left);
                                var equalSymb = symbInf.Symbol;
                                _conditionVar[equalSymb] = !elseBlock;
                            }
                            else if (equalNode.Left.IsKind(SyntaxKind.NullLiteralExpression))
                            {
                                var symbInf = _checker._currentContext.SemanticModel.GetSymbolInfo(equalNode.Right);
                                var equalSymb = symbInf.Symbol;
                                _conditionVar[equalSymb] = !elseBlock;
                            }
                        }
                        break;
                    case SyntaxKind.NotEqualsExpression:
                        var inequalNode = node as BinaryExpressionSyntax;
                        if (inequalNode != null)
                        {
                            if (inequalNode.Right.IsKind(SyntaxKind.NullLiteralExpression))
                            {
                                var symbInf = _checker._currentContext.SemanticModel.GetSymbolInfo(inequalNode.Left);
                                var inequalSymb = symbInf.Symbol;
                                _conditionVar[inequalSymb] = elseBlock;
                            }
                            else if (inequalNode.Left.IsKind(SyntaxKind.NullLiteralExpression))
                            {
                                var symbInf = _checker._currentContext.SemanticModel.GetSymbolInfo(inequalNode.Right);
                                var inequalSymb = symbInf.Symbol;
                                _conditionVar[inequalSymb] = elseBlock;
                            }
                        }
                        break;
                    default:
                        break;
                }
            }

            private void setConditionVar(Scope childScope)
            {
                foreach (var symb in childScope._conditionVar.Keys)
                {
                    if (childScope._conditionVar[symb] 
                        && childScope._varSetAtNullInAncestorScopes.ContainsKey(symb)
                        && childScope._varSetAtNullInAncestorScopes[symb])
                    {
                        if(_varSetAtNullInScope.ContainsKey(symb))
                            _varSetAtNullInScope[symb] = true;
                        else if (_varSetAtNullInAncestorScopes.ContainsKey(symb))
                            _varSetAtNullInAncestorScopes[symb] = true;
                    }
                }
            }

            public void AnalyzeScope()
            {
                var descendantNodes = _scopeNode.DescendantNodes(DescendIntoChildren);
                Scope child;
                foreach (var descendantNode in descendantNodes)
                {
                    switch (descendantNode.Kind())
                    {
                        case SyntaxKind.VariableDeclarator:
                            var declaratorNode = descendantNode as VariableDeclaratorSyntax;
                            if (declaratorNode != null && declaratorNode.Initializer != null)
                            {
                                if(declaratorNode.Initializer.IsKind(SyntaxKind.EqualsValueClause)
                                    && declaratorNode.Initializer.Value.IsKind(SyntaxKind.NullLiteralExpression))
                                {
                                    var declarSymb = _checker._currentContext.SemanticModel.GetDeclaredSymbol(declaratorNode);
                                    if(declarSymb!=null)
                                    {
                                        _varSetAtNullInScope[declarSymb] = false;
                                    }
                                }
                            }
                            break;
                        case SyntaxKind.SimpleAssignmentExpression:
                            var assignmentNode = descendantNode as AssignmentExpressionSyntax;
                            if(assignmentNode!=null)
                            {
                                var symbInf = _checker._currentContext.SemanticModel.GetSymbolInfo(assignmentNode.Left);
                                var assignSymb = symbInf.Symbol;
                                if (assignSymb != null)
                                {
                                    if(assignmentNode.Right.IsKind(SyntaxKind.NullLiteralExpression))
                                    {
                                        _varSetAtNullInScope[assignSymb] = false;
                                    }
                                    else if(_varSetAtNullInScope.ContainsKey(assignSymb))
                                    {
                                        _varSetAtNullInScope[assignSymb] = true;
                                    }
                                    else if (_varSetAtNullInAncestorScopes.ContainsKey(assignSymb))
                                    {
                                        _varSetAtNullInAncestorScopes[assignSymb] = true;
                                    }
                                }
                            }
                            break;
                        case SyntaxKind.Argument:
                            var argNode = descendantNode as ArgumentSyntax;
                            if (argNode != null && argNode.RefKindKeyword.ValueText == "out")
                            {
                                var symbInf = _checker._currentContext.SemanticModel.GetSymbolInfo(argNode.Expression);
                                var argSymb = symbInf.Symbol;
                                if (argSymb != null && _varSetAtNullInScope.ContainsKey(argSymb))
                                {
                                    _varSetAtNullInScope[argSymb] = true;
                                }
                            }
                            break;
                        case SyntaxKind.SimpleMemberAccessExpression:
                            var memberAccessNode = descendantNode as MemberAccessExpressionSyntax;
                            if (memberAccessNode != null)
                            {
                                var symbInf = _checker._currentContext.SemanticModel.GetSymbolInfo(memberAccessNode.Expression);
                                var elementAccessSymbol = symbInf.Symbol;
                                if (_conditionVar != null && elementAccessSymbol != null &&
                                    (!_conditionVar.ContainsKey(elementAccessSymbol)|| _conditionVar[elementAccessSymbol])
                                  )
                                {
                                    if(_varSetAtNullInScope.ContainsKey(elementAccessSymbol))
                                    {
                                        if(!_varSetAtNullInScope[elementAccessSymbol])
                                        {
                                            var pos = memberAccessNode.GetLocation().GetMappedLineSpan();
                                            _checker.AddViolation(elementAccessSymbol, new[] { pos });
                                        }
                                    }
                                    else if(_varSetAtNullInAncestorScopes.ContainsKey(elementAccessSymbol))
                                    {
                                        if (!_varSetAtNullInAncestorScopes[elementAccessSymbol])
                                        {
                                            var pos = memberAccessNode.GetLocation().GetMappedLineSpan();
                                            _checker.AddViolation(elementAccessSymbol, new[] { pos });
                                        }
                                    }
                                }
                            }
                            break;
                        case SyntaxKind.ElementAccessExpression:
                            var elementAccessNode = descendantNode as ElementAccessExpressionSyntax;
                            if(elementAccessNode!=null)
                            {
                                var symbInf = _checker._currentContext.SemanticModel.GetSymbolInfo(elementAccessNode.Expression);
                                var elementAccessSymbol = symbInf.Symbol;
                                if (_conditionVar != null && elementAccessSymbol != null &&
                                    (!_conditionVar.ContainsKey(elementAccessSymbol) || _conditionVar[elementAccessSymbol])
                                  )
                                {
                                    if (_varSetAtNullInScope.ContainsKey(elementAccessSymbol))
                                    {
                                        if (!_varSetAtNullInScope[elementAccessSymbol])
                                        {
                                            var pos = elementAccessNode.GetLocation().GetMappedLineSpan();
                                            _checker.AddViolation(elementAccessSymbol, new[] { pos });
                                        }
                                    }
                                    else if (_varSetAtNullInAncestorScopes.ContainsKey(elementAccessSymbol))
                                    {
                                        if (!_varSetAtNullInAncestorScopes[elementAccessSymbol])
                                        {
                                            var pos = elementAccessNode.GetLocation().GetMappedLineSpan();
                                            _checker.AddViolation(elementAccessSymbol, new[] { pos });
                                        }
                                    }
                                }
                            }
                            break;                        
                        case SyntaxKind.IfStatement:
                            var ifNode = descendantNode as IfStatementSyntax;
                            if (ifNode != null)
                            {
                                // check condition
                                child = AddChildScope(ifNode.Condition);
                                child.CheckCondition(ifNode.Condition);
                                child.AnalyzeScope();
                                // check then block
                                child = AddChildScope(ifNode.Statement);
                                child.CheckCondition(ifNode.Condition);
                                child.AnalyzeScope();
                                setConditionVar(child);
                                // check else block
                                if(ifNode.Else!=null)
                                {
                                    child = AddChildScope(ifNode.Else);
                                    child.CheckCondition(ifNode.Condition, true);
                                    child.AnalyzeScope();
                                    setConditionVar(child);
                                }
                            }
                            break;
                        case SyntaxKind.ForEachStatement:
                            var foreachNode = descendantNode as ForEachStatementSyntax;
                            if (foreachNode != null)
                            {
                                child = AddChildScope(foreachNode.Statement);
                                child.AnalyzeScope();
                                setConditionVar(child);
                            }
                            break;
                        case SyntaxKind.ForStatement:
                            var forNode = descendantNode as ForStatementSyntax;
                            if (forNode != null)
                            {
                                child = AddChildScope(forNode.Statement);
                                child.AnalyzeScope();
                                setConditionVar(child);
                            }
                            break;
                        case SyntaxKind.WhileStatement:
                            var whileNode = descendantNode as WhileStatementSyntax;
                            if (whileNode != null)
                            {
                                child = AddChildScope(whileNode.Statement);
                                child.CheckCondition(whileNode.Condition);
                                child.AnalyzeScope();
                                setConditionVar(child);
                                
                            }
                            break;
                        case SyntaxKind.SwitchStatement:
                            var switchNode = descendantNode as SwitchStatementSyntax;
                            if (switchNode != null)
                            {
                                foreach(var section in switchNode.Sections)
                                {
                                    child = AddChildScope(section);
                                    child.AnalyzeScope();
                                    setConditionVar(child);
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }

            }

            
        }

        

        public static bool DescendIntoChildren(SyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.IfStatement:
                case SyntaxKind.ForEachStatement:
                case SyntaxKind.ForStatement:
                case SyntaxKind.WhileStatement:
                case SyntaxKind.SwitchStatement:
                    return false;
                default:
                    return true;
            }
        }


    }
}

