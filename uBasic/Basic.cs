using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;


namespace uBasic
{
    public class Basic()
    {
        const string FOR_PREFIX = "#FOR_";
        const string NEXT_PREFIX = "#NEXT_";

        const string IF_PREFIX = "#IF_";
        const string IF_NEXT_PREFIX = "#IF_NEXT_";
        const string IF_END_IF_PREFIX = "#ENDIF_";

        public static Stack<string> labels = new Stack<string>();

        /*
        AstNode ParseLet(IEnumerable<Token> tokens, int Index)
        {
            // (LET)? IDENTIFIER EQUALS EXPRESSION
            AstNode ret = null;

        }
        */

        public static Tuple<int, Parser.AstToken?> ParseToken(List<Token> tokens, int Index, Runtime runtime)
        {
            Token t = tokens[Index];
            Parser.AstToken val = new(t);
            return new Tuple<int, Parser.AstToken?>(Index + 1, val);
        }
        public static Tuple<int, Parser.AstValue?> ParseValue(List<Token> tokens, int Index, Runtime runtime)
        {
            /*
             <Value>       ::= '(' <Expression> ')'
                | ID 
                | ID '(' <Expression List> ')'
                | <Constant> 
            */
            Token t = tokens[Index];
            Parser.AstValue val = new Parser.AstValue(t);

            if (t.Type == Token_Type.TOKEN_LPAREN)
            {
                Tuple<int, Parser.AstExpression?> expression = ParseExpression(tokens, Index + 1, runtime);
                if (expression.Item2 != null)
                {
                    if (tokens[expression.Item1].Type == Token_Type.TOKEN_RPAREN)
                    {
                        val.Set(expression.Item2);
                        return new Tuple<int, Parser.AstValue?>(expression.Item1 + 1, val);
                    }
                    else
                    {
                        throw new Exception($"Incomplete value mismatched Parenthesis Line: {tokens[expression.Item1].LineNumber} Column: {tokens[expression.Item1].ColumnNumber}");
                    }
                }
            }
            else if (t.Type == Token_Type.TOKEN_IDENTIFIER)
            {
                // Is this a variable or a function call?
                Parser.AstVariable id = new Parser.AstVariable(t);
                Parser.AstExpressionList? args = null;
                if (tokens.Count > Index + 1 && (tokens[Index + 1].Type == Token_Type.TOKEN_LPAREN))
                {
                    // Function call
                    args = new(tokens[Index + 1]);
                    int i = Index + 2;
                    bool readArgs = true;
                    while (readArgs)
                    {
                        Tuple<int, Parser.AstExpression?> expression = ParseExpression(tokens, i, runtime);
                        if (expression.Item2 != null)
                        {
                            i = expression.Item1;
                            args.Add(expression.Item2);
                        }

                        if (tokens.Count > i && (tokens[i].Type == Token_Type.TOKEN_RPAREN))
                        {
                            Index = i + 1;
                            readArgs = false;
                        }
                        else if (tokens.Count > i && (tokens[i].Type != Token_Type.TOKEN_COMMA))
                        {
                            // Bad expression list.
                            throw new Exception($"Expected comma or ')' on Line: {tokens[i].LineNumber}, Column: {tokens[i].ColumnNumber}.");
                        }
                        else // Token Type = Comma
                            i++;
                    }
                    Parser.AstFunctionCall functionCall = new Parser.AstFunctionCall(t);
                    functionCall.Set(id, args);
                    val.Set(functionCall);
                    return new Tuple<int, Parser.AstValue?>(Index, val);
                }
                else
                {
                    // Variable
                    val.Set(id);
                    return new Tuple<int, Parser.AstValue?>(Index + 1, val);
                }
            }
            Tuple<int, Parser.AstConstant?> constant = ParseConstant(tokens, Index, runtime);
            if (constant.Item2 != null)
            {
                val.Set(constant.Item2);
                return new Tuple<int, Parser.AstValue?>(constant.Item1, val);
            }
            return new Tuple<int, Parser.AstValue?>(Index, null);
        }

        public static Tuple<int, Parser.AstComment?> ParseComent(List<Token> tokens, int Index, Runtime runtime)
        {

            if (tokens[Index].Type == Token_Type.TOKEN_COMMENT)
            {
                return new Tuple<int, Parser.AstComment?>(Index + 1, new Parser.AstComment(tokens[Index]));
            }
            return new Tuple<int, Parser.AstComment?>(Index, null);
        }

        public static Tuple<int, Parser.AstConstant?> ParseConstant(List<Token> tokens, int Index, Runtime runtime)
        {

            switch (tokens[Index].Type)
            {
                case Token_Type.TOKEN_TRUE:
                case Token_Type.TOKEN_FALSE:
                case Token_Type.TOKEN_INTEGER:
                case Token_Type.TOKEN_FLOAT:
                case Token_Type.TOKEN_STRING:
                    return new Tuple<int, Parser.AstConstant?>(Index + 1, new Parser.AstConstant(tokens[Index]));
            }
            return new Tuple<int, Parser.AstConstant?>(Index, null);
        }

        public static Tuple<int, Parser.AstPowerExpression?> ParsePowerExpression(List<Token> tokens, int Index, Runtime runtime)
        {
            /*
             <Power Exp> '^' <Value> 
                | <Value> 
             */
            Tuple<int, Parser.AstValue?> lhs;
            Tuple<int, Parser.AstValue?> rhs;
            Parser.AstPowerExpression? power = null;

            int i = Index;
            lhs = ParseValue(tokens, i, runtime);
            if (lhs.Item2 != null)
            {
                i = lhs.Item1;
                power = new Parser.AstPowerExpression(tokens[Index]);
                power.Set(lhs.Item2);
            }

            while (power != null)
            {
                if (tokens.Count > i && tokens[i].Type == Token_Type.TOKEN_POWER)
                {
                    rhs = ParseValue(tokens, i + 1, runtime);
                    if (rhs.Item2 != null)
                    {
                        // <Power Exp> '^' <Value>
                        Parser.AstPowerExpression next = new(tokens[i + 1]);
                        next.Set(power, rhs.Item2);
                        power = next;
                        i = rhs.Item1;
                    }
                    else
                    {
                        throw new Exception($"Incomplete Power Expression. Line: {tokens[i].LineNumber}, Column: {tokens[i].ColumnNumber}");
                    }
                }
                else
                {
                    return new Tuple<int, Parser.AstPowerExpression?>(i, power);
                }
            }
            return new Tuple<int, Parser.AstPowerExpression?>(Index, null);
        }

