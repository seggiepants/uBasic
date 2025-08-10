using System;
using System.Collections.Generic;
using System.Linq;
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
                return $"VARIABLE: \"{Name}\"";
            }

        }

        public class AstLet : AstNode
        {
            /*
             LET Id '=' <Expression> 
             */
            public AstVariable? variable;
            public AstExpression? expression;
            public AstLet(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                variable = null;
                expression = null;
            }

            public AstNode? Get()
            {
                if (variable != null && expression != null)
                    return this;
                return null;
            }

            public void Set(AstVariable? name, AstExpression? value)
            {
                variable = name;
                expression = value;
            }

            public override string ToString()
            {
                if (variable != null & expression != null)
                    return $"LET_STATMENT({variable} = {expression})";
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
                    return $"CONSTANT(Bool) = {nodeBool.Value}";
                }
                else if (nodeInt != null)
                {
                    return $"CONSTANT(Integer) = {nodeInt.Value}";
                }
                else if (nodeFloat != null)
                {
                    return $"CONSTANT(Real) = {nodeFloat.Value}";
                }
                else if (nodeString != null)
                {
                    return $"CONSTANT(String) = \"{nodeString.Value}\"";
                }
                else
                    return $"Error: Bad Constant - NULL";
            }
        }

        public class AstFunctionCall : AstNode
        {
            public AstVariable? function;
            public AstExpressionList? args;
            public AstFunctionCall(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                function = null;
                args = null;
            }

            public AstNode? Get()
            {
                if (function != null && args != null)
                    return this;
                if (function != null)
                    return function;
                else return null;
            }

            public void Set(AstVariable? name, AstExpressionList exps)
            {
                function = name;
                args = exps;
            }

            public void Set(AstVariable? name)
            {
                function = name;
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
                return $"FUNCTION_CALL({function}({argString}))";
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
            public AstFunctionCall? nodeFunction;

            public AstValue(Token t) : base(t.LineNumber, t.ColumnNumber) { }

            public void Set(AstExpression node)
            {
                nodeConstant = null;
                nodeExpression = node;
                nodeFunction = null;
                nodeVariable = null;
            }
            public void Set(AstVariable node)
            {
                nodeConstant = null;
                nodeExpression = null;
                nodeFunction = null;
                nodeVariable = node;
            }

            public void Set(AstConstant node)
            {
                nodeConstant = node;
                nodeExpression = null;
                nodeFunction = null;
                nodeVariable = null;
            }

            public void Set(AstFunctionCall node)
            {
                nodeConstant = null;
                nodeExpression = null;
                nodeFunction = node;
                nodeVariable = null;
            }

            public AstNode? Get()
            {
                return nodeConstant ?? nodeExpression ?? (AstNode?)nodeFunction ?? nodeVariable ?? null;
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
                    return $"POWER_EXPRESSION({powerExpression} ^ {value})";
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
                    return $"NEGATE_EXPRESSION(-{powerExpression})";
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
                    return $"MULTIPLY_EXPRESSION({negateExpression} {operation} {multiplyExpression})";
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
                    return $"ADD_EXPRESSION({multiplyExpression} {operation} {addExpression})";
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
                    return $"COMPARE_EXPRESSION({lhs} {op.Text} {rhs})";
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
                    return $"NOT_EXPRESSION(NOT {compare})";
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
                    return $"AND_EXPRESSION({lhs} AND {rhs})";
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
                    return $"EXPRESSION({lhs} OR {rhs})";
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
                StringBuilder sbArgs = new StringBuilder();
                bool first = true;
                foreach (AstExpression exp in expList)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sbArgs.Append(", ");
                    }
                    sbArgs.Append($"{exp}");
                }
                return sbArgs.ToString();
            }
        }

        public class AstStatement : AstNode
        {
            public AstLet? stmtLet;
            public AstStatement(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                stmtLet = null;
            }

            public void Set(AstLet? stmt)
            {
                this.stmtLet = stmt;
            }

            public AstNode? Get()
            {
                if (stmtLet != null)
                    return stmtLet;
                return null;
            }

            public override string ToString()
            {
                if (stmtLet != null)
                    return $"{stmtLet}";
                return "null";
            }
        }        
    }
}
