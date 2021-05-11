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
                    //if (node.Identifier.ValueText == "f14")
                    {
                        Scope methodScope = new Scope(node, this);
                        methodScope.AnalyzeScope();
                    }

                }
                catch (Exception e)
                {
                    Log.Warn(" Exception while analyzing " + context.SemanticModel.SyntaxTree.FilePath + ": " + context.Node.GetLocation().GetMappedLineSpan(), e);
                }
            }
        }


        public class SymbolList : IEquatable<SymbolList>
        {
            public List<ISymbol> symbols;

            public SymbolList()
            {
                symbols = new List<ISymbol>();
            }

            public SymbolList(ISymbol symbol)
            {
                symbols = new List<ISymbol>() { symbol };
            }

            public SymbolList(List<ISymbol> symbolsList)
            {
                symbols = symbolsList;
            }

            public void Add(ISymbol symbol)
            {
                symbols.Add(symbol);
            }

            public void AddRange(SymbolList listSymbols)
            {
                symbols.AddRange(listSymbols.symbols);
            }

            public bool Equals(SymbolList symbList)
            {
                //Check for null and compare run-time types.
                if ((symbList == null) || !this.GetType().Equals(symbList.GetType()))
                {
                    return false;
                }
                else if(symbols.Count != symbList.symbols.Count)
                {
                    return false;                 
                }
                else
                {
                    for(int index=0;index<symbols.Count;index++)
                    {
                        if(!symbols[index].Equals(symbList.symbols[index]))
                            return false;
                    }
                    return true;
                }
            }

            public override int GetHashCode()
            {
                int hashCode = 0;
                if(symbols.Count>0)
                {
                    hashCode = symbols[0].GetHashCode();
                    for(int index=1;index<symbols.Count;index++)
                    {
                        hashCode = hashCode ^ symbols[index].GetHashCode();
                    }
                }
                return hashCode;
            }
           
        }

        public class KeyValuePairOfSymbolListComparer : IEqualityComparer<KeyValuePair<SymbolList, bool>>
        {
            public bool Equals(KeyValuePair<SymbolList, bool> x, KeyValuePair<SymbolList, bool> y)
            {
                return x.Key.Equals(y.Key);
            }

            public int GetHashCode(KeyValuePair<SymbolList, bool> x)
            {
                return x.GetHashCode();
            }
        }

        public class SymbolListComparer : IEqualityComparer<SymbolList>
        {
            public bool Equals(SymbolList x, SymbolList y)
            {
                return x.Equals(y);
            }

            public int GetHashCode(SymbolList x)
            {
                return x.GetHashCode();
            }
        }

        public class KeyValuePairOfScopeComparer : IEqualityComparer<KeyValuePair<ISymbol, bool>>
       {
            public bool Equals(KeyValuePair<ISymbol, bool> x, KeyValuePair<ISymbol, bool> y)
            {
                return x.Key.Equals(y.Key);
            }

            public int GetHashCode(KeyValuePair<ISymbol, bool> x)
            {
                return x.GetHashCode();
            }
       }

        public class Scope
        {
            public SyntaxNode _scopeNode;
            public Scope _parentScope;
            public AvoidNullPointerDereference _checker;
            public List<Scope> _childrenScope = new List<Scope>();
            public Dictionary<SymbolList, bool> _varSetAtNullInScope = new Dictionary<SymbolList, bool>();
            public Dictionary<SymbolList, bool> _varSetAtNullInAncestorScopes;
            public Dictionary<SymbolList, bool> _conditionVar = new Dictionary<SymbolList, bool>();
            
            // this constructor MUST only be invoked one time for the scope of the method
            public Scope(SyntaxNode node, AvoidNullPointerDereference checker) 
            {
                _scopeNode = node;
                _parentScope = null;
                _checker = checker;
                _varSetAtNullInAncestorScopes = new Dictionary<SymbolList, bool>();
            }

            public Scope(SyntaxNode node, Scope parentScope)
            {
                _scopeNode = node;
                _parentScope = parentScope;
                _checker = parentScope._checker;
                _varSetAtNullInAncestorScopes = _parentScope._varSetAtNullInAncestorScopes
                                                    .Concat(parentScope._varSetAtNullInScope)
                                                    .Distinct(new KeyValuePairOfSymbolListComparer())
                                                    .ToDictionary(x => x.Key, x => x.Value);

            }

            private Scope AddChildScope(SyntaxNode childNode)
            {
                Scope child = new Scope(childNode, this);
                _childrenScope.Add(child);
                return child;
            }

            private SymbolList getSymbolList(SyntaxNode node)
            {
                var accessNode = node as MemberAccessExpressionSyntax;
                if(accessNode!=null)
                {
                    SymbolInfo symbInf = _checker._currentContext.SemanticModel.GetSymbolInfo(accessNode.Name);
                    ISymbol symb = symbInf.Symbol;
                    SymbolList listSymbols = new SymbolList();
                    if (symb != null)
                    {
                        listSymbols.Add( symb );
                        var res = getSymbolList(accessNode.Expression);
                        if(res == null)
                        {
                            return null;
                        }
                        else
                        {
                            listSymbols.AddRange(res);
                            return listSymbols;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    var symbInf = _checker._currentContext.SemanticModel.GetSymbolInfo(node);
                    var symb = symbInf.Symbol;
                    if (symb != null)
                        return new SymbolList(symb);
                    return null;
                }
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
                                //var symbInf = _checker._currentContext.SemanticModel.GetSymbolInfo(equalNode.Left);
                                //var equalSymb = symbInf.Symbol;
                                var equalSymb = getSymbolList(equalNode.Left);
                                if (equalSymb != null)
                                    _conditionVar[equalSymb] = !elseBlock;
                            }
                            else if (equalNode.Left.IsKind(SyntaxKind.NullLiteralExpression))
                            {
                                //var symbInf = _checker._currentContext.SemanticModel.GetSymbolInfo(equalNode.Right);
                                //var equalSymb = symbInf.Symbol;
                                var equalSymb = getSymbolList(equalNode.Right);
                                if(equalSymb!=null)
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
                                //var symbInf = _checker._currentContext.SemanticModel.GetSymbolInfo(inequalNode.Left);
                                //var inequalSymb = symbInf.Symbol;
                                var inequalSymb = getSymbolList(inequalNode.Left);
                                if (inequalSymb != null)
                                    _conditionVar[inequalSymb] = elseBlock;
                            }
                            else if (inequalNode.Left.IsKind(SyntaxKind.NullLiteralExpression))
                            {
                                //var symbInf = _checker._currentContext.SemanticModel.GetSymbolInfo(inequalNode.Right);
                                //var inequalSymb = symbInf.Symbol;
                                var inequalSymb = getSymbolList(inequalNode.Right);
                                if (inequalSymb != null)
                                    _conditionVar[inequalSymb] = elseBlock;
                            }
                        }
                        break;
                    default:
                        break;
                }

                var descendantNodes = node.DescendantNodes().OfType<ConditionalAccessExpressionSyntax>();
                foreach(var conditionalAccessNode in descendantNodes)
                {
                    var identifier = conditionalAccessNode.Expression as IdentifierNameSyntax;
                    if(identifier!=null)
                    {
                        //var symbInf = _checker._currentContext.SemanticModel.GetSymbolInfo(identifier);
                        //var identSymb = symbInf.Symbol;
                        var identSymb = getSymbolList(identifier);
                        if (identSymb != null)
                            _conditionVar[identSymb] = elseBlock;
                    }
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
                                        _varSetAtNullInScope[new SymbolList( declarSymb )] = false;
                                    }
                                }
                            }
                            break;
                        case SyntaxKind.SimpleAssignmentExpression:
                            var assignmentNode = descendantNode as AssignmentExpressionSyntax;
                            if(assignmentNode!=null)
                            {
                                //var symbInf = _checker._currentContext.SemanticModel.GetSymbolInfo(assignmentNode.Left);
                                //var assignSymb = symbInf.Symbol;
                                var assignSymb = getSymbolList(assignmentNode.Left);
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
                                //var symbInf = _checker._currentContext.SemanticModel.GetSymbolInfo(argNode.Expression);
                                //var argSymb = symbInf.Symbol;
                                var argSymb = getSymbolList(argNode.Expression);
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
                                //var symbInf = _checker._currentContext.SemanticModel.GetSymbolInfo(memberAccessNode.Expression);
                                //var elementAccessSymbol = symbInf.Symbol;
                                var elementAccessSymbol = getSymbolList(memberAccessNode.Expression);
                                if (_conditionVar != null && elementAccessSymbol != null &&
                                    (!_conditionVar.ContainsKey(elementAccessSymbol)|| _conditionVar[elementAccessSymbol])
                                  )
                                {
                                    if(_varSetAtNullInScope.ContainsKey(elementAccessSymbol))
                                    {
                                        if(!_varSetAtNullInScope[elementAccessSymbol])
                                        {
                                            var pos = memberAccessNode.GetLocation().GetMappedLineSpan();
                                            _checker.AddViolation(elementAccessSymbol.symbols[0], new[] { pos });
                                        }
                                    }
                                    else if(_varSetAtNullInAncestorScopes.ContainsKey(elementAccessSymbol))
                                    {
                                        if (!_varSetAtNullInAncestorScopes[elementAccessSymbol])
                                        {
                                            var pos = memberAccessNode.GetLocation().GetMappedLineSpan();
                                            _checker.AddViolation(elementAccessSymbol.symbols[0], new[] { pos });
                                        }
                                    }
                                }
                            }
                            break;
                        case SyntaxKind.ElementAccessExpression:
                            var elementAccessNode = descendantNode as ElementAccessExpressionSyntax;
                            if(elementAccessNode!=null)
                            {
                                //var symbInf = _checker._currentContext.SemanticModel.GetSymbolInfo(elementAccessNode.Expression);
                                //var elementAccessSymbol = symbInf.Symbol;
                                var elementAccessSymbol = getSymbolList(elementAccessNode.Expression);
                                if (_conditionVar != null && elementAccessSymbol != null &&
                                    (!_conditionVar.ContainsKey(elementAccessSymbol) || _conditionVar[elementAccessSymbol])
                                  )
                                {
                                    if (_varSetAtNullInScope.ContainsKey(elementAccessSymbol))
                                    {
                                        if (!_varSetAtNullInScope[elementAccessSymbol])
                                        {
                                            var pos = elementAccessNode.GetLocation().GetMappedLineSpan();
                                            _checker.AddViolation(elementAccessSymbol.symbols[0], new[] { pos });
                                        }
                                    }
                                    else if (_varSetAtNullInAncestorScopes.ContainsKey(elementAccessSymbol))
                                    {
                                        if (!_varSetAtNullInAncestorScopes[elementAccessSymbol])
                                        {
                                            var pos = elementAccessNode.GetLocation().GetMappedLineSpan();
                                            _checker.AddViolation(elementAccessSymbol.symbols[0], new[] { pos });
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

