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
            public AstComment? stmtComment;
            public AstFor? stmtFor;
            public AstIf? stmtIf;
            public AstLet? stmtLet;
            public AstStatement(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                stmtComment = null;
                stmtFor = null;
                stmtIf = null;
                stmtLet = null;
            }

            public void Set(AstComment? stmt)
            {
                stmtComment = stmt;
                stmtFor = null;
                stmtIf = null;
                stmtLet = null;
            }

            public void Set(AstFor? stmt)
            {
                stmtComment = null;
                stmtFor = stmt;
                stmtIf = null;
                stmtLet = null;
            }

            public void Set(AstIf? stmt)
            {
                stmtComment = null;
                stmtFor = null;
                stmtIf = stmt;
                stmtLet = null;
            }

            public void Set(AstLet? stmt)
            {
                stmtComment = null;
                stmtFor = null;
                stmtIf = null;
                stmtLet = stmt;
            }

            public AstNode? Get()
            {
                if (stmtComment != null)
                    return stmtComment;

                if (stmtFor != null)
                    return stmtFor;

                if (stmtIf != null)
                    return stmtIf;

                if (stmtLet != null)
                    return stmtLet;
                return null;
            }

            public override string ToString()
            {
                if (stmtComment != null)
                    return $"{stmtComment}";
                else if (stmtFor != null)
                    return $"{stmtFor}";
                else if (stmtIf != null)
                    return $"{stmtIf}";
                else if (stmtLet != null)
                    return $"{stmtLet}";
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
                return $"COMMENT({comment.Text})";
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
                StringBuilder sbStmt = new StringBuilder();
                bool first = true;
                foreach (AstStatement stmt in statements)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sbStmt.Append(" : ");
                    }
                    sbStmt.Append($"{stmt}");
                }
                return sbStmt.ToString();
            }
        }

        public class AstLine : AstNode
        {
            public int? line;
            public AstStatements? statements;
            public AstLine(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                statements = null;
                line = null;
            }

            public void Set(AstStatements stmts)
            {
                line = null;
                statements = stmts;
            }

            public void Set(int line, AstStatements stmts)
            {
                this.line = line;
                statements = stmts;
            }

            public override string ToString()
            {
                if (statements != null && line != null)
                    return $"{line} {statements}";
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

                StringBuilder sb = new();
                sb.AppendLine("LINES(");
                foreach (AstLine line in lines)
                {
                    sb.AppendLine($"{line}");
                }
                sb.AppendLine(")");
                return sb.ToString();
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
                    StringBuilder sb = new();
                    foreach (AstLine line in lines.lines)
                    {
                        sb.AppendLine($"{line}");
                    }
                    return sb.ToString();
                }
                else
                    return "";
            }

        }

        public class AstIf : AstNode
        {
            public AstExpression? exp;
            public AstLines? lines;
            public List<AstConditionAndLines>? elseIfClauses;
            public AstLines? elseLines;
            Token savedToken;
            public AstIf(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                savedToken = t;
                exp = null;
                lines = null;
                elseIfClauses = null;
                elseLines = null;
            }

            public void Set(AstExpression exp)
            {
                this.exp = exp;
                // Add the lines of the body separately.
            }

            public void AddThenLine(AstLine line)
            {
                if (lines == null)
                    lines = new AstLines(savedToken);
                lines.Add(line);
            }

            public void Set(AstExpression exp, AstLines body)
            {
                // If then endif
                this.exp = exp;
                this.lines = body;
            }

            public void AddElseIf(AstConditionAndLines elseifClause)
            {
                if (elseIfClauses == null)
                    elseIfClauses = new List<AstConditionAndLines>();
                elseIfClauses.Add(elseifClause);
            }

            public void AddElse(AstLines? lines)
            {
                elseLines = lines;
            }

            public void AddElseLine(AstLine line)
            {
                if (elseLines == null)
                    elseLines = new AstLines(savedToken);
                elseLines.Add(line);
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"IF {exp} THEN");
                if (lines != null && lines.lines != null)
                {
                    foreach(AstLine line in lines.lines)
                    {
                        sb.AppendLine($"{line}");
                    }
                }
                if (elseIfClauses != null)
                {
                    foreach (AstConditionAndLines cond in elseIfClauses)
                    {
                        sb.AppendLine($"ELSEIF {cond.exp} THEN ");
                        if (cond.lines != null && cond.lines.lines != null)
                        {
                            foreach (AstLine line in cond.lines.lines)
                            {
                                sb.AppendLine($"{line}");
                            }
                        }
                    }
                }
                if (elseLines != null && elseLines.lines != null)
                {
                    sb.AppendLine($"ELSE");
                    foreach (AstLine line in elseLines.lines)
                    {
                        sb.AppendLine($"{line}");
                    }
                }
                sb.AppendLine("END IF");
                return sb.ToString();
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
            public AstLines? lines;
            Token savedToken;
            public AstFor(Token t) : base(t.LineNumber, t.ColumnNumber)
            {
                savedToken = t;
                id = null;
                beginExp = null;
                endExp = null;
                lines = null;
                step = 1;
            }

            public void Set(AstVariable id, AstExpression beginExp, AstExpression endExp, int step = 1)
            {
                // Add the lines of the body separately.
                this.step = step;
                this.id = id;
                this.beginExp = beginExp;
                this.endExp = endExp;
            }

            public void AddLine(AstLine line)
            {
                if (lines == null)
                    lines = new AstLines(savedToken);
                lines.Add(line);
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                string stepClause = step == 1 ? "": $" STEP {step}";
                sb.AppendLine($"FOR {id} = {beginExp} TO {endExp}{stepClause}");
                if (lines != null && lines.lines != null)
                {
                    foreach (AstLine line in lines.lines)
                    {
                        sb.AppendLine($"{line}");
                    }
                }
                sb.AppendLine($"NEXT {id}");
                
                return sb.ToString();
            }

        }
    }
}