        public static Tuple<int, Parser.AstNegateExpression?> ParseNegateExpression(List<Token> tokens, int Index, Runtime runtime)
        {
            /*
             <Negate Exp>  ::= '-' <Power Exp> 
                | <Power Exp>  
             */
            Tuple<int, Parser.AstPowerExpression?> ret;
            bool negate = false;
            int i = Index;
            if (tokens.Count > i && tokens[i].Type == Token_Type.TOKEN_SUBTRACT)
            {
                negate = true;
                i++;
            }
            ret = ParsePowerExpression(tokens, i, runtime);
            if (ret.Item2 != null)
            {
                Parser.AstNegateExpression negateExp = new(tokens[i]);
                negateExp.Set(ret.Item2, negate);
                return new Tuple<int, Parser.AstNegateExpression?>(ret.Item1, negateExp);
            }

            return new Tuple<int, Parser.AstNegateExpression?>(Index, null);
        }

        public static Tuple<int, Parser.AstMultiplyExpression?> ParseMultiplyExpression(List<Token> tokens, int Index, Runtime runtime)
        {
            /*
             <Negate Exp> '*' <Mult Exp> 
                | <Negate Exp> '/' <Mult Exp> 
                | <Negate Exp>   
             */
            int i = Index;
            Stack<Parser.AstNegateExpression>? exps = null;
            Stack<Token>? ops = null;
            Tuple<int, Parser.AstNegateExpression?> lhs = ParseNegateExpression(tokens, i, runtime);
            if (lhs.Item2 != null)
            {
                exps = new();
                ops = new();

                i = lhs.Item1;
                exps.Push(lhs.Item2);
            }
            while (lhs.Item2 != null && ops != null && exps != null)
            {
                if (tokens.Count > i && tokens[i].Type == Token_Type.TOKEN_MULTIPLY || tokens.Count > i && tokens[i].Type == Token_Type.TOKEN_DIVIDE)
                {
                    Token op = tokens[i];
                    lhs = ParseNegateExpression(tokens, i + 1, runtime);
                    if (lhs.Item2 != null)
                    {
                        // <Negate Exp> '*' <Mult Exp> 
                        // | <Negate Exp> '/' <Mult Exp>
                        ops.Push(op);
                        exps.Push(lhs.Item2);
                        i = lhs.Item1;
                    }
                    else
                    {
                        throw new Exception($"Incomplete Multiply/Divide Expression. Operator = {op.Text}, Line: {tokens[i].LineNumber}, Column: {tokens[i].ColumnNumber}");
                    }
                }
                else
                {
                    break;
                }
            }

            // Combine.
            Parser.AstMultiplyExpression? multiply = null;
            if (exps != null && ops != null)
            {
                while (exps.Count > 0)
                {
                    if (multiply == null)
                    {
                        multiply = new Parser.AstMultiplyExpression(tokens[Index]);
                        multiply.Set(exps.Pop());
                    }
                    else
                    {
                        Parser.AstMultiplyExpression nextMultiply = new(tokens[Index]);
                        nextMultiply.Set(exps.Pop(), ops.Pop(), multiply);
                        multiply = nextMultiply;
                    }
                }
            }
            return new Tuple<int, Parser.AstMultiplyExpression?>(i, multiply);
        }


        public static Tuple<int, Parser.AstAddExpression?> ParseAddExpression(List<Token> tokens, int Index, Runtime runtime)
        {
            /*
             <Add Exp>  ::= <Mult Exp> '+' <Add Exp> 
                | <Mult Exp> '-' <Add Exp> 
                | <Mult Exp> 
             */
            int i = Index;
            Stack<Parser.AstMultiplyExpression>? exps = null;
            Stack<Token>? ops = null;
            Tuple<int, Parser.AstMultiplyExpression?> lhs = ParseMultiplyExpression(tokens, i, runtime);
            if (lhs.Item2 != null)
            {
                exps = new();
                ops = new();

                i = lhs.Item1;
                exps.Push(lhs.Item2);
            }
            while (lhs.Item2 != null && ops != null && exps != null)
            {
                if (tokens.Count > i && tokens[i].Type == Token_Type.TOKEN_ADD || tokens.Count > i && tokens[i].Type == Token_Type.TOKEN_SUBTRACT)
                {
                    Token op = tokens[i];
                    lhs = ParseMultiplyExpression(tokens, i + 1, runtime);
                    if (lhs.Item2 != null)
                    {
                        // <Mult Exp> '+' <Add Exp> 
                        // | <Mult Exp> '-' <Add Exp>
                        ops.Push(op);
                        exps.Push(lhs.Item2);
                        i = lhs.Item1;
                    }
                    else
                    {
                        throw new Exception($"Incomplete Add/Subtract Expression. Operator = {op.Text}, Line: {tokens[i].LineNumber}, Column: {tokens[i].ColumnNumber}");
                    }
                }
                else
                {
                    break;
                }
            }

            // Combine.
            Parser.AstAddExpression? add = null;
            if (exps != null && ops != null)
            {
                while (exps.Count > 0)
                {
                    if (add == null)
                    {
                        add = new Parser.AstAddExpression(tokens[Index]);
                        add.Set(exps.Pop());
                    }
                    else
                    {
                        Parser.AstAddExpression nextAdd = new(tokens[Index]);
                        nextAdd.Set(exps.Pop(), ops.Pop(), add);
                        add = nextAdd;
                    }
                }
            }
            return new Tuple<int, Parser.AstAddExpression?>(i, add);
        }

