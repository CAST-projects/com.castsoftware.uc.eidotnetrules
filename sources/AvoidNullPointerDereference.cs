﻿using System;
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
                    //if (node.Identifier.ValueText == "f40")
                    {
                        /*
                         *  This rule is based on finding the "null status" of variables in each scope of a method.
                         *  Therefore we use the Scope class as basis for this analysis.
                         *  This rule is limited to variables declared, assigned or tested "null" in the method.
                         */
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

        /*
         * This class is a container of a list of ISymbols. This list of symbols aims to represent variables of expressions such as "a.b.c" (multi access member).
         * Another aims is to verify the equality of 2 such lists.
         */
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

        // Comparer for equality of pair <SymbolList, bool>
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
        // Comparer for equality of SymbolList
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
        // Comparer for equality of pair <ISymbol, bool>
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

        public struct CheckNullInfos
        {
            public SymbolList varCheck;
            public bool equalOpr;
        }

        /*
         * Class representing a scope 
         */
        public class Scope
        {
            public SyntaxNode _scopeNode;
            public Scope _parentScope;
            // reference to the instance of AvoidNullPointerDereference to access AbstractRuleChecker methods.
            public AvoidNullPointerDereference _checker;
            // list of children scopes of this instance scope 
            public List<Scope> _childrenScope = new List<Scope>();
            // Dictionary containing the null status of variables in a scope, "false" is null and "true" is not null.
            // The symbol list is used to represent variables because of variable are frequently contains in another like a field in a class (In this example the list will contain the symbols of both the field and the instance of the class). 
            public Dictionary<SymbolList, bool> _varSetAtNullInScope = new Dictionary<SymbolList, bool>();
            //
            public Dictionary<SymbolList, CheckNullInfos> _varBoolCheckNullInVarDeclaration = new Dictionary<SymbolList, CheckNullInfos>();
            // Dictionary containing the null status of variables in the ancestors scopes, "false" is null and "true" is not null.
            public Dictionary<SymbolList, bool> _varSetAtNullInAncestorScopes;
            // Dictionary containing the null status of variables in the conditional scopes, "false" is null and "true" is not null.
            public Dictionary<SymbolList, bool> _conditionVar = new Dictionary<SymbolList, bool>();
            // Dictionary containing the null status of variables in the conditional scopes during its evaluations "expression by expression", "false" is null and "true" is not null.
            public Dictionary<SymbolList, bool> _conditionalState = new Dictionary<SymbolList, bool>();
            // Boolean indicating whether the scope contains a return statement.
            public bool ScopeContainsReturn = false;
            
            // this constructor MUST only be invoked one time for the scope of the METHOD
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
                _varBoolCheckNullInVarDeclaration = _parentScope._varBoolCheckNullInVarDeclaration.ToDictionary(x => x.Key, x => x.Value);
                _conditionVar = _parentScope._conditionVar.ToDictionary(x => x.Key, x => x.Value);
            }

            private Scope AddChildScope(SyntaxNode childNode)
            {
                Scope child = new Scope(childNode, this);
                _childrenScope.Add(child);
                return child;
            }

            /*
             * Get the symbols list of symbol from member access expression (ex: a.b.c.d).
             */
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
                            // check if the list of symbols "res" contains only one element and in that case if it's "this" (which is never null)
                            if (res.symbols.Count == 1 && res.symbols[0] is IParameterSymbol)
                            {
                                var paramSymb = res.symbols[0] as IParameterSymbol;
                                if(paramSymb!=null && paramSymb.IsThis)
                                {
                                    return listSymbols;
                                }
                            }
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

            public void HandleWithNullLiteralVar(SymbolList symb, ISymbol varDeclaration, bool elseBlock)
            {
                if (symb != null)
                {
                    if (varDeclaration != null)
                    {
                        CheckNullInfos nullInfos;
                        nullInfos.varCheck = symb;
                        nullInfos.equalOpr = !elseBlock;
                        _varBoolCheckNullInVarDeclaration[new SymbolList(varDeclaration)] = nullInfos;
                    }
                    else
                        _conditionVar[symb] = !elseBlock;
                }
            }

            // Check conditions variables tested against "null"
            public void CheckCondition(SyntaxNode node, bool elseBlock = false, ISymbol varDeclaration = null)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.LogicalAndExpression:
                        var andNode = node as BinaryExpressionSyntax;
                        if(andNode != null)
                        {
                            CheckCondition(andNode.Left, elseBlock, varDeclaration);
                            CheckCondition(andNode.Right, elseBlock, varDeclaration);
                        }
                        break;
                    case SyntaxKind.LogicalOrExpression:
                        var orNode = node as BinaryExpressionSyntax;
                        if (orNode != null)
                        {
                            CheckCondition(orNode.Left, elseBlock, varDeclaration);
                            CheckCondition(orNode.Right, elseBlock, varDeclaration);
                        }
                        break;
                    case SyntaxKind.EqualsExpression:
                        var equalNode = node as BinaryExpressionSyntax;
                        if(equalNode!=null)
                        {
                            if(equalNode.Right.IsKind(SyntaxKind.NullLiteralExpression)) // case: var == null
                            {
                                var equalSymb = getSymbolList(equalNode.Left);
                                HandleWithNullLiteralVar(equalSymb, varDeclaration, elseBlock);
                            }
                            else if (equalNode.Left.IsKind(SyntaxKind.NullLiteralExpression)) // case: null == var
                            {                                
                                var equalSymb = getSymbolList(equalNode.Right);
                                HandleWithNullLiteralVar(equalSymb, varDeclaration, elseBlock);
                            }
                        }
                        break;
                    case SyntaxKind.NotEqualsExpression:
                        var inequalNode = node as BinaryExpressionSyntax;
                        if (inequalNode != null)
                        {
                            if (inequalNode.Right.IsKind(SyntaxKind.NullLiteralExpression)) // case: var != null
                            {                                
                                var inequalSymb = getSymbolList(inequalNode.Left);
                                HandleWithNullLiteralVar(inequalSymb, varDeclaration, !elseBlock);
                            }
                            else if (inequalNode.Left.IsKind(SyntaxKind.NullLiteralExpression)) // case: null != var
                            {                                
                                var inequalSymb = getSymbolList(inequalNode.Right);
                                HandleWithNullLiteralVar(inequalSymb, varDeclaration, !elseBlock);
                            }
                        }
                        break;
                    case SyntaxKind.SimpleMemberAccessExpression:
                        var accessExpression = node as Microsoft.CodeAnalysis.CSharp.Syntax.MemberAccessExpressionSyntax;
                        if(accessExpression!=null)
                        {
                            node = accessExpression.Name;
                            goto case SyntaxKind.IdentifierName;
                        }
                        break;
                    case SyntaxKind.IdentifierName: // check condition on a boolean variable (case: if(valIsNotNull) where valIsNotNull = val!=null)

                        var parent = this._parentScope;
                        bool found = false;
                        while (parent != null && !found)
                        {
                            var childrenScopes = parent._childrenScope;
                            var identSymb = getSymbolList(node);
                            if (identSymb != null)
                            {
                                foreach (var child in childrenScopes)
                                {
                                    if (child._varBoolCheckNullInVarDeclaration.ContainsKey(identSymb))
                                    {
                                        var infos = child._varBoolCheckNullInVarDeclaration[identSymb];
                                        _conditionVar[infos.varCheck] = infos.equalOpr ? !elseBlock : elseBlock;
                                        found = true;
                                    }
                                }
                            }
                            parent = parent._parentScope;
                        }

                        break;
                    default:
                        break;
                }
                // Check conditional access expression "?."
                var descendantNodes = node.DescendantNodes().OfType<ConditionalAccessExpressionSyntax>();
                foreach(var conditionalAccessNode in descendantNodes)
                {
                    var identifier = conditionalAccessNode.Expression as IdentifierNameSyntax;
                    if(identifier!=null)
                    {
                        var identSymb = getSymbolList(identifier);
                        if (identSymb != null)
                            _conditionVar[identSymb] = elseBlock;
                    }
                }
            }

            //Use informations in conditions
            private void setConditionVar(Scope childScope)
            {
                foreach (var symb in childScope._conditionVar.Keys)
                {
                    if (childScope._conditionVar[symb] )
                    {
                        // variables tested null then instantiated don't raise violation later in the scope
                        if( childScope._varSetAtNullInAncestorScopes.ContainsKey(symb)
                            && childScope._varSetAtNullInAncestorScopes[symb])
                        {
                            if (_varSetAtNullInScope.ContainsKey(symb))
                            {
                                _varSetAtNullInScope[symb] = true;
                            }
                            else if (_varSetAtNullInAncestorScopes.ContainsKey(symb))
                            {
                                _varSetAtNullInAncestorScopes[symb] = true;
                            }
                        }
                        //if childScope contains a return statement then the null variable in the conditionVar cannot raise violation 
                        if (childScope.ScopeContainsReturn)
                        {
                            if (_varSetAtNullInScope.ContainsKey(symb))
                                _varSetAtNullInScope[symb] = true;
                            else if (_varSetAtNullInAncestorScopes.ContainsKey(symb))
                                _varSetAtNullInAncestorScopes[symb] = true;
                        }
                    }

                }
            }

            public void AnalyzeScope()
            {
                var descendantNodes = _scopeNode.DescendantNodes(DescendIntoChildren);
                if (descendantNodes.Count() == 0)
                    descendantNodes = descendantNodes.Append<SyntaxNode>(_scopeNode);
                Scope child;
                foreach (var descendantNode in descendantNodes)
                {
                    switch (descendantNode.Kind())
                    {
                        case SyntaxKind.VariableDeclarator:
                            AnalyseVariableDeclarator(descendantNode);
                            break;
                        case SyntaxKind.SimpleAssignmentExpression:
                            AnalyseSimpleAssignmentExpression(descendantNode);
                            break;
                        case SyntaxKind.Argument:
                            AnalyseArgument(descendantNode);
                            break;
                        case SyntaxKind.SimpleMemberAccessExpression:
                            AnalyseSimpleMemberAccessExpression(descendantNode);
                            break;
                        case SyntaxKind.ElementAccessExpression:
                            AnalyseElementAccessExpression(descendantNode);
                            break;                        
                        case SyntaxKind.IfStatement:
                            var ifNode = descendantNode as IfStatementSyntax;
                            if (ifNode != null)
                            {
                                Scope condition, thenBlock, elseBlock;
                                // check condition
                                condition = AddChildScope(ifNode.Condition);
                                condition.CheckCondition(ifNode.Condition);
                                //child.AnalyzeScope();
                                condition.AnalyseCondition(ifNode.Condition);
                                condition._conditionalState.Clear();
                                // check then block
                                thenBlock = AddChildScope(ifNode.Statement);
                                thenBlock.CheckCondition(ifNode.Condition);
                                foreach (var newConditionVar in thenBlock._conditionVar)
                                {
                                    if (newConditionVar.Value)
                                    {
                                        thenBlock._varSetAtNullInAncestorScopes[newConditionVar.Key] = !newConditionVar.Value;
                                    }
                                }
                                thenBlock.AnalyzeScope();
                                setConditionVar(thenBlock);
                                // check else block
                                if(ifNode.Else!=null)
                                {
                                    elseBlock = AddChildScope(ifNode.Else);
                                    elseBlock.CheckCondition(ifNode.Condition, true);
                                    foreach (var newConditionVar in elseBlock._conditionVar)
                                    {
                                        if (newConditionVar.Value)
                                        {
                                            elseBlock._varSetAtNullInAncestorScopes[newConditionVar.Key] = !newConditionVar.Value;
                                        }
                                    }
                                    elseBlock.AnalyzeScope();
                                    setConditionVar(elseBlock);
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
                        case SyntaxKind.ReturnStatement:
                            ScopeContainsReturn = true;
                            break;
                        
                        default:
                            break;
                    }
                }

            }

            public void AnalyseCondition(SyntaxNode node, bool logicalAndExpression=false)
            {
                var descendantNodes = node.DescendantNodes(DescendIntoChildren);
                //if (descendantNodes.Count() == 0)
                descendantNodes = descendantNodes.Append<SyntaxNode>(node);
                //Scope child;
                foreach (var descendantNode in descendantNodes)
                {
                    switch (descendantNode.Kind())
                    {
                        case SyntaxKind.Argument:
                            AnalyseArgument(descendantNode);
                            break;
                        case SyntaxKind.SimpleMemberAccessExpression:
                            AnalyseSimpleMemberAccessExpression(descendantNode);
                            break;
                        case SyntaxKind.ElementAccessExpression:
                            AnalyseElementAccessExpression(descendantNode);
                            break;
                        case SyntaxKind.LogicalOrExpression:
                                var orNode = descendantNode as BinaryExpressionSyntax;
                                if (orNode != null)
                                {
                                    AnalyseCondition(orNode.Left);
                                    AnalyseCondition(orNode.Right);
                                }
                            break;
                        case SyntaxKind.LogicalAndExpression:
                            var andNode = descendantNode as BinaryExpressionSyntax;
                                if (andNode != null)
                                {   
                                    AnalyseCondition(andNode.Left, true);
                                    AnalyseCondition(andNode.Right, true);
                                }
                            break;
                        case SyntaxKind.EqualsExpression:
                            var equalNode = descendantNode as BinaryExpressionSyntax;
                            if (equalNode != null)
                            {
                                if (equalNode.Right.IsKind(SyntaxKind.NullLiteralExpression)) // case: var == null
                                {
                                    var equalSymb = getSymbolList(equalNode.Left);
                                    if (equalSymb != null)
                                        _conditionalState[equalSymb] = !logicalAndExpression;
                                }
                                else if (equalNode.Left.IsKind(SyntaxKind.NullLiteralExpression)) // case: null == var
                                {
                                    var equalSymb = getSymbolList(equalNode.Right);
                                    if (equalSymb != null)
                                        _conditionalState[equalSymb] = !logicalAndExpression;
                                }
                            }
                            break;
                        case SyntaxKind.NotEqualsExpression:
                            var inequalNode = descendantNode as BinaryExpressionSyntax;
                            if (inequalNode != null)
                            {
                                if (inequalNode.Right.IsKind(SyntaxKind.NullLiteralExpression)) // case: var != null
                                {
                                    var inequalSymb = getSymbolList(inequalNode.Left);
                                    if (inequalSymb != null)
                                        _conditionalState[inequalSymb] = logicalAndExpression;
                                }
                                else if (inequalNode.Left.IsKind(SyntaxKind.NullLiteralExpression)) // case: null != var
                                {
                                    var inequalSymb = getSymbolList(inequalNode.Right);
                                    if (inequalSymb != null)
                                        _conditionalState[inequalSymb] = logicalAndExpression;
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }

            }

            public bool IsKindBoolCondition(ExpressionSyntax expSyntax)
            {
                return expSyntax.IsKind(SyntaxKind.LogicalAndExpression) ||
                       expSyntax.IsKind(SyntaxKind.LogicalOrExpression) ||
                       expSyntax.IsKind(SyntaxKind.LogicalNotExpression) ||
                       expSyntax.IsKind(SyntaxKind.EqualsExpression) ||
                       expSyntax.IsKind(SyntaxKind.NotEqualsExpression);
            }

            public void AnalyseVariableDeclarator(SyntaxNode node)
            {
                var declaratorNode = node as VariableDeclaratorSyntax;
                if (declaratorNode != null && declaratorNode.Initializer != null)
                {
                    if (declaratorNode.Initializer.IsKind(SyntaxKind.EqualsValueClause)) 
                    {
                        if (declaratorNode.Initializer.Value.IsKind(SyntaxKind.NullLiteralExpression)) // null initialization
                        {
                            var declarSymb = _checker._currentContext.SemanticModel.GetDeclaredSymbol(declaratorNode);
                            if (declarSymb != null)
                            {
                                _varSetAtNullInScope[new SymbolList(declarSymb)] = false;
                            }
                        }
                        else if (IsKindBoolCondition(declaratorNode.Initializer.Value))
                        {

                            var declarSymb = _checker._currentContext.SemanticModel.GetDeclaredSymbol(declaratorNode);
                            // Check bool condition in variable declaration
                            // stock for if and check access expression violation in bool condition
                            Scope conditionInVarDeclaration;
                            conditionInVarDeclaration = AddChildScope(declaratorNode.Initializer.Value);

                            // check violation inside.
                            conditionInVarDeclaration.AnalyseCondition(declaratorNode.Initializer.Value);
                            conditionInVarDeclaration._conditionalState.Clear();

                            // stock and add when check if statement
                            conditionInVarDeclaration.CheckCondition(declaratorNode.Initializer.Value, false, declarSymb);
                        }
                    }
                }
            }

            public void AnalyseSimpleAssignmentExpression(SyntaxNode node)
            {
                var assignmentNode = node as AssignmentExpressionSyntax;
                if (assignmentNode != null)
                {
                    var assignSymb = getSymbolList(assignmentNode.Left);
                    if (assignSymb != null)
                    {
                        if (assignmentNode.Right.IsKind(SyntaxKind.NullLiteralExpression)) // null assignment
                        {
                            _varSetAtNullInScope[assignSymb] = false;
                        }
                        else if (IsKindBoolCondition(assignmentNode.Right)) // check for case "val = var!=null;"
                        {
                            SymbolInfo symbInf = _checker._currentContext.SemanticModel.GetSymbolInfo(assignmentNode.Left);
                            ISymbol leftSymb = symbInf.Symbol;
                            if (leftSymb != null)
                            { // Check bool condition in variable assignment
                                // stock for if and check access expression violation in bool condition
                                Scope conditionInVarAssignment;
                                conditionInVarAssignment = AddChildScope(assignmentNode.Right);

                                // check violation inside.
                                conditionInVarAssignment.AnalyseCondition(assignmentNode.Right);
                                conditionInVarAssignment._conditionalState.Clear();

                                // stock and add when check if statement
                                conditionInVarAssignment.CheckCondition(assignmentNode.Right, false, leftSymb);
                            }
                        }
                        else
                        {
                            var rightSymb = getSymbolList(assignmentNode.Right);
                            bool instantiated = true;
                            if (_varSetAtNullInScope.ContainsKey(assignSymb))
                            {
                                if (rightSymb != null && _varSetAtNullInScope.ContainsKey(rightSymb))
                                {
                                    instantiated = _varSetAtNullInScope[rightSymb];
                                }
                                _varSetAtNullInScope[assignSymb] = instantiated;
                            }
                            else if (_varSetAtNullInAncestorScopes.ContainsKey(assignSymb))
                            {
                                if (rightSymb != null && _varSetAtNullInAncestorScopes.ContainsKey(rightSymb))
                                {
                                    instantiated = _varSetAtNullInAncestorScopes[rightSymb];
                                }
                                _varSetAtNullInAncestorScopes[assignSymb] = instantiated;
                            }
                        }

                    }
                }
            }

            public void AnalyseArgument(SyntaxNode node)
            {
                var argNode = node as ArgumentSyntax;
                if (argNode != null && argNode.RefKindKeyword.ValueText == "out")
                {
                    var argSymb = getSymbolList(argNode.Expression);
                    if (argSymb != null && _varSetAtNullInScope.ContainsKey(argSymb))
                    {
                        _varSetAtNullInScope[argSymb] = true;
                    }
                }
            }

            public void AnalyseSimpleMemberAccessExpression(SyntaxNode node)
            {
                var memberAccessNode = node as MemberAccessExpressionSyntax;
                if (memberAccessNode != null)
                {
                    var elementAccessSymbol = getSymbolList(memberAccessNode.Expression);
                    if (_conditionVar != null && elementAccessSymbol != null &&
                        (!_conditionVar.ContainsKey(elementAccessSymbol) || _conditionVar[elementAccessSymbol])
                      )
                    {
                        if (_conditionalState.ContainsKey(elementAccessSymbol))
                        {
                            if (!_conditionalState[elementAccessSymbol])
                            {
                                var pos = memberAccessNode.GetLocation().GetMappedLineSpan();
                                _checker.AddViolation(elementAccessSymbol.symbols[0].ContainingSymbol, new[] { pos });
                            }
                        }
                        else if (_varSetAtNullInScope.ContainsKey(elementAccessSymbol))
                        {
                            if (!_varSetAtNullInScope[elementAccessSymbol])
                            {
                                var pos = memberAccessNode.GetLocation().GetMappedLineSpan();
                                _checker.AddViolation(elementAccessSymbol.symbols[0].ContainingSymbol, new[] { pos });
                            }
                        }
                        else if (_varSetAtNullInAncestorScopes.ContainsKey(elementAccessSymbol))
                        {
                            if (!_varSetAtNullInAncestorScopes[elementAccessSymbol])
                            {
                                var pos = memberAccessNode.GetLocation().GetMappedLineSpan();
                                _checker.AddViolation(elementAccessSymbol.symbols[0].ContainingSymbol, new[] { pos });
                            }
                        }
                    }
                }
            }

            public void AnalyseElementAccessExpression(SyntaxNode node)
            {
                var elementAccessNode = node as ElementAccessExpressionSyntax;
                if (elementAccessNode != null)
                {
                    var elementAccessSymbol = getSymbolList(elementAccessNode.Expression);
                    if (_conditionVar != null && elementAccessSymbol != null &&
                        (!_conditionVar.ContainsKey(elementAccessSymbol) || _conditionVar[elementAccessSymbol])
                      )
                    {
                        if (_conditionalState.ContainsKey(elementAccessSymbol))
                        {
                            if (!_conditionalState[elementAccessSymbol])
                            {
                                var pos = elementAccessNode.GetLocation().GetMappedLineSpan();
                                _checker.AddViolation(elementAccessSymbol.symbols[0].ContainingSymbol, new[] { pos });
                            }
                        }
                        else if (_varSetAtNullInScope.ContainsKey(elementAccessSymbol))
                        {
                            if (!_varSetAtNullInScope[elementAccessSymbol])
                            {
                                var pos = elementAccessNode.GetLocation().GetMappedLineSpan();
                                _checker.AddViolation(elementAccessSymbol.symbols[0].ContainingSymbol, new[] { pos });
                            }
                        }
                        else if (_varSetAtNullInAncestorScopes.ContainsKey(elementAccessSymbol))
                        {
                            if (!_varSetAtNullInAncestorScopes[elementAccessSymbol])
                            {
                                var pos = elementAccessNode.GetLocation().GetMappedLineSpan();
                                _checker.AddViolation(elementAccessSymbol.symbols[0].ContainingSymbol, new[] { pos });
                            }
                        }
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
                case SyntaxKind.LogicalOrExpression:
                case SyntaxKind.LogicalAndExpression:
                    return false;
                default:
                    return true;
            }
        }


    }
}

