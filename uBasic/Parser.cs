using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace uBasic
{
    public class Parser
    {
        public class AstNode
        {
            public int LineNumber { get; }
            public int ColumnNumber { get; }

            public AstNode(int lineNumber, int columnNumber)
            {
                LineNumber = lineNumber;
                ColumnNumber = columnNumber;
            }

        }

        public class AstToken : AstNode
        {
            private Token token;
            public Token_Type Type { get { return token.Type; } }
            public string SourceCode { get { return token.Text; } }

            public AstToken(Token token) : base(token.LineNumber, token.ColumnNumber)
            {
                this.token = token;
            }
        };

        public class AstInteger : AstToken
        {
            public int Value
            {
                get
                {
                    if (!int.TryParse(this.SourceCode, out int parsed))
                        parsed = 0;
                    return parsed;
                }
            }

            public AstInteger(Token token) : base(token) { }
        }

        public class AstFloat : AstToken
        {
            public double Value
            {
                get
                {
                    if (!double.TryParse(this.SourceCode, out double parsed))
                        parsed = 0.0;
                    return parsed;
                }
            }

            public AstFloat(Token token) : base(token) { }
        }

        public class AstBoolean : AstToken
        {
            public bool Value
            {
                get
                {
                    return this.SourceCode.ToUpper().Trim().Equals("TRUE");
                }
            }
            public AstBoolean(Token token) : base(token) { }
        }

        public class AstString : AstToken
        {
            public string Value
            {
                get
                {
                    int length = this.SourceCode.Length - 2;
                    if (length <= 0)
                        return "";
                    return this.SourceCode.Substring(1, length).Replace("\\\"", "\"");
                }
            }
            public AstString(Token token) : base(token) { }
        }

        public class AstVariable : AstToken
        {
            public string Name
            {
                get
                {
                    return this.SourceCode;
                }
            }

            public AstVariable(Token token) : base(token) { }

            public override string ToString()
            {
                return Name;
            }

        }

        public class AstLet : AstNode
        {
            /*
             LET Id '=' <Expression> 
             */
            public AstVariable? variable;
            public AstArrayAccess? arrayRef;
            public AstExpression? expression;
            public AstLet(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                arrayRef = null;
                variable = null;
                expression = null;
            }

            public AstNode? Get()
            {
                if ((arrayRef != null || variable != null) && expression != null)
                    return this;
                return null;
            }

            public void Set(AstVariable? name, AstExpression? value)
            {
                arrayRef = null;
                variable = name;
                expression = value;
            }

            public void Set(AstArrayAccess? name, AstExpression? value)
            {
                arrayRef = name;
                variable = null;
                expression = value;
            }

            public override string ToString()
            {
                if (variable != null && expression != null)
                    return $"LET {variable} = {expression}";
                else if (arrayRef != null && expression != null)
                    return $"LET {arrayRef} = {expression}";
                return "null";
            }
        }

        public class AstConstant : AstNode
        {
            /*
             <Constant> ::= Integer 
             | String 
             | Float
             | Boolean
             */
            public AstBoolean? nodeBool;
            public AstInteger? nodeInt;
            public AstFloat? nodeFloat;
            public AstString? nodeString;
            public AstConstant(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                if (t.Type == Token_Type.TOKEN_TRUE || t.Type == Token_Type.TOKEN_FALSE)
                {
                    this.Set(new AstBoolean(t));
                }
                else if (t.Type == Token_Type.TOKEN_INTEGER)
                {
                    this.Set(new AstInteger(t));
                }
                else if (t.Type == Token_Type.TOKEN_FLOAT)
                {
                    this.Set(new AstFloat(t));
                }
                else if (t.Type == Token_Type.TOKEN_STRING)
                {
                    this.Set(new AstString(t));
                }
                else
                {
                    this.nodeBool = null;
                    this.nodeInt = null;
                    this.nodeFloat = null;
                    this.nodeString = null;
                }
            }

            public AstNode? Get()
            {
                return nodeString ?? nodeFloat ?? (AstNode?)nodeInt ?? nodeBool ?? null;
            }
            public void Set(AstBoolean node)
            {
                nodeBool = node;
                nodeInt = null;
                nodeFloat = null;
                nodeString = null;
            }
            public void Set(AstInteger node)
            {
                nodeBool = null;
                nodeInt = node;
                nodeFloat = null;
                nodeString = null;
            }
            public void Set(AstFloat node)
            {
                nodeBool = null;
                nodeInt = null;
                nodeFloat = node;
                nodeString = null;
            }
            public void Set(AstString node)
            {
                nodeBool = null;
                nodeInt = null;
                nodeFloat = null;
                nodeString = node;
            }

            public override string ToString()
            {
                if (nodeBool != null)
                {
                    return $"{nodeBool.Value}";
                }
                else if (nodeInt != null)
                {
                    return $"{nodeInt.Value}";
                }
                else if (nodeFloat != null)
                {
                    return $"{nodeFloat.Value}";
                }
                else if (nodeString != null)
                {
                    return $"\"{nodeString.Value}\"";
                }
                else
                    return $"Error: Bad Constant - NULL";
            }
        }

        public class AstFunctionCall : AstNode
        {
            public string function;
            public AstExpressionList? args;
            public AstFunctionCall(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                function = t.Text;
                args = null;
            }

            public void Set(AstExpressionList exps)
            {
                args = exps;
            }

            public void AddArg(AstExpression? exp)
            {
                if (exp != null)
                {
                    if (args == null)
                    {
                        Token t = new Token();
                        t.LineNumber = exp.LineNumber;
                        t.ColumnNumber = exp.ColumnNumber;
                        t.Type = Token_Type.TOKEN_NONE;
                        args = new AstExpressionList(t);
                    }
                    args.Add(exp);
                }
            }

            public override string ToString()
            {
                string argString = "";
                if (function == null)
                    return "null";
                if (args != null)
                    argString = args.ToString();
                return $"{function}({argString})";
            }

        }

        public class AstValue : AstNode
        {
            /*
             <Value>       ::= 
                '(' <Expression> ')'
                | ID 
                | ID '(' <Expression List> ')'
                | <Constant>
             */
            public AstVariable? nodeVariable;
            public AstConstant? nodeConstant;
            public AstExpression? nodeExpression;
            public AstFunctionCall? nodeFunctionCall;
            public AstArrayAccess? nodeArrayAccess;

            public AstValue(Token t) : base(t.LineNumber, t.ColumnNumber) 
            {
                nodeArrayAccess = null;
                nodeConstant = null;
                nodeExpression = null;
                nodeFunctionCall = null;
                nodeVariable = null;
            }
            public void Set(AstArrayAccess node)
            {
                nodeArrayAccess = node;
                nodeConstant = null;
                nodeExpression = null;
                nodeFunctionCall = null;
                nodeVariable = null;
            }
            public void Set(AstExpression node)
            {
                nodeArrayAccess = null;
                nodeConstant = null;
                nodeExpression = node;
                nodeFunctionCall = null;
                nodeVariable = null;
            }
            public void Set(AstFunctionCall node)
            {
                nodeArrayAccess = null;
                nodeConstant = null;
                nodeExpression = null;
                nodeFunctionCall = node;
                nodeVariable = null;
            }

            public void Set(AstVariable node)
            {
                nodeArrayAccess = null;
                nodeConstant = null;
                nodeExpression = null;
                nodeFunctionCall = null;
                nodeVariable = node;
            }

            public void Set(AstConstant node)
            {
                nodeArrayAccess = null;
                nodeConstant = node;
                nodeExpression = null;
                nodeVariable = null;
            }

            public AstNode? Get()
            {
                return nodeArrayAccess ?? nodeConstant ?? nodeExpression ?? nodeFunctionCall ?? (AstNode?) nodeVariable ?? null;
            }

            public override string ToString()
            {
                return $"{Get()}";
            }
        }

        public class AstPowerExpression : AstNode
        {
            public AstPowerExpression? powerExpression;
            public AstValue? value;
            public AstPowerExpression(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                powerExpression = null;
                value = null;
            }

            public AstNode? Get()
            {
                return (AstNode?)powerExpression ?? value ?? null;
            }

            public void Set(AstPowerExpression node, AstValue nodeValue)
            {
                powerExpression = node;
                value = nodeValue;
            }

            public void Set(AstValue node)
            {
                powerExpression = null;
                value = node;
            }

            public override string ToString()
            {
                if (powerExpression != null && value != null)
                    return $"{powerExpression} ^ {value}";
                else if (value != null)
                    return $"{value}";
                else
                    return "null";

            }
        }
        public class AstNegateExpression : AstNode
        {
            public AstPowerExpression? powerExpression;
            public bool negate;
            public AstNegateExpression(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                powerExpression = null;
                negate = false;
            }

            public AstNode? Get(out bool isNegated)
            {
                isNegated = negate;
                return (AstNode?)powerExpression;
            }

            public void Set(AstPowerExpression node, bool isNegated)
            {
                powerExpression = node;
                negate = isNegated;
            }

            public override string ToString()
            {
                if (powerExpression != null && negate)
                    return $"-{powerExpression}";
                else if (powerExpression != null)
                    return $"{powerExpression}";
                else
                    return "null";

            }
        }

        public class AstMultiplyExpression : AstNode
        {
            public AstNegateExpression? negateExpression;
            public AstMultiplyExpression? multiplyExpression;
            public Token? op;

            public AstMultiplyExpression(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                negateExpression = null;
                multiplyExpression = null;
                op = null;
            }

            public AstNode? Get()
            {
                if (negateExpression != null && multiplyExpression != null)
                    return this;
                else if (negateExpression != null)
                    return negateExpression;
                return null;
            }

            public void Set(AstNegateExpression lhs, Token t, AstMultiplyExpression rhs)
            {
                negateExpression = lhs;
                multiplyExpression = rhs;
                op = t;
            }

            public void Set(AstNegateExpression lhs)
            {
                negateExpression = lhs;
                multiplyExpression = null;
                op = null;
            }

            public override string ToString()
            {
                string operation = "";
                if (op != null)
                {
                    if (op.Type == Token_Type.TOKEN_MULTIPLY)
                        operation = "*";
                    else if (op.Type == Token_Type.TOKEN_DIVIDE)
                        operation = "/";
                }

                if (negateExpression != null && multiplyExpression != null)
                    return $"{negateExpression} {operation} {multiplyExpression}";
                else if (negateExpression != null)
                    return $"{negateExpression}";
                else
                    return "null";
            }
        }


        public class AstAddExpression : AstNode
        {
            public AstMultiplyExpression? multiplyExpression;
            public AstAddExpression? addExpression;
            public Token? op;

            public AstAddExpression(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                multiplyExpression = null;
                addExpression = null;
                op = null;
            }

            public AstNode? Get()
            {
                if (multiplyExpression != null && addExpression != null)
                    return this;
                else if (multiplyExpression != null)
                    return multiplyExpression;
                return null;
            }

            public void Set(AstMultiplyExpression lhs, Token t, AstAddExpression rhs)
            {
                multiplyExpression = lhs;
                addExpression = rhs;
                op = t;
            }
            public void Set(AstMultiplyExpression lhs)
            {
                multiplyExpression = lhs;
                addExpression = null;
                op = null;
            }
            public override string ToString()
            {
                string operation = "";
                if (op != null)
                {
                    if (op.Type == Token_Type.TOKEN_ADD)
                        operation = "+";
                    else if (op.Type == Token_Type.TOKEN_SUBTRACT)
                        operation = "-";
                }
                if (multiplyExpression != null && addExpression != null)
                    return $"{multiplyExpression} {operation} {addExpression}";
                else if (multiplyExpression != null)
                    return $"{multiplyExpression}";
                else
                    return "null";
            }
        }

        public class AstCompareExpression : AstNode
        {
            /*
            <Compare Exp> ::= <Add Exp> '=='  <Compare Exp> 
                | <Add Exp> '<>' <Compare Exp> 
                | <Add Exp> '!=' <Compare Exp> 
                | <Add Exp> '>'  <Compare Exp> 
                | <Add Exp> '>=' <Compare Exp> 
                | <Add Exp> '<'  <Compare Exp> 
                | <Add Exp> '<=' <Compare Exp> 
                | <Add Exp> 
             */
            public AstAddExpression? lhs;
            public AstCompareExpression? rhs;
            public Token? op;
            public AstCompareExpression(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                lhs = null;
                rhs = null;
                op = null;
            }

            public AstNode? Get()
            {
                if (lhs != null && op != null && rhs != null)
                    return this;
                else if (lhs != null)
                    return lhs;
                return null;
            }

            public void Set(AstAddExpression exp)
            {
                lhs = exp;
                rhs = null;
            }

            public void Set(AstAddExpression add, Token t, AstCompareExpression compare)
            {
                lhs = add;
                op = t;
                rhs = compare;
            }

            public override string ToString()
            {
                if (lhs != null && op != null && rhs != null)
                {
                    return $"{lhs} {op.Text} {rhs}";
                }
                else if (lhs != null)
                {
                    return $"{lhs}";
                }
                else
                {
                    return "null";
                }
            }
        }

        public class AstNotExpression : AstNode
        {
            /*
             <Not Exp>     ::= NOT <Compare Exp> 
                            | <Compare Exp> 
             */
            public bool negate;
            public AstCompareExpression? compare;
            public AstNotExpression(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                negate = false;
                compare = null;
            }

            public AstNode? Get(out bool isNegated)
            {
                isNegated = negate;
                return compare;
            }

            public void Set(bool isNegated, AstCompareExpression? value)
            {
                negate = isNegated;
                compare = value;
            }

            public void Set(AstCompareExpression? value)
            {
                negate = false;
                compare = value;
            }

            public override string ToString()
            {
                if (negate && compare != null)
                {
                    return $"NOT {compare}";
                }
                else if (compare != null)
                {
                    return $"{compare}";
                }
                return "null";
            }
        }

        public class AstAndExpression : AstNode
        {
            /*
             <And Exp>     ::= <Not Exp> AND <And Exp> 
                                | <Not Exp>
             */
            public AstNotExpression? lhs;
            public AstAndExpression? rhs;
            public AstAndExpression(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                lhs = null;
                rhs = null;
            }

            public AstNode? Get()
            {
                if (lhs != null && rhs != null)
                {
                    return this;
                }
                if (lhs != null && rhs == null)
                {
                    return lhs;
                }
                return null;
            }

            public void Set(AstNotExpression? first, AstAndExpression? second)
            {
                lhs = first;
                rhs = second;
            }

            public void Set(AstNotExpression? first)
            {
                lhs = first;
                rhs = null;
            }

            public override string ToString()
            {
                if (lhs != null && rhs != null)
                {
                    return $"{lhs} AND {rhs}";
                }
                if (lhs != null && rhs == null)
                {
                    return $"{lhs}";
                }
                return "null";
            }
        }

        public class AstExpression : AstNode
        {
            /*
             <Expression>  ::= <And Exp> OR <Expression> 
                | <And Exp> 
             */
            public AstAndExpression? lhs;
            public AstExpression? rhs;
            public AstExpression(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                lhs = null;
                rhs = null;
            }

            public AstNode? Get()
            {
                if (lhs != null && rhs != null)
                {
                    return this;
                }
                if (lhs != null && rhs == null)
                {
                    return lhs;
                }
                return null;
            }

            public void Set(AstAndExpression? first, AstExpression? second)
            {
                lhs = first;
                rhs = second;
            }

            public void Set(AstAndExpression? first)
            {
                lhs = first;
                rhs = null;
            }

            public override string ToString()
            {
                if (lhs != null && rhs != null)
                {
                    return $"{lhs} OR {rhs}";
                }
                if (lhs != null && rhs == null)
                {
                    return $"{lhs}";
                }
                return "null";
            }
        }

        public class AstExpressionList : AstNode
        {
            public List<AstExpression>? expList;

            public AstExpressionList(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                expList = null;
            }

            public AstNode? Get()
            {
                if (expList != null)
                    return this;
                return null;
            }

            public void Add(AstExpression exp)
            {
                if (expList == null)
                {
                    expList = new List<AstExpression>();
                }
                expList.Add(exp);
            }

            public void Set(List<AstExpression>? exps)
            {
                expList = new List<AstExpression>();
                if (exps != null)
                    expList.AddRange(exps);
            }

            public override string ToString()
            {
                if (expList == null)
                    return "";
                return String.Join(", ", (from exp in expList
                                           select $"{exp}"));
            }
        }

        public class AstStatement : AstNode
        {
            public AstComment? stmtComment;
            public AstData? stmtData;
            public AstDim? stmtDim;
            public AstEnd? stmtEnd;
            public AstFileOpen? stmtFileOpen;
            public AstFileClose? stmtFileClose;
            public AstFor? stmtFor;
            public AstForNext? stmtForNext;
            public AstFunctionCall? stmtFunctionCall;
            public AstGosub? stmtGosub;
            public AstGoto? stmtGoto;
            public AstIf? stmtIf;
            public AstIfElseIf? stmtIfElseIf;
            public AstIfElse? stmtIfElse;
            public AstIfEndIf? stmtIfEndIf;
            public AstInput? stmtInput;
            public AstLet? stmtLet;
            public AstPrint? stmtPrint;
            public AstRead? stmtRead;
            public AstRestore? stmtRestore;
            public AstReturn? stmtReturn;


            public AstStatement(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                stmtComment = null;
                stmtData = null;
                stmtData = null;
                stmtDim = null;
                stmtEnd = null;
                stmtFileOpen = null;
                stmtFileClose = null;
                stmtFor = null;
                stmtForNext = null;
                stmtFunctionCall = null;
                stmtGosub = null;
                stmtGoto = null;
                stmtIf = null;
                stmtIfElseIf = null;
                stmtIfElse = null;
                stmtIfEndIf = null;
                stmtInput = null;
                stmtLet = null;
                stmtPrint = null;
                stmtRead = null;
                stmtRestore = null;
                stmtReturn = null;
            }

            public void Set(AstComment? stmt)
            {
                stmtComment = stmt;
                stmtData = null;
                stmtDim = null;
                stmtEnd = null;
                stmtFileOpen = null;
                stmtFileClose = null; 
                stmtFor = null;
                stmtForNext = null;
                stmtFunctionCall = null;
                stmtGosub = null;
                stmtGoto = null;
                stmtIf = null;
                stmtIfElseIf = null;
                stmtIfElse = null;
                stmtIfEndIf = null;
                stmtInput = null;
                stmtLet = null;
                stmtPrint = null;
                stmtRead = null;
                stmtRestore = null;
                stmtReturn = null;
            }
            public void Set(AstData? stmt)
            {
                stmtComment = null;
                stmtData = stmt;
                stmtDim = null;
                stmtEnd = null;
                stmtFileOpen = null;
                stmtFileClose = null; 
                stmtFor = null;
                stmtForNext = null;
                stmtFunctionCall = null;
                stmtGosub = null;
                stmtGoto = null;
                stmtIf = null;
                stmtIfElseIf = null;
                stmtIfElse = null;
                stmtIfEndIf = null;
                stmtInput = null;
                stmtLet = null;
                stmtPrint = null;
                stmtRead = null;
                stmtRestore = null;
                stmtReturn = null;
            }
            public void Set(AstDim? stmt)
            {
                stmtComment = null;
                stmtData = null;
                stmtDim = stmt;
                stmtEnd = null;
                stmtFileOpen = null;
                stmtFileClose = null; 
                stmtFor = null;
                stmtForNext = null;
                stmtFunctionCall = null;
                stmtGosub = null;
                stmtGoto = null;
                stmtIf = null;
                stmtIfElseIf = null;
                stmtIfElse = null;
                stmtIfEndIf = null;
                stmtInput = null;
                stmtLet = null;
                stmtPrint = null;
                stmtRead = null;
                stmtRestore = null;
                stmtReturn = null;
            }
            
            public void Set(AstEnd? stmt)
            {
                stmtComment = null;
                stmtData = null; 
                stmtDim = null;
                stmtEnd = stmt;
                stmtFileOpen = null;
                stmtFileClose = null; 
                stmtFor = null;
                stmtForNext = null;
                stmtFunctionCall = null;
                stmtGosub = null;
                stmtGoto = null;
                stmtIf = null;
                stmtIfElseIf = null;
                stmtIfElse = null;
                stmtIfEndIf = null;
                stmtInput = null;
                stmtLet = null;
                stmtPrint = null;
                stmtRead = null;
                stmtRestore = null;
                stmtReturn = null;
            }

            public void Set(AstFileOpen? stmt)
            {
                stmtComment = null;
                stmtData = null; 
                stmtDim = null;
                stmtEnd = null;
                stmtFileOpen = stmt;
                stmtFileClose = null; 
                stmtFor = null;
                stmtForNext = null;
                stmtFunctionCall = null;
                stmtGosub = null;
                stmtGoto = null;
                stmtIf = null;
                stmtIfElseIf = null;
                stmtIfElse = null;
                stmtIfEndIf = null;
                stmtInput = null;
                stmtLet = null;
                stmtPrint = null;
                stmtRead = null;
                stmtRestore = null;
                stmtReturn = null;
            }

            public void Set(AstFileClose? stmt)
            {
                stmtComment = null;
                stmtData = null;
                stmtDim = null;
                stmtEnd = null;
                stmtFileOpen = null;
                stmtFileClose = stmt;
                stmtFor = null;
                stmtForNext = null;
                stmtFunctionCall = null;
                stmtGosub = null;
                stmtGoto = null;
                stmtIf = null;
                stmtIfElseIf = null;
                stmtIfElse = null;
                stmtIfEndIf = null;
                stmtInput = null;
                stmtLet = null;
                stmtPrint = null;
                stmtRead = null;
                stmtRestore = null;
                stmtReturn = null;
            }

            public void Set(AstFor? stmt)
            {
                stmtComment = null;
                stmtData = null;
                stmtDim = null;
                stmtEnd = null;
                stmtFileOpen = null;
                stmtFileClose = null;
                stmtFor = stmt;
                stmtForNext = null;
                stmtFunctionCall = null;
                stmtGosub = null;
                stmtGoto = null;
                stmtIf = null;
                stmtIfElseIf = null;
                stmtIfElse = null;
                stmtIfEndIf = null;
                stmtInput = null;
                stmtLet = null;
                stmtPrint = null;
                stmtRead = null;
                stmtRestore = null;
                stmtReturn = null;
            }

            public void Set(AstForNext? stmt)
            {
                stmtComment = null;
                stmtData = null; 
                stmtDim = null;
                stmtEnd = null;
                stmtFileOpen = null;
                stmtFileClose = null; 
                stmtFor = null;
                stmtForNext = stmt;
                stmtFunctionCall = null;
                stmtGosub = null;
                stmtGoto = null;
                stmtIf = null;
                stmtIfElseIf = null;
                stmtIfElse = null;
                stmtIfEndIf = null;
                stmtInput = null;
                stmtLet = null;
                stmtPrint = null;
                stmtRead = null;
                stmtRestore = null;
                stmtReturn = null;
            }

            public void Set(AstFunctionCall? stmt)
            {
                stmtComment = null;
                stmtData = null;
                stmtDim = null;
                stmtEnd = null;
                stmtFileOpen = null;
                stmtFileClose = null; 
                stmtFor = null;
                stmtForNext = null;
                stmtFunctionCall = stmt;
                stmtGosub = null;
                stmtGoto = null;
                stmtIf = null;
                stmtIfElseIf = null;
                stmtIfElse = null;
                stmtIfEndIf = null;
                stmtInput = null;
                stmtLet = null;
                stmtPrint = null;
                stmtRead = null;
                stmtRestore = null;
                stmtReturn = null;
            }

            public void Set(AstGosub? stmt)
            {
                stmtComment = null;
                stmtData = null; 
                stmtDim = null;
                stmtEnd = null;
                stmtFileOpen = null;
                stmtFileClose = null; 
                stmtFor = null;
                stmtForNext = null;
                stmtFunctionCall = null;
                stmtGosub = stmt;
                stmtGoto = null;
                stmtIf = null;
                stmtIfElseIf = null;
                stmtIfElse = null;
                stmtIfEndIf = null;
                stmtInput = null;
                stmtLet = null;
                stmtPrint = null;
                stmtRead = null;
                stmtRestore = null;
                stmtReturn = null;
            }

            public void Set(AstGoto? stmt)
            {
                stmtComment = null;
                stmtData = null; 
                stmtDim = null;
                stmtEnd = null;
                stmtFileOpen = null;
                stmtFileClose = null; 
                stmtFor = null;
                stmtForNext = null;
                stmtFunctionCall = null;
                stmtGosub = null;
                stmtGoto = stmt;
                stmtIf = null;
                stmtIfElseIf = null;
                stmtIfElse = null;
                stmtIfEndIf = null;
                stmtInput = null;
                stmtLet = null;
                stmtPrint = null;
                stmtRead = null;
                stmtRestore = null;
                stmtReturn = null;
            }

            public void Set(AstIf? stmt)
            {
                stmtComment = null;
                stmtData = null; 
                stmtDim = null;
                stmtEnd = null;
                stmtFileOpen = null;
                stmtFileClose = null; 
                stmtFor = null;
                stmtForNext = null;
                stmtFunctionCall = null;
                stmtGosub = null;
                stmtGoto = null;
                stmtIf = stmt;
                stmtIfElseIf = null;
                stmtIfElse = null;
                stmtIfEndIf = null;
                stmtInput = null;
                stmtLet = null;
                stmtPrint = null;
                stmtRead = null;
                stmtRestore = null;
                stmtReturn = null;
            }

            public void Set(AstIfElseIf? stmt)
            {
                stmtComment = null;
                stmtData = null; 
                stmtDim = null;
                stmtEnd = null;
                stmtFileOpen = null;
                stmtFileClose = null; 
                stmtFor = null;
                stmtForNext = null;
                stmtFunctionCall = null;
                stmtGosub = null;
                stmtGoto = null;
                stmtIf = null;
                stmtIfElseIf = stmt;
                stmtIfElse = null;
                stmtIfEndIf = null;
                stmtInput = null;
                stmtLet = null;
                stmtPrint = null;
                stmtRead = null;
                stmtRestore = null;
                stmtReturn = null;
            }

            public void Set(AstIfElse? stmt)
            {
                stmtComment = null;
                stmtData = null; 
                stmtDim = null;
                stmtEnd = null;
                stmtFileOpen = null;
                stmtFileClose = null; 
                stmtFor = null;
                stmtForNext = null;
                stmtFunctionCall = null;
                stmtGosub = null;
                stmtGoto = null;
                stmtIf = null;
                stmtIfElseIf = null;
                stmtIfElse = stmt;
                stmtIfEndIf = null;
                stmtInput = null;
                stmtLet = null;
                stmtPrint = null;
                stmtRead = null;
                stmtRestore = null;
                stmtReturn = null;
            }

            public void Set(AstIfEndIf? stmt)
            {
                stmtComment = null;
                stmtData = null; 
                stmtDim = null;
                stmtEnd = null;
                stmtFileOpen = null;
                stmtFileClose = null; 
                stmtFor = null;
                stmtForNext = null;
                stmtFunctionCall = null;
                stmtGosub = null;
                stmtGoto = null;
                stmtIf = null;
                stmtIfElseIf = null;
                stmtIfElse = null;
                stmtIfEndIf = stmt;
                stmtInput = null;
                stmtLet = null;
                stmtPrint = null;
                stmtRead = null;
                stmtRestore = null;
                stmtReturn = null;
            }

            public void Set(AstInput? stmt)
            {
                stmtComment = null;
                stmtData = null; 
                stmtDim = null;
                stmtEnd = null;
                stmtFileOpen = null;
                stmtFileClose = null; 
                stmtFor = null;
                stmtForNext = null;
                stmtFunctionCall = null;
                stmtGosub = null;
                stmtGoto = null;
                stmtIf = null;
                stmtIfElseIf = null;
                stmtIfElse = null;
                stmtIfEndIf = null;
                stmtInput = stmt;
                stmtLet = null;
                stmtPrint = null;
                stmtRead = null;
                stmtRestore = null;
                stmtReturn = null;
            }

            public void Set(AstLet? stmt)
            {
                stmtComment = null;
                stmtData = null; 
                stmtDim = null;
                stmtEnd = null;
                stmtFileOpen = null;
                stmtFileClose = null; 
                stmtFor = null;
                stmtForNext = null;
                stmtFunctionCall = null;
                stmtGosub = null;
                stmtGoto = null;
                stmtIf = null;
                stmtIfElseIf = null;
                stmtIfElse = null;
                stmtIfEndIf = null;
                stmtInput = null;
                stmtLet = stmt;
                stmtPrint = null;
                stmtRead = null;
                stmtRestore = null;
                stmtReturn = null;
            }

            public void Set(AstPrint? stmt)
            {
                stmtComment = null;
                stmtData = null; 
                stmtDim = null;
                stmtEnd = null;
                stmtFileOpen = null;
                stmtFileClose = null; 
                stmtFor = null;
                stmtForNext = null;
                stmtFunctionCall = null;
                stmtGosub = null;
                stmtGoto = null;
                stmtIf = null;
                stmtIfElseIf = null;
                stmtIfElse = null;
                stmtIfEndIf = null;
                stmtInput = null;
                stmtLet = null;
                stmtPrint = stmt;
                stmtRead = null;
                stmtRestore = null;
                stmtReturn = null;
            }


            public void Set(AstRead? stmt)
            {
                stmtComment = null;
                stmtData = null; 
                stmtDim = null;
                stmtEnd = null;
                stmtFileOpen = null;
                stmtFileClose = null; 
                stmtFor = null;
                stmtForNext = null;
                stmtFunctionCall = null;
                stmtGosub = null;
                stmtGoto = null;
                stmtIf = null;
                stmtIfElseIf = null;
                stmtIfElse = null;
                stmtIfEndIf = null;
                stmtInput = null;
                stmtLet = null;
                stmtPrint = null;
                stmtRead = stmt;
                stmtRestore = null;
                stmtReturn = null;
            }

            public void Set(AstRestore? stmt)
            {
                stmtComment = null;
                stmtData = null;
                stmtDim = null;
                stmtEnd = null;
                stmtFileOpen = null;
                stmtFileClose = null; 
                stmtFor = null;
                stmtForNext = null;
                stmtFunctionCall = null;
                stmtGosub = null;
                stmtGoto = null;
                stmtIf = null;
                stmtIfElseIf = null;
                stmtIfElse = null;
                stmtIfEndIf = null;
                stmtInput = null;
                stmtLet = null;
                stmtPrint = null;
                stmtRead = null;
                stmtRestore = stmt;
                stmtReturn = null;
            }
            public void Set(AstReturn? stmt)
            {
                stmtComment = null;
                stmtData = null;
                stmtDim = null;
                stmtEnd = null;
                stmtFileOpen = null;
                stmtFileClose = null; 
                stmtFor = null;
                stmtForNext = null;
                stmtFunctionCall = null;
                stmtGosub = null;
                stmtGoto = null;
                stmtIf = null;
                stmtIfElseIf = null;
                stmtIfElse = null;
                stmtIfEndIf = null;
                stmtInput = null;
                stmtLet = null;
                stmtPrint = null;
                stmtRead = null;
                stmtRestore = null;
                stmtReturn = stmt;
            }
            public AstNode? Get()
            {
                if (stmtComment != null)
                    return stmtComment;

                if (stmtData != null)
                {
                    return stmtData; 
                }

                if (stmtDim != null)
                    return stmtDim;

                if (stmtEnd != null)
                    return stmtEnd;

                if (stmtFileOpen != null)
                    return stmtFileOpen;

                if (stmtFileClose != null)
                    return stmtFileClose;

                if (stmtFor != null)
                    return stmtFor;

                if (stmtForNext != null)
                    return stmtForNext;

                if (stmtFunctionCall != null)
                    return stmtFunctionCall;

                if (stmtGosub != null)
                    return stmtGosub;

                if (stmtGoto != null)
                    return stmtGoto;

                if (stmtIf != null)
                    return stmtIf;

                if (stmtIfElseIf != null)
                    return stmtIfElseIf;

                if (stmtIfElse != null)
                    return stmtIfElse;

                if (stmtIfEndIf != null)
                    return stmtIfEndIf;

                if (stmtInput != null)
                    return stmtInput;

                if (stmtLet != null)
                    return stmtLet;

                if (stmtPrint != null)
                    return stmtPrint;

                if (stmtRead != null)
                    return stmtRead;

                if (stmtRestore != null)
                    return stmtRestore;

                if (stmtReturn != null)
                    return stmtReturn;

                return null;
            }

            public override string ToString()
            {
                if (stmtComment != null)
                    return $"{stmtComment}";
                else if (stmtData != null)
                    return $"{stmtData}";
                else if (stmtDim != null)
                    return $"{stmtDim}";
                else if (stmtEnd != null)
                    return $"{stmtEnd}";
                else if (stmtFileOpen != null)
                    return $"{stmtFileOpen}";
                else if (stmtFileClose != null)
                    return $"{stmtFileClose}";
                else if (stmtFor != null)
                    return $"{stmtFor}";
                else if (stmtForNext != null)
                    return $"{stmtForNext}";
                else if (stmtFunctionCall != null)
                    return $"{stmtFunctionCall}";
                else if (stmtGosub != null)
                    return $"{stmtGosub}";
                else if (stmtGoto != null)
                    return $"{stmtGoto}";
                else if (stmtIf != null)
                    return $"{stmtIf}";
                else if (stmtIfElseIf != null)
                    return $"{stmtIfElseIf}";
                else if (stmtIfElse != null)
                    return $"{stmtIfElse}";
                else if (stmtIfEndIf != null)
                    return $"{stmtIfEndIf}";
                else if (stmtInput != null)
                    return $"{stmtInput}";
                else if (stmtLet != null)
                    return $"{stmtLet}";
                else if (stmtPrint != null)
                    return $"{stmtPrint}";
                else if (stmtRead != null)
                    return $"{stmtRead}";
                else if (stmtRestore != null)
                    return $"{stmtRestore}";
                else if (stmtReturn != null)
                    return $"{stmtReturn}";

                return "";
            }
        }

        public class AstComment : AstNode
        {
            public Token comment;

            public AstComment(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                comment = t;
            }

            public override string ToString()
            {
                return $"'{comment.Text}";
            }

        }
        public class AstStatements : AstNode
        {
            public List<AstStatement>? statements;

            public AstStatements(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                statements = null;
            }

            public AstNode? Get()
            {
                if (statements != null)
                    return this;
                return null;
            }

            public void Add(AstStatement stmt)
            {
                if (statements == null)
                {
                    statements = new List<AstStatement>();
                }
                statements.Add(stmt);
            }

            public void Set(List<AstStatement>? stmts)
            {
                statements = new List<AstStatement>();
                if (stmts != null)
                    statements.AddRange(stmts);
            }

            public override string ToString()
            {
                if (statements == null)
                    return "";
                return String.Join(" : ", (from stmt in statements select $"{stmt}"));
            }
        }

        public class AstLine : AstNode
        {
            public int? line;
            public string? label;
            public AstStatements? statements;
            public AstLine(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                statements = null;
                this.label = null;
                line = null;
            }

            public void Set(AstStatements stmts)
            {
                line = null;
                this.label = null;
                statements = stmts;
            }

            public void Set(int line, AstStatements stmts)
            {
                this.line = line;
                this.label = null;
                statements = stmts;
            }

            public void Set(string label, AstStatements stmts)
            {
                this.line = null;
                this.label = label;
                statements = stmts;
            }

            public void Set(int? line, string? label, AstStatements? stmts)
            {
                this.line = line;
                this.label = label;
                statements = stmts;
            }

            public override string ToString()
            {
                if (statements != null && line != null && label == null)
                    return $"{line} {statements}";
                else if (statements != null && line == null && label != null)
                    return $"{label}: {statements}";
                else if (statements != null)
                    return $"{statements}";
                return "null";
            }
        }

        public class AstLines : AstNode
        {
            public List<AstLine>? lines;
            public AstLines(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                lines = null;
            }
            public void Add(AstLine line)
            {
                if (lines == null)
                    lines = new List<AstLine>();
                lines.Add(line);
            }

            public void Set(List<AstLine>? body)
            {
                lines = new List<AstLine>();
                if (body != null)
                    lines.AddRange(body);
            }

            public override string ToString()
            {
                if (lines == null)
                    return "null";

                return String.Join("", (from line in lines select $"{line}\n"));
            }

        }

        public class AstConditionAndLines : AstNode
        {
            public AstExpression? exp;
            public AstLines? lines;
            Token savedToken;
            public AstConditionAndLines(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                savedToken = t;
                exp = null;
                lines = null;
            }

            public void Set(AstExpression exp)
            {
                this.exp = exp;
            }

            public void Set(AstExpression exp, AstLines body)
            {
                this.exp = exp;
                this.lines = body;
            }

            public void Add(AstLine line)
            {
                if (lines == null)
                    lines = new AstLines(savedToken);
                lines.Add(line);
            }

            public void Set(List<AstLine>? body)
            {
                lines = new AstLines(savedToken);
                if (body != null)
                    lines.Set(body);
            }

            public override string ToString()
            {
                if (lines != null && lines.lines != null)
                {
                    return String.Join("", (from line in lines.lines select $"{line}\n"));
                }
                else
                    return "";
            }

        }

        public class AstIf : AstNode
        {
            public AstExpression? exp;
            public string label = "";
            public bool multiLine = true;
            Token savedToken;
            public AstIf(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                savedToken = t;
                exp = null;
            }

            public void Set(AstExpression exp)
            {
                this.exp = exp;
            }

            public override string ToString()
            {
                if (exp != null)
                    return $"IF {exp} THEN";
                else
                    return "ERROR: IF expression THEN";
            }

        }

        public class AstIfElseIf : AstNode
        {
            public AstExpression? exp;
            public string label = "";

            public AstIfElseIf(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                exp = null;
            }

            public void Set(AstExpression exp)
            {
                this.exp = exp;
                // Add the lines of the body separately.
            }

            public override string ToString()
            {
                return $"ELSEIF {exp} THEN";
            }

        }

        public class AstIfElse : AstNode
        {
            public string label = "";

            public AstIfElse(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
            }

            public override string ToString()
            {
                return "ELSE";
            }

        }
        
        public class AstIfEndIf : AstNode
        {
            public string label = "";

            public AstIfEndIf(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
            }

            public override string ToString()
            {
                return "END IF";
            }
        }

        public class AstFor : AstNode
        {
            /*
                FOR ID '=' <Expression> TO<Expression>     
                | FOR ID '=' <Expression> TO<Expression> STEP Integer
            */
            public AstVariable? id;
            public AstExpression? beginExp;
            public AstExpression? endExp;
            public int step;
            public string label = "";
            public bool calledFromNext;

            public AstFor(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                id = null;
                beginExp = null;
                endExp = null;
                step = 1;
                calledFromNext = false;
            }

            public void Set(AstVariable id, AstExpression beginExp, AstExpression endExp, int step = 1)
            {
                // Add the lines of the body separately.
                this.step = step;
                this.id = id;
                this.beginExp = beginExp;
                this.endExp = endExp;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                string stepClause = step == 1 ? "": $" STEP {step}";
                return $"FOR {id} = {beginExp} TO {endExp}{stepClause}";
            }

        }
        public class AstForNext : AstNode
        {
            /*
                NEXT ID?                
            */
            public AstVariable? id;
            public string label;
            public string labelFor;
            Token savedToken;
            public AstForNext(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                savedToken = t;
                id = null;
                label = "";
                labelFor = "";
            }

            public void Set(AstVariable? id)
            {
                this.id = id;
            }

            public override string ToString()
            {
                return this.id != null ? $"NEXT {id}" : "NEXT";
            }

        }

        public class AstGoto : AstNode
        {
            /*
                GOTO <LINE NUMBER|LABEL>
            */
            public string label = "";
            public int line = -1;
            public AstGoto(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
            }

            public void Set(string label)
            {
                this.label = label;
                this.line = -1;
            }
            public void Set(int line)
            {
                this.label = "";
                this.line = line;
            }

            public override string ToString()
            {
                string target = "ERROR";
                if (this.label != "")
                    target = label;
                else if (this.line != -1)
                    target = this.line.ToString();
                return $"GOTO {target}";
            }

        }

        public class AstPrintList : AstNode
        {
            public List<AstExpression>? exps;

            public AstPrintList(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                exps = null;
            }

            public void Add(AstExpression e)
            {
                if (exps == null)
                    exps = new List<AstExpression>();
                exps.Add(e);
            }

            public override string ToString()
            {
                if (exps == null || exps.Count == 0)
                    return "";
                return String.Join(';', 
                    (from exp in exps
                    select $"{exp}").ToArray<string>());
            }
        }

        public class AstPrint : AstNode
        {
            public AstPrintList? exps;
            public bool emitCrlf = true;
            Token saved;
            public AstPrint(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                exps = null;
                saved = t;
            }

            public void Set(AstPrintList printList)
            {
                exps = printList;
            }

            public void Add(AstExpression exp)
            {
                if (exps == null)
                    exps = new AstPrintList(saved);
                exps.Add(exp);
            }

            public override string ToString()
            {
                if (exps == null || exps.exps == null || exps.exps.Count == 0)
                    return "PRINT";
                return $"PRINT {exps}";
            }
        }

        public class AstIDList : AstNode
        {
            public List<AstNode>? ids;
            public AstIDList(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                ids = null;
            }

            public void Set(List<AstNode>? ids)
            {
                this.ids = ids;
            }

            public void Add(AstVariable id)
            {
                if (ids == null)
                    ids = new List<AstNode>();
                ids.Add(id);
            }

            public void Add(AstArrayAccess id)
            {
                if (ids == null)
                    ids = new List<AstNode>();
                ids.Add(id);
            }

            public override string ToString()
            {
                if (ids == null || ids.Count == 0)
                    return "";

                return String.Join(", ", (from id in ids
                                          select id.ToString()));
            }
        }

        public class AstInput : AstNode
        {
            public AstIDList? ids;
            public string prompt = "";
            Token saved;
            public AstInput(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                ids = null;
                prompt = "";
                saved = t;
            }

            public void Set(string prompt)
            {
                this.ids = null;
                this.prompt = "";
            }

            public void Set(string prompt, AstIDList? ids)
            {
                this.prompt = prompt;
                this.ids = ids;
            }

            public void Set(AstIDList? ids)
            {
                this.prompt = "";
                this.ids = ids;
            }

            public void Add(AstVariable id)
            {
                if (ids == null)
                    ids = new AstIDList(saved);
                ids.Add(id);                
            }
            public override string ToString()
            {
                string promptText = prompt.Length > 0 ? $" \"{prompt}\"," : "";
                string idList = ids != null && ids.ids != null && ids.ids.Count > 0 ? $" {ids}" : "";
                return $"INPUT{promptText}{idList}";
            }
        }

        public class AstEnd : AstNode
        {
            public AstEnd(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
            }
            public override string ToString()
            {
                return "END";
            }
        }

        public class AstGosub : AstNode
        {
            public string label = "";
            public int line = -1;

            public AstGosub(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                label = "";
                line = -1;
            }

            public void Set(string label)
            {
                this.label = label;
                this.line = -1;
            }

            public void Set(int line)
            {
                this.label = "";
                this.line = line;
            }

            public override string ToString()
            {
                string target;
                if (label.Length > 0)
                    target = label;
                else if (line >= 0)
                    target = line.ToString();
                else
                    target = "";

                return $"GOSUB {target}";
            }
        }

        public class AstReturn : AstNode
        {
            public AstReturn(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
            }

            public override string ToString()
            {
                return "RETURN";
            }     
        }

        public class AstArrayAccess : AstNode
        {
            public string variable;
            public AstExpressionList? exps;
            Token saved;
            public AstArrayAccess(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                saved = t;
                variable = "";
                exps = null;
            }

            public void Set(string variableName, AstExpressionList? exps)
            {
                variable = variableName;
                this.exps = exps;
                if (this.exps != null && this.exps.expList != null && this.exps.expList.Count > 4)
                    throw new Exception($"Too many indexes passed to ${variable}");
            }

            public void Set(string variableName)
            {
                variable = variableName;
            }

            public void Add(AstExpression? exp)
            {
                if (exps == null)
                    exps = new AstExpressionList(saved);
                if (exp != null)
                    this.exps.Add(exp);
                if (this.exps != null && this.exps.expList != null && this.exps.expList.Count > 4)
                    throw new Exception($"Too many indexes passed to ${variable}");
            }

            public override string ToString()
            {
                string expList = "";
                if (exps != null && exps.expList != null)
                    expList = $"[{exps}]";
                return $"{variable}{expList}";
            }
        }

        public class AstDim : AstNode
        {
            public string? name;
            public AstExpressionList? rank;
            Token saved;

            public AstDim(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                saved = t;
                name = null;
                rank = null;
            }

            public void Set(string name)
            {
                this.name = name;
            }

            public void Add(AstExpression exp)
            {
                if (rank == null)
                    rank = new AstExpressionList(saved);
                rank.Add(exp);
                if (rank.expList != null && rank.expList.Count > 4)
                    throw new Exception($"Too many array indexes for {name}.");
            }

            public override string ToString()
            {
                return $"DIM {name}[{rank}]";
            }
        }

        public class AstData : AstNode
        {
            public List<AstConstant>? values;

            public AstData(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                values = null;
            }

            public void Add(AstConstant constant)
            {
                if (values == null)
                    values = new List<AstConstant>();
                values.Add(constant);
            }

            public override string ToString()
            {
                if (values == null)
                    return "DATA";

                string valueString = String.Join(", ", values.Select(value => $"{value}"));
                return $"DATA {valueString}";
            }

        }

        public class AstRestore : AstNode
        {
            public string? lineLabel;
            public int? lineNum;

            public AstRestore(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                lineLabel = null;
                lineNum = null;
            }

            public void set(int line)
            {
                lineLabel = null;
                lineNum = line;
            }

            public void set(string line)
            {
                lineLabel = line;
                lineNum = null;
            }

            public override string ToString()
            {
                if (lineLabel != null)
                    return $"RESTORE {lineLabel}";
                else if (lineNum != null)
                    return $"RESTORE {lineNum}";
                return "RESTORE";
            }
        }

        public class AstRead : AstNode
        {
            public AstIDList? ids;
            Token saved;
            public AstRead(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                ids = null;
                saved = t;
            }

            public void Set(AstIDList? ids)
            {
                this.ids = ids;
            }
            public void Add(AstVariable id)
            {
                if (ids == null)
                    ids = new AstIDList(saved);
                ids.Add(id);
            }

            public void Add(AstArrayAccess id)
            {
                if (ids == null)
                    ids = new AstIDList(saved);
                ids.Add(id);
            }
            public override string ToString()
            {
                string idList = ids != null && ids.ids != null && ids.ids.Count > 0 ? $" {ids}" : "";
                return $"READ{idList}";
            }
        }

        public class AstFileOpen : AstNode
        {
            // OPEN file$ [FOR mode] AS [#]filenumber% 
            public AstValue? fileName;
            public FileMode mode;
            public AstValue? fileNumber;
            public int? fileNum;

            public AstFileOpen(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                mode = FileMode.OUTPUT;
                fileName = null;
                fileNumber = null;
            }

            public void Set(AstValue fileName, string fileMode, AstValue fileNumber)
            {
                this.fileName = fileName;
                this.mode = AstFileOpen.StringToFileMode(fileMode);
                this.fileNumber = fileNumber;
                this.fileNum = null;
            }

            public void Set(AstValue fileName, string fileMode, int fileNum)
            {
                this.fileName = fileName;
                this.mode = AstFileOpen.StringToFileMode(fileMode);
                this.fileNum = fileNum;
                this.fileNumber = null;
            }

            public static FileMode StringToFileMode(string fileMode)
            {
                if (fileMode.ToUpperInvariant().Trim() == "INPUT")
                    return FileMode.INPUT;
                else if (fileMode.ToUpperInvariant().Trim() == "OUTPUT")
                    return FileMode.OUTPUT;
                else if (fileMode.ToUpperInvariant().Trim() == "APPEND")
                    return FileMode.APPEND;
                return FileMode.OUTPUT; // Default
            }

            public static String FileModeToString(FileMode fileMode)
            {
                string modeStr = "ERROR";
                if (fileMode == FileMode.INPUT)
                    modeStr = "INPUT";
                else if (fileMode == FileMode.OUTPUT)
                    modeStr = "OUTPUT";
                else if (fileMode == FileMode.APPEND)
                    modeStr = "APPEND";
                return modeStr;
            }

            public override string ToString()
            {
                const string ERR_UNDEFINED = "ERROR_NOT_SPECIFIED";
                string modeStr = FileModeToString(mode);
                string fileNameStr = fileName != null ? fileName.ToString() : ERR_UNDEFINED;
                string fileNumberStr = fileNumber != null ? fileNumber.ToString() : fileNum != null ? $"#{fileNum}" : ERR_UNDEFINED;

                return $"OPEN {fileNameStr} FOR {modeStr} AS #{fileNumberStr}";
            }
        }

        public class AstFileClose : AstNode
        {
            public AstValue? fileNumber;
            public int? fileNum;
            public AstFileClose(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                fileNumber = null;
            }

            public void Set(AstValue? fileNumber)
            {
                this.fileNumber = fileNumber;
                this.fileNum = null;
            }
            public void Set(int? fileNum)
            {
                this.fileNumber = null;
                this.fileNum = fileNum;
            }

            public override string ToString()
            {
                if (fileNum != null)
                    return $"CLOSE #{fileNum.ToString()}";
                else if (fileNumber != null)
                    return $"CLOSE {fileNumber}";
                else
                    return "CLOSE";
            }
        }
    }
}
