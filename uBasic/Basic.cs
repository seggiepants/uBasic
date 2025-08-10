using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace uBasic
{
    public class Basic()
    {
        /*
        AstNode ParseLet(IEnumerable<Token> tokens, int Index)
        {
            // (LET)? IDENTIFIER EQUALS EXPRESSION
            AstNode ret = null;

        }
        */

        public static Tuple<int, Parser.AstValue?> ParseValue(List<Token> tokens, int Index)
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
                Tuple<int, Parser.AstExpression?> expression = ParseExpression(tokens, Index + 1);
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
                    while(readArgs)
                    {
                        Tuple<int, Parser.AstExpression?> expression = ParseExpression(tokens, i);
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
            Tuple<int, Parser.AstConstant?> constant = ParseConstant(tokens, Index);
            if (constant.Item2 != null)
            {
                val.Set(constant.Item2);
                return new Tuple<int, Parser.AstValue?>(constant.Item1, val);
            }
            return new Tuple<int, Parser.AstValue?>(Index, null);
        }

        public static Tuple<int, Parser.AstConstant?> ParseConstant(List<Token> tokens, int Index)
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

        public static Tuple<int, Parser.AstPowerExpression?> ParsePowerExpression(List<Token> tokens, int Index)
        {
            /*
             <Power Exp> '^' <Value> 
                | <Value> 
             */
            Tuple<int, Parser.AstValue?> lhs;
            Tuple<int, Parser.AstValue?> rhs;
            Parser.AstPowerExpression? power = null;

            int i = Index;
            lhs = ParseValue(tokens, i);
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
                    rhs = ParseValue(tokens, i + 1);
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

        public static Tuple<int, Parser.AstNegateExpression?> ParseNegateExpression(List<Token> tokens, int Index)
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
            ret = ParsePowerExpression(tokens, i);
            if (ret.Item2 != null)
            {
                Parser.AstNegateExpression negateExp = new(tokens[i]);
                negateExp.Set(ret.Item2, negate);
                return new Tuple<int, Parser.AstNegateExpression?>(ret.Item1, negateExp);
            }

            return new Tuple<int, Parser.AstNegateExpression?>(Index, null);
        }

        public static Tuple<int, Parser.AstMultiplyExpression?> ParseMultiplyExpression(List<Token> tokens, int Index)
        {
            /*
             <Negate Exp> '*' <Mult Exp> 
                | <Negate Exp> '/' <Mult Exp> 
                | <Negate Exp>   
             */
            int i = Index;
            Stack<Parser.AstNegateExpression>? exps = null;
            Stack<Token>? ops = null;
            Tuple<int, Parser.AstNegateExpression?> lhs = ParseNegateExpression(tokens, i);
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
                    lhs = ParseNegateExpression(tokens, i + 1);
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


        public static Tuple<int, Parser.AstAddExpression?> ParseAddExpression(List<Token> tokens, int Index)
        {
            /*
             <Add Exp>  ::= <Mult Exp> '+' <Add Exp> 
                | <Mult Exp> '-' <Add Exp> 
                | <Mult Exp> 
             */
            int i = Index;
            Stack<Parser.AstMultiplyExpression>? exps = null;
            Stack<Token>? ops = null;
            Tuple<int, Parser.AstMultiplyExpression?> lhs = ParseMultiplyExpression(tokens, i);
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
                    lhs = ParseMultiplyExpression(tokens, i + 1);
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

        public static Tuple<int, Parser.AstCompareExpression?> ParseCompareExpression(List<Token> tokens, int Index)
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
            Tuple<int, Parser.AstAddExpression?> lhs = ParseAddExpression(tokens, i);
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
                    lhs = ParseAddExpression(tokens, i + 1);
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

        public static Tuple<int, Parser.AstNotExpression?> ParseNotExpression(List<Token> tokens, int Index)
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
            ret = ParseCompareExpression(tokens, i);
            if (ret.Item2 != null)
            {
                Parser.AstNotExpression notExp = new(tokens[i]);
                notExp.Set(negate, ret.Item2);
                return new Tuple<int, Parser.AstNotExpression?>(ret.Item1, notExp);
            }

            return new Tuple<int, Parser.AstNotExpression?>(Index, null);
        }

        public static Tuple<int, Parser.AstAndExpression?> ParseAndExpression(List<Token> tokens, int Index)
        {
            /*
             <And Exp>     ::= <Not Exp> AND <And Exp> 
                              | <Not Exp>
             */
            int i = Index;
            Stack<Parser.AstNotExpression>? exps = null;
            Tuple<int, Parser.AstNotExpression?> lhs = ParseNotExpression(tokens, i);
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
                    lhs = ParseNotExpression(tokens, i + 1);
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

        public static Tuple<int, Parser.AstExpression?> ParseExpression(List<Token> tokens, int Index)
        {
            /*
             <Expression>  ::= <And Exp> OR <Expression> 
                            | <And Exp>
             */
            int i = Index;
            Stack<Parser.AstAndExpression>? exps = null;
            Tuple<int, Parser.AstAndExpression?> lhs = ParseAndExpression(tokens, i);
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
                    lhs = ParseAndExpression(tokens, i + 1);
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
        
        public static Tuple<int, Parser.AstStatement?> ParseStatement(List<Token> tokens, int Index)
        {
            Tuple<int, Parser.AstLet?> let = ParseLetStatement(tokens, Index);
            if (let.Item2 != null)
            {
                Parser.AstStatement statement = new Parser.AstStatement(tokens[Index]);
                statement.Set(let.Item2);
                return new Tuple<int, Parser.AstStatement?>(let.Item1, statement);
            }
            // ZZZ -- Add more, a lot more.
            return new Tuple<int, Parser.AstStatement?>(Index, null);
        }

        public static Tuple<int, Parser.AstLet?> ParseLetStatement(List<Token> tokens, int Index)
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
            Tuple<int, Parser.AstExpression?> result = ParseExpression(tokens, i);
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

    }
}