        public static Tuple<int, Parser.AstCompareExpression?> ParseCompareExpression(List<Token> tokens, int Index, Runtime runtime)
        {
            /*
            <Compare Exp> ::= <Add Exp> '='  <Compare Exp> 
                | <Add Exp> '<>' <Compare Exp> 
                | <Add Exp> '!=' <Compare Exp> 
                | <Add Exp> '>'  <Compare Exp> 
                | <Add Exp> '>=' <Compare Exp> 
                | <Add Exp> '<'  <Compare Exp> 
                | <Add Exp> '<=' <Compare Exp> 
                | <Add Exp> 
             */
            int i = Index;
            Stack<Parser.AstAddExpression>? exps = null;
            Stack<Token>? ops = null;
            Tuple<int, Parser.AstAddExpression?> lhs = ParseAddExpression(tokens, i, runtime);
            if (lhs.Item2 != null)
            {
                exps = new();
                ops = new();

                i = lhs.Item1;
                exps.Push(lhs.Item2);
            }
            List<Token_Type> compareOps = new List<Token_Type> {
                Token_Type.TOKEN_IS_EQUAL,
                Token_Type.TOKEN_NOT_EQUAL,
                Token_Type.TOKEN_LESS_THAN,
                Token_Type.TOKEN_LESS_EQUAL,
                Token_Type.TOKEN_GREATER_THAN,
                Token_Type.TOKEN_GREATER_EQUAL,
            };

            while (lhs.Item2 != null && ops != null && exps != null)
            {
                if (tokens.Count > i && compareOps.Contains(tokens[i].Type))
                {
                    Token op = tokens[i];
                    lhs = ParseAddExpression(tokens, i + 1, runtime);
                    if (lhs.Item2 != null)
                    {
                        /*
                         <Add Exp> '='  <Compare Exp> 
                        | <Add Exp> '<>' <Compare Exp> 
                        | <Add Exp> '!=' <Compare Exp> 
                        | <Add Exp> '>'  <Compare Exp> 
                        | <Add Exp> '>=' <Compare Exp> 
                        | <Add Exp> '<'  <Compare Exp> 
                        | <Add Exp> '<=' <Compare Exp> 
                         */
                        ops.Push(op);
                        exps.Push(lhs.Item2);
                        i = lhs.Item1;
                    }
                    else
                    {
                        throw new Exception($"Incomplete Comparison Expression. Operator = {op.Text}, Line: {tokens[i].LineNumber}, Column: {tokens[i].ColumnNumber}");
                    }
                }
                else
                {
                    break;
                }
            }

            // Combine.
            Parser.AstCompareExpression? compare = null;
            if (exps != null && ops != null)
            {
                while (exps.Count > 0)
                {
                    if (compare == null)
                    {
                        compare = new Parser.AstCompareExpression(tokens[Index]);
                        compare.Set(exps.Pop());
                    }
                    else
                    {
                        Parser.AstCompareExpression nextCompare = new(tokens[Index]);
                        nextCompare.Set(exps.Pop(), ops.Pop(), compare);
                        compare = nextCompare;
                    }
                }
            }
            return new Tuple<int, Parser.AstCompareExpression?>(i, compare);
        }

        public static Tuple<int, Parser.AstNotExpression?> ParseNotExpression(List<Token> tokens, int Index, Runtime runtime)
        {
            /*
             <Not Exp>     ::= NOT <Compare Exp> 
                            | <Compare Exp>
             */
            Tuple<int, Parser.AstCompareExpression?> ret;
            bool negate = false;
            int i = Index;
            if (tokens.Count > i && tokens[i].Type == Token_Type.TOKEN_NOT)
            {
                negate = true;
                i++;
            }
            ret = ParseCompareExpression(tokens, i, runtime);
            if (ret.Item2 != null)
            {
                Parser.AstNotExpression notExp = new(tokens[i]);
                notExp.Set(negate, ret.Item2);
                return new Tuple<int, Parser.AstNotExpression?>(ret.Item1, notExp);
            }

            return new Tuple<int, Parser.AstNotExpression?>(Index, null);
        }

        public static Tuple<int, Parser.AstAndExpression?> ParseAndExpression(List<Token> tokens, int Index, Runtime runtime)
        {
            /*
             <And Exp>     ::= <Not Exp> AND <And Exp> 
                              | <Not Exp>
             */
            int i = Index;
            Stack<Parser.AstNotExpression>? exps = null;
            Tuple<int, Parser.AstNotExpression?> lhs = ParseNotExpression(tokens, i, runtime);
            if (lhs.Item2 != null)
            {
                exps = new();

                i = lhs.Item1;
                exps.Push(lhs.Item2);
            }
            while (lhs.Item2 != null && exps != null)
            {
                if (tokens.Count > i && tokens[i].Type == Token_Type.TOKEN_AND)
                {
                    Token op = tokens[i];
                    lhs = ParseNotExpression(tokens, i + 1, runtime);
                    if (lhs.Item2 != null)
                    {
                        // <Not Exp> AND <And Exp> 
                        exps.Push(lhs.Item2);
                        i = lhs.Item1;
                    }
                    else
                    {
                        throw new Exception($"Incomplete And Expression. Operator = {op.Text}, Line: {tokens[i].LineNumber}, Column: {tokens[i].ColumnNumber}");
                    }
                }
                else
                {
                    break;
                }
            }

            // Combine.
            Parser.AstAndExpression? combined = null;
            if (exps != null)
            {
                while (exps.Count > 0)
                {
                    if (combined == null)
                    {
                        combined = new Parser.AstAndExpression(tokens[Index]);
                        combined.Set(exps.Pop());
                    }
                    else
                    {
                        Parser.AstAndExpression nextAnd = new(tokens[Index]);
                        nextAnd.Set(exps.Pop(), combined);
                        combined = nextAnd;
                    }
                }
            }
            return new Tuple<int, Parser.AstAndExpression?>(i, combined);
        }

        public static Tuple<int, Parser.AstExpression?> ParseExpression(List<Token> tokens, int Index, Runtime runtime)
        {
            /*
             <Expression>  ::= <And Exp> OR <Expression> 
                            | <And Exp>
             */
            int i = Index;
            Stack<Parser.AstAndExpression>? exps = null;
            Tuple<int, Parser.AstAndExpression?> lhs = ParseAndExpression(tokens, i, runtime);
            if (lhs.Item2 != null)
            {
                exps = new();

                i = lhs.Item1;
                exps.Push(lhs.Item2);
            }
            while (lhs.Item2 != null && exps != null)
            {
                if (tokens.Count > i && tokens[i].Type == Token_Type.TOKEN_OR)
                {
                    Token op = tokens[i];
                    lhs = ParseAndExpression(tokens, i + 1, runtime);
                    if (lhs.Item2 != null)
                    {
                        // <Not Exp> AND <And Exp> 
                        exps.Push(lhs.Item2);
                        i = lhs.Item1;
                    }
                    else
                    {
                        throw new Exception($"Incomplete OR Expression. Operator = {op.Text}, Line: {tokens[i].LineNumber}, Column: {tokens[i].ColumnNumber}");
                    }
                }
                else
                {
                    break;
                }
            }

            // Combine.
            Parser.AstExpression? combined = null;
            if (exps != null)
            {
                while (exps.Count > 0)
                {
                    if (combined == null)
                    {
                        combined = new Parser.AstExpression(tokens[Index]);
                        combined.Set(exps.Pop());
                    }
                    else
                    {
                        Parser.AstExpression nextOr = new(tokens[Index]);
                        nextOr.Set(exps.Pop(), combined);
                        combined = nextOr;
                    }
                }
            }
            return new Tuple<int, Parser.AstExpression?>(i, combined);
        }

        public static Tuple<int, Parser.AstStatement?> ParseStatement(List<Token> tokens, int Index, Runtime runtime)
        {
            Parser.AstStatement statement;

            Tuple<int, Parser.AstComment?> comment = ParseComent(tokens, Index, runtime);
            if (comment.Item2 != null)
            {
                statement = new Parser.AstStatement(tokens[Index]);
                statement.Set(comment.Item2);
                runtime.program.Add(statement);
                return new Tuple<int, Parser.AstStatement?>(comment.Item1, statement);
            }

            Tuple<int, Parser.AstEnd?> endStatement = ParseEnd(tokens, Index, runtime);
            if (endStatement.Item2 != null)
            {
                statement = new Parser.AstStatement(tokens[Index]);
                statement.Set(endStatement.Item2);
                runtime.program.Add(statement);
                return new Tuple<int, Parser.AstStatement?>(endStatement.Item1, statement);
            }

            Tuple<int, Parser.AstFor?> forStatement = ParseForStatment(tokens, Index, runtime);
            if (forStatement.Item2 != null)
            {
                statement = new Parser.AstStatement(tokens[Index]);
                statement.Set(forStatement.Item2);
                int lineNum = runtime.program.Count;
                runtime.lineLabels.Add(forStatement.Item2.label, lineNum);
                runtime.program.Add(statement);
                return new Tuple<int, Parser.AstStatement?>(forStatement.Item1, statement);
            }

            Tuple<int, Parser.AstForNext?> forNextStatement = ParseForNextStatement(tokens, Index, runtime);
            if (forNextStatement.Item2 != null)
            {
                statement = new Parser.AstStatement(tokens[Index]);
                statement.Set(forNextStatement.Item2);
                int lineNum = runtime.program.Count;
                runtime.lineLabels.Add(forNextStatement.Item2.label, lineNum);
                runtime.program.Add(statement);
                return new Tuple<int, Parser.AstStatement?>(forNextStatement.Item1, statement);
            }

            Tuple<int, Parser.AstGoto?> gotoStatement = ParseGotoStatement(tokens, Index, runtime);
            if (gotoStatement.Item2 != null)
            {
                statement = new Parser.AstStatement(tokens[Index]);
                statement.Set(gotoStatement.Item2);
                runtime.program.Add(statement);
                return new Tuple<int, Parser.AstStatement?>(gotoStatement.Item1, statement);
            }

            Tuple<int, Parser.AstIf?> ifStatement = ParseIfStatment(tokens, Index, runtime);
            if (ifStatement.Item2 != null)
            {
                statement = new Parser.AstStatement(tokens[Index]);
                statement.Set(ifStatement.Item2);
                int lineNum = runtime.program.Count;
                runtime.lineLabels.Add(ifStatement.Item2.label, lineNum);
                runtime.program.Add(statement);
                return new Tuple<int, Parser.AstStatement?>(ifStatement.Item1, statement);
            }

            Tuple<int, Parser.AstIfElseIf?> ifElseIfStatement = ParseIfElseIfStatment(tokens, Index, runtime);
            if (ifElseIfStatement.Item2 != null)
            {
                statement = new Parser.AstStatement(tokens[Index]);
                statement.Set(ifElseIfStatement.Item2);
                int lineNum = runtime.program.Count;
                runtime.lineLabels.Add(ifElseIfStatement.Item2.label, lineNum);
                runtime.program.Add(statement);
                return new Tuple<int, Parser.AstStatement?>(ifElseIfStatement.Item1, statement);
            }

            Tuple<int, Parser.AstIfElse?> ifElseStatement = ParseIfElseStatment(tokens, Index, runtime);
            if (ifElseStatement.Item2 != null)
            {
                statement = new Parser.AstStatement(tokens[Index]);
                statement.Set(ifElseStatement.Item2);
                int lineNum = runtime.program.Count;
                runtime.lineLabels.Add(ifElseStatement.Item2.label, lineNum);
                runtime.program.Add(statement);
                return new Tuple<int, Parser.AstStatement?>(ifElseStatement.Item1, statement);
            }

            Tuple<int, Parser.AstIfEndIf?> ifEndIfStatement = ParseIfEndIfStatment(tokens, Index, runtime);
            if (ifEndIfStatement.Item2 != null)
            {
                statement = new Parser.AstStatement(tokens[Index]);
                statement.Set(ifEndIfStatement.Item2);
                int lineNum = runtime.program.Count;
                // May have added for single line mode if so remove it.
                if (runtime.lineLabels.ContainsKey(ifEndIfStatement.Item2.label))
                    runtime.lineLabels.Remove(ifEndIfStatement.Item2.label);
                runtime.lineLabels.Add(ifEndIfStatement.Item2.label, lineNum);
                runtime.program.Add(statement);
                return new Tuple<int, Parser.AstStatement?>(ifEndIfStatement.Item1, statement);
            }

            Tuple<int, Parser.AstInput?> inputStatement = ParseInput(tokens, Index, runtime);
            if (inputStatement.Item2 != null)
            {
                statement = new Parser.AstStatement(tokens[Index]);
                statement.Set(inputStatement.Item2);
                runtime.program.Add(statement);
                return new Tuple<int, Parser.AstStatement?>(inputStatement.Item1, statement);

            }

            Tuple<int, Parser.AstLet?> let = ParseLetStatement(tokens, Index, runtime);
            if (let.Item2 != null)
            {
                statement = new Parser.AstStatement(tokens[Index]);
                statement.Set(let.Item2);
                runtime.program.Add(statement);
                return new Tuple<int, Parser.AstStatement?>(let.Item1, statement);
            }

            Tuple<int, Parser.AstPrint?> printStmt = ParsePrint(tokens, Index, runtime);
            if (printStmt.Item2 != null)
            {
                statement = new Parser.AstStatement(tokens[Index]);
                statement.Set(printStmt.Item2);
                runtime.program.Add(statement);
                return new Tuple<int, Parser.AstStatement?>(printStmt.Item1, statement);
            }

            // ZZZ -- Add more, a lot more.
            return new Tuple<int, Parser.AstStatement?>(Index, null);
        }

        public static Tuple<int, Parser.AstFor?> ParseForStatment(List<Token> tokens, int Index, Runtime runtime)
        {
            // FOR ID '=' <Expression> TO <Expression>     
            // | FOR ID '=' < Expression > TO < Expression > STEP Integer

            Parser.AstFor ret = new(tokens[Index]);
            Tuple<int, Parser.AstFor?> failure = new Tuple<int, Parser.AstFor?>(Index, null);
            int i = Index;

            if (tokens.Count < i || tokens[i].Type != Token_Type.TOKEN_FOR)
            {
                return failure;
            }
            i++; // Consume FOR

            if (tokens.Count < i || tokens[i].Type != Token_Type.TOKEN_IDENTIFIER)
            {
                return failure;
            }
            ret.id = new Parser.AstVariable(tokens[i]);
            i++; // Consume ID

            if (tokens.Count < i || tokens[i].Type != Token_Type.TOKEN_SET_EQUAL)
            {
                return failure;
            }
            i++; // Consume =

            Tuple<int, Parser.AstExpression?> resultExpBegin = ParseExpression(tokens, i, runtime);
            if (resultExpBegin.Item2 == null)
            {
                return failure;
            }
            ret.beginExp = resultExpBegin.Item2;
            i = resultExpBegin.Item1;

            if (tokens.Count < i || tokens[i].Type != Token_Type.TOKEN_TO)
            {
                return failure;
            }
            // Consume TO
            i++;

            Tuple<int, Parser.AstExpression?> resultExpEnd = ParseExpression(tokens, i, runtime);
            if (resultExpEnd.Item2 == null)
            {
                return failure;
            }
            ret.endExp = resultExpEnd.Item2;
            i = resultExpEnd.Item1;

            // Optional STEP
            if (tokens.Count > i && tokens[i].Type == Token_Type.TOKEN_STEP)
            {
                i++; // Consume STEP

                if (tokens.Count < i || tokens[i].Type != Token_Type.TOKEN_INTEGER)
                {
                    return failure;
                }
                Parser.AstInteger step = new Parser.AstInteger(tokens[i]);
                ret.step = step.Value;
                i++; // Consume STEP value
            }

            int idFor = runtime.NextCounter();
            string labelFor = $"{FOR_PREFIX}_{idFor}";
            ret.label = labelFor;
            runtime.forStack.Push(idFor);

            return new Tuple<int, Parser.AstFor?>(i, ret);
        }

        public static Tuple<int, Parser.AstForNext?> ParseForNextStatement(List<Token> tokens, int Index, Runtime runtime)
        {
            // NEXT <identifier>?
            int i = Index;
            Parser.AstForNext ret = new(tokens[i]);
            Tuple<int, Parser.AstForNext?> failure = new Tuple<int, Parser.AstForNext?>(i, null);

            if (tokens.Count < i || tokens[i].Type != Token_Type.TOKEN_NEXT)
                return failure;

            i++; // Consume next.

            int idFor = runtime.forStack.Count > 0 ? runtime.forStack.Pop() : 0;
            string labelFor = $"{FOR_PREFIX}_{idFor}";
            string labelNext = $"{NEXT_PREFIX}_{idFor}";
            ret.labelFor = labelFor;
            ret.label = labelNext;

            if (tokens.Count > i && tokens[i].Type == Token_Type.TOKEN_IDENTIFIER)
            {
                Parser.AstVariable nextId = new Parser.AstVariable(tokens[i]);
                i++; // Consume variable.

                Parser.AstFor? stmtFor = runtime.program[runtime.lineLabels[labelFor]].stmtFor;

                // Make sure it matches
                if (ret.id != null && stmtFor != null && stmtFor.id != null && stmtFor.id.Name != ret.id.Name)
                    return failure;
            }

            return new Tuple<int, Parser.AstForNext?>(i, ret);
        }


        public static Tuple<int, Parser.AstIf?> ParseIfStatment(List<Token> tokens, int Index, Runtime runtime)
        {
            // IF <Expression> THEN <Statement> 
            int i = Index;
            Parser.AstIf ret = new(tokens[Index]);

            Tuple<int, Parser.AstIf?> failure = new Tuple<int, Parser.AstIf?>(Index, null);
            if (tokens.Count < i || tokens[i].Type != Token_Type.TOKEN_IF)
            {
                return failure;
            }
            // Consume IF
            i++;

            Tuple<int, Parser.AstExpression?> resultCond = ParseExpression(tokens, i, runtime);
            if (resultCond.Item2 == null)
            {
                return failure;
            }
            ret.exp = resultCond.Item2;
            i = resultCond.Item1;

            if (tokens.Count < i || tokens[i].Type != Token_Type.TOKEN_THEN)
            {
                return failure;
            }
            // Consume then
            i++;

            int idIf = runtime.NextCounter();
            string labelIf = $"{IF_PREFIX}_{idIf}";
            ret.label = labelIf;
            runtime.ifStack.Push(idIf);

            ret.multiLine = true;
            for (int j = i; j < tokens.Count; j++)
            {
                Token t = tokens[j];
                if (t.Type != Token_Type.TOKEN_NONE && t.Type != Token_Type.TOKEN_NEWLINE && t.Type != Token_Type.TOKEN_WHITE_SPACE && t.Type != Token_Type.TOKEN_COMMENT)
                {
                    ret.multiLine = false;
                    break;
                }
            }

            if (!ret.multiLine)
                labels.Push($"{IF_END_IF_PREFIX}_{idIf}");

            return new Tuple<int, Parser.AstIf?>(i, ret);
        }

        private static void GotoEndif(Token t, int idIf, Runtime runtime)
        {
            string gotoEndIfLabel = $"{IF_END_IF_PREFIX}_{idIf}";

            if (gotoEndIfLabel != "")
            {
                Parser.AstGoto expGoto = new(t);
                expGoto.Set(gotoEndIfLabel);
                Parser.AstStatement stmtGoto = new(t);
                stmtGoto.Set(expGoto);
                runtime.program.Add(stmtGoto);
            }
        }

        private static string IfNextLabel(Runtime runtime)
        {
            int elseCounter = 0;
            bool foundNext = false;
            string elseIfLabel = "";
            int idIf = runtime.ifStack.Peek();
            while (!foundNext)
            {
                elseCounter++;
                elseIfLabel = $"{IF_NEXT_PREFIX}_{idIf}_{elseCounter}";
                if (!runtime.lineLabels.ContainsKey(elseIfLabel))
                    foundNext = true;
            }
            return elseIfLabel;
        }

        public static Tuple<int, Parser.AstIfElseIf?> ParseIfElseIfStatment(List<Token> tokens, int Index, Runtime runtime)
        {
            // ELSEIF <Expression> THEN 
            int i = Index;
            Parser.AstIfElseIf ret = new(tokens[Index]);

            Tuple<int, Parser.AstIfElseIf?> failure = new Tuple<int, Parser.AstIfElseIf?>(Index, null);
            if (tokens.Count < i || tokens[i].Type != Token_Type.TOKEN_ELSEIF)
            {
                return failure;
            }
            // Consume ELSEIF
            i++;

            Tuple<int, Parser.AstExpression?> resultCond = ParseExpression(tokens, i, runtime);
            if (resultCond.Item2 == null)
            {
                return failure;
            }
            ret.exp = resultCond.Item2;
            i = resultCond.Item1;

            if (tokens.Count < i || tokens[i].Type != Token_Type.TOKEN_THEN)
            {
                return failure;
            }
            // Consume then
            i++;

            if (runtime.ifStack.Count == 0)
                return failure;

            string ifNextLabel = IfNextLabel(runtime);

            ret.label = ifNextLabel;

            GotoEndif(tokens[Index], runtime.ifStack.Peek(), runtime);

            return new Tuple<int, Parser.AstIfElseIf?>(i, ret);
        }

        public static Tuple<int, Parser.AstIfElse?> ParseIfElseStatment(List<Token> tokens, int Index, Runtime runtime)
        {
            // ELSE
            int i = Index;
            Parser.AstIfElse ret = new(tokens[Index]);

            Tuple<int, Parser.AstIfElse?> failure = new Tuple<int, Parser.AstIfElse?>(Index, null);
            if (tokens.Count < i || tokens[i].Type != Token_Type.TOKEN_ELSE)
            {
                return failure;
            }
            // Consume ELSE
            i++;

            if (runtime.ifStack.Count == 0)
                return failure;

            string ifNextLabel = IfNextLabel(runtime);

            ret.label = ifNextLabel;

            GotoEndif(tokens[Index], runtime.ifStack.Peek(), runtime);

            return new Tuple<int, Parser.AstIfElse?>(i, ret);
        }

        public static Tuple<int, Parser.AstIfEndIf?> ParseIfEndIfStatment(List<Token> tokens, int Index, Runtime runtime)
        {
            // END IF
            int i = Index;
            Parser.AstIfEndIf? ret = new Parser.AstIfEndIf(tokens[i]);
            Tuple<int, Parser.AstIfEndIf?> failure = new Tuple<int, Parser.AstIfEndIf?>(Index, null);

            if (tokens.Count <= i || (tokens.Count > i && tokens[i].Type != Token_Type.TOKEN_END))
            {
                return failure;
            }
            else
            {
                // Consume end
                i++;
            }

            if (tokens.Count <= i || (tokens.Count > i && tokens[i].Type != Token_Type.TOKEN_IF))
            {
                return failure;
            }
            else
            {
                // Consume if
                i++;
            }

            if (runtime.ifStack.Count == 0)
                return failure;
            string nextIfLabel = IfNextLabel(runtime);
            runtime.lineLabels.Add(nextIfLabel, runtime.program.Count);
            int idIf = runtime.ifStack.Pop();
            ret.label = $"{IF_END_IF_PREFIX}_{idIf}";

            return new Tuple<int, Parser.AstIfEndIf?>(i, ret);
        }

        public static Tuple<int, Parser.AstGoto?> ParseGotoStatement(List<Token> tokens, int Index, Runtime runtime)
        {
            //GOTO <Expression>
            int i = Index;
            Tuple<int, Parser.AstGoto?> failure = new Tuple<int, Parser.AstGoto?>(i, null);
            if (tokens[i].Type != Token_Type.TOKEN_GOTO)
            {
                return failure;
            }
            Parser.AstGoto ret = new Parser.AstGoto(tokens[i]);
            i++;

            if (tokens[i].Type == Token_Type.TOKEN_STRING)
            {
                ret.Set(tokens[i].Text);
                i++;
            }
            else if (tokens[i].Type == Token_Type.TOKEN_INTEGER)
            {
                if (int.TryParse(tokens[i].Text, out int lineNumber))
                {
                    ret.Set(lineNumber);
                    i++;
                }
                else
                    return failure;
            }
            else
                return failure;

            return new Tuple<int, Parser.AstGoto?>(i, ret);
        }

        public static Tuple<int, Parser.AstLet?> ParseLetStatement(List<Token> tokens, int Index, Runtime runtime)
        {
            // LET Id '=' <Expression>

            int i = Index;
            Parser.AstVariable? variable = null;
            Parser.AstExpression? expression = null;

            // optional keyword LET
            if (tokens[i].Type == Token_Type.TOKEN_LET)
            {
                // Consume the let.
                i++;
            }

            // Variable
            if (i < tokens.Count && tokens[i].Type == Token_Type.TOKEN_IDENTIFIER)
            {
                // Consume the variable;
                variable = new Parser.AstVariable(tokens[i]);
                i++;
            }
            else
            {
                return new Tuple<int, Parser.AstLet?>(Index, null);
            }

            // Equals
            if (i < tokens.Count && tokens[i].Type == Token_Type.TOKEN_SET_EQUAL)
            {
                // Consume the equals
                i++;
            }
            else
            {
                return new Tuple<int, Parser.AstLet?>(Index, null);
            }

            // Expression.
            Tuple<int, Parser.AstExpression?> result = ParseExpression(tokens, i, runtime);
            if (result.Item2 != null)
            {
                expression = result.Item2;
                i = result.Item1;
            }
            else
            {
                return new Tuple<int, Parser.AstLet?>(Index, null);
            }

            Parser.AstLet statement = new Parser.AstLet(tokens[Index]);
            statement.Set(variable, expression);

            return new Tuple<int, Parser.AstLet?>(i, statement);
        }

        public static Tuple<int, Parser.AstStatements?> ParseStatements(List<Token> tokens, int Index, Runtime runtime)
        {
            /*
             <Statements>  ::= <Statement> ':' <Statements>
                            | <Statement
             */
            Parser.AstStatements statements = new(tokens[Index]);
            int i = Index;
            Tuple<int, Parser.AstStatement?> stmt = ParseStatement(tokens, i, runtime);
            if (stmt.Item2 != null)
            {
                i = stmt.Item1;
                statements.Add(stmt.Item2);
                while (stmt.Item2 != null)
                {
                    stmt = new(Index, null); // So we don't loop on end of list.
                    if (tokens.Count > i && tokens[i].Type == Token_Type.TOKEN_COLON)
                    {
                        stmt = ParseStatement(tokens, i + 1, runtime);
                        if (stmt.Item2 != null)
                        {
                            statements.Add(stmt.Item2);
                            i = stmt.Item1;
                        }
                        else
                        {
                            throw new Exception($"Incomplete statements ':' followed by non-statment, Line: {tokens[i].LineNumber}, Column: {tokens[i].ColumnNumber}");
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                return new Tuple<int, Parser.AstStatements?>(Index, null);
            }

            return new Tuple<int, Parser.AstStatements?>(i, statements);
        }

        public static Tuple<int, Parser.AstLine?> ParseLine(List<Token> tokens, int Index, Runtime runtime)
        {
            /*
             <Line>  ::= Integer? <Statements>
                            | <Statement
             */
            int i = Index;
            int? lineNumber = null;

            if (tokens.Count > i + 1 && tokens[i].Type == Token_Type.TOKEN_LOAD && tokens[i + 1].Type == Token_Type.TOKEN_STRING)
            {
                string fileName = tokens[i + 1].Text;
                if (fileName.Length >= 2)
                    fileName = fileName.Substring(1, fileName.Length - 2);
                Tuple<int, Parser.AstLine?> ret =  new Tuple<int, Parser.AstLine?>(i + 2, null);
                if (!File.Exists(fileName))
                {
                    Console.WriteLine("File not found.");
                    return ret;
                }
                string program = "";
                using (StreamReader reader = new(fileName))
                {
                    program = reader.ReadToEnd();
                }

                runtime.Clear();
                Lexer lex = new();
                List<Token> programTokens = lex.Tokenize(program).ToList<Token>();
                for (int index = 0; index < programTokens.Count; index++)
                {
                    Token token = programTokens[index];
                    while (token.Type == Token_Type.TOKEN_NEWLINE || token.Type == Token_Type.TOKEN_WHITE_SPACE && index < programTokens.Count)
                    {
                        if (token.Type == Token_Type.TOKEN_NEWLINE)
                        {
                            while (Basic.labels.Count > 0)
                            {
                                if (runtime.lineLabels.ContainsKey(Basic.labels.Peek()))
                                    Basic.labels.Pop();
                                else
                                    runtime.lineLabels.Add(Basic.labels.Pop(), runtime.program.Count);
                            }
                        }
                        index++;
                        token = programTokens[index];
                    }
                    int lineNum = runtime.program.Count;
                    Tuple<int, Parser.AstLine?> line = Basic.ParseLine(programTokens, index, runtime);

                    if (line.Item2 != null)
                    {
                        if (line.Item2.statements != null && line.Item2.statements.statements != null)
                        {
                            if (line.Item2.line != null)
                                runtime.lineNumbers.Add((int)line.Item2.line, lineNum);
                        }
                        index = line.Item1 - 1;
                    }
                    else
                    {
                        Console.WriteLine($"Unrecognized token/syntax error at Line: {programTokens[index].LineNumber}, Column: {programTokens[index].ColumnNumber} :: {programTokens[index].Text}");                        
                    }
                    
                }
                // Get any stragglers
                while (Basic.labels.Count > 0)
                {
                    if (runtime.lineLabels.ContainsKey(Basic.labels.Peek()))
                        Basic.labels.Pop();
                    else
                        runtime.lineLabels.Add(Basic.labels.Pop(), runtime.program.Count);
                }

                return ret;
            }


            if (tokens[i].Type == Token_Type.TOKEN_INTEGER)
            {
                lineNumber = Convert.ToInt32(tokens[i].Text);

                Tuple<int, Parser.AstStatements?> statements = ParseStatements(tokens, i + 1, runtime);
                if (statements.Item2 != null)
                {
                    Parser.AstLine line = new Parser.AstLine(tokens[i]);
                    line.Set((int)lineNumber, statements.Item2);
                    return new Tuple<int, Parser.AstLine?>(statements.Item1, line);
                }
                else
                {
                    // Turn line number only lines into a blank comment statement line.
                    Token tokenComment = new Token();
                    tokenComment.Type = Token_Type.TOKEN_COMMENT;
                    tokenComment.LineNumber = tokens[i].LineNumber;
                    tokenComment.ColumnNumber = tokens[i].ColumnNumber;
                    tokenComment.Text = "";
                    Parser.AstComment commentNull = new Parser.AstComment(tokenComment);
                    commentNull.comment.Text = "";
                    Parser.AstStatement stmtNull = new Parser.AstStatement(tokenComment);
                    stmtNull.Set(commentNull);
                    Parser.AstStatements stmtsNull = new Parser.AstStatements(tokenComment);
                    stmtsNull.Add(stmtNull);
                    Parser.AstLine lineNull = new Parser.AstLine(tokenComment);
                    lineNull.Set(stmtsNull);
                    return new Tuple<int, Parser.AstLine?>(statements.Item1, lineNull);
                }
            }

            i = Index;
            Tuple<int, Parser.AstStatements?> stmts = ParseStatements(tokens, i, runtime);
            if (stmts.Item2 != null)
            {
                Parser.AstLine line = new Parser.AstLine(tokens[i]);
                line.Set(stmts.Item2);
                return new Tuple<int, Parser.AstLine?>(stmts.Item1, line);
            }

            /*
            if (runtime.ifStack.Count > 0)
            {
                string label = $"{IF_END_IF_PREFIX}_{runtime.ifStack.Peek()}";
                if (runtime.lineLabels.ContainsKey(label))
                    runtime.lineLabels.Remove(label);
                runtime.lineLabels.Add(label, runtime.program.Count);
            }
            */
            return new Tuple<int, Parser.AstLine?>(Index, null);
        }

        public static Tuple<int, Parser.AstLines?> ParseLines(List<Token> tokens, int Index, Runtime runtime)
        {
            bool foundNewLine;
            int i = Index;
            Parser.AstLines lines = new(tokens[Index]);
            do
            {
                foundNewLine = false;
                Tuple<int, Parser.AstLine?> line = ParseLine(tokens, i, runtime);
                if (line.Item2 != null)
                {
                    lines.Add(line.Item2);
                    i = line.Item1;
                    while (labels.Count > 0)
                    {
                        if (runtime.lineLabels.ContainsKey(labels.Peek()))
                            labels.Pop();
                        else
                            runtime.lineLabels.Add(labels.Pop(), runtime.program.Count);
                    }
                }
                else
                {
                    break;
                }

                while (tokens.Count > i && tokens[i].Type == Token_Type.TOKEN_WHITE_SPACE)
                {
                    i++;
                }

                if (tokens.Count > i && tokens[i].Type == Token_Type.TOKEN_NEWLINE)
                {
                    foundNewLine = true;
                    i++;
                }
            } while (foundNewLine);

            if (lines.lines != null && lines.lines.Count > 0)
            {
                return new Tuple<int, Parser.AstLines?>(i, lines);
            }
            else
            {
                return new Tuple<int, Parser.AstLines?>(Index, null);
            }
        }
        public static Tuple<int, Parser.AstPrint?> ParsePrint(List<Token> tokens, int Index, Runtime runtime)
        {
            // PRINT <Print list>
            int i = Index;
            Tuple<int, Parser.AstPrint?> failure = new Tuple<int, Parser.AstPrint?>(i, null);
            if (tokens[i].Type != Token_Type.TOKEN_PRINT)
            {
                return failure;
            }
            Parser.AstPrint ret = new Parser.AstPrint(tokens[i]);
            i++;

            Tuple<int, Parser.AstPrintList?> printList = ParsePrintList(tokens, i, runtime);
            if (printList.Item2 != null)
            {
                ret.Set(printList.Item2);
                i = printList.Item1;
            }

            // Skip the CRLF?
            if (i < tokens.Count && tokens[i].Type == Token_Type.TOKEN_SEMICOLON)
            {
                ret.emitCrlf = false;
                i++;
            }

            return new Tuple<int, Parser.AstPrint?>(i, ret);
        }
        public static Tuple<int, Parser.AstPrintList?> ParsePrintList(List<Token> tokens, int Index, Runtime runtime)
        {
            // <Print List> ::= <Expression> ';' <Print List>
            //              | < Expression >
            //
            //              |
            int i = Index;
            
            Parser.AstPrintList ret = new Parser.AstPrintList(tokens[i]);

            Tuple<int, Parser.AstExpression?> exp = ParseExpression(tokens, i, runtime);
            bool success = exp.Item2 != null;
            while (success && exp.Item2 != null)
            {
                ret.Add(exp.Item2);
                i = exp.Item1;

                // next is semicolon.
                success = false;
                if (i < tokens.Count && tokens[i].Type == Token_Type.TOKEN_SEMICOLON && tokens.Count > i + 1)
                {                    
                    exp = ParseExpression(tokens, i + 1, runtime);
                    success = exp.Item2 != null;
                }
            }
            
            return new Tuple<int, Parser.AstPrintList?>(i, ret);
        }

        public static Tuple<int, Parser.AstIDList?> ParseIDList(List<Token> tokens, int Index, Runtime runtime)
        {
            /*
             <ID List>  ::= ID ',' <ID List> 
             | ID
             */
            int i = Index;
            Tuple<int, Parser.AstIDList?> failure = new Tuple<int, Parser.AstIDList?>(Index, null);
            Parser.AstIDList ret = new(tokens[i]);

            Tuple<int, Parser.AstValue?> variable = ParseValue(tokens, i, runtime);
            while (variable.Item2 != null && variable.Item2.nodeVariable != null)
            {
                i = variable.Item1;
                ret.Add(variable.Item2.nodeVariable);

                if (tokens.Count > i && tokens[i].Type == Token_Type.TOKEN_COMMA)
                {
                    variable = ParseValue(tokens, i + 1, runtime);
                }
                else
                {
                    break;
                }
            }
            return new Tuple<int, Parser.AstIDList?>(i, ret);
        }

        public static Tuple<int, Parser.AstInput?> ParseInput(List<Token> tokens, int Index, Runtime runtime)
        {
            int i = Index;
            Tuple<int, Parser.AstInput?> failure = new(Index, null);
            if (tokens[i].Type != Token_Type.TOKEN_INPUT)
            {
                return failure;
            }
            Parser.AstInput ret = new Parser.AstInput(tokens[i]);
            i++;

            // do we have a prompt
            string prompt = "";
            if (tokens.Count > i + 2 && tokens[i].Type == Token_Type.TOKEN_STRING && tokens[i + 1].Type == Token_Type.TOKEN_SEMICOLON)
            {
                prompt = tokens[i].Text;
                if (prompt.Length >= 2)
                    prompt = prompt.Substring(1, prompt.Length - 2);

                i += 2;
            }

            Tuple<int, Parser.AstIDList?> ids = ParseIDList(tokens, i, runtime);
            if (ids.Item2 == null)
                return failure;
            i = ids.Item1;
            ret.Set(prompt, ids.Item2);
            return new Tuple<int ,  Parser.AstInput?>(i, ret);
        }

        public static Tuple<int, Parser.AstEnd?> ParseEnd(List<Token> tokens, int Index, Runtime runtime)
        {
            int i = Index;
            Tuple<int, Parser.AstEnd?> failure = new(Index, null);
            if (tokens[i].Type != Token_Type.TOKEN_END)
            {
                return failure;
            }

            Parser.AstEnd ret = new Parser.AstEnd(tokens[i]);
            i++;

            if (tokens.Count > i && tokens[i].Type == Token_Type.TOKEN_IF)
            {
                return failure;
            }

            return new Tuple<int, Parser.AstEnd?>(i, ret);
        }
    }
}
