using System.Text;
using static uBasic.Parser;

namespace uBasic
{
    public static class Interpreter
    {
        public static object? Interpret(this Parser.AstNode node, Runtime runtime) { return null; }

        public static object? Interpret(this Parser.AstToken node, Runtime runtime)
        {
            if (node.Type == Token_Type.TOKEN_LIST)
            {
                StringBuilder sb = new();
                foreach(KeyValuePair<int, Parser.AstStatements> line in runtime.program)
                {
                    sb.AppendLine($"{line.Key} {line.Value}");
                }
                return sb.ToString();
            }
            else if (node.Type == Token_Type.TOKEN_RUN)
            {
                object? result = null;
                foreach (KeyValuePair<int, Parser.AstStatements> line in runtime.program)
                {
                    result = line.Value.Interpret(runtime);
                }
                return result;
            }
            return null;
        }
        public static bool Interpret(this Parser.AstBoolean node, Runtime runtime) { return node.Value; }
        public static int Interpret(this Parser.AstInteger node, Runtime runtime) { return node.Value; }
        public static double Interpret(this Parser.AstFloat node, Runtime runtime) { return node.Value; }
        public static string Interpret(this Parser.AstString node, Runtime runtime) { return node.Value; }
        public static object? Interpret(this Parser.AstVariable node, Runtime runtime) { return runtime.symbolTable.Get(node.Name); }
        public static object? Interpret(this Parser.AstConstant node, Runtime runtime) {
            if (node.nodeBool != null)
                return node.nodeBool.Interpret(runtime);
            else if (node.nodeInt != null)
                return node.nodeInt.Interpret(runtime);
            else if (node.nodeFloat != null)
                return node.nodeFloat.Interpret(runtime);
            else if (node.nodeString != null)
                return node.nodeString.Interpret(runtime);
            return null;
        }
        public static object? Interpret(this Parser.AstValue node, Runtime runtime) 
        {
            if (node.nodeConstant != null)
                return node.nodeConstant.Interpret(runtime);
            else if (node.nodeExpression != null)
                return node.nodeExpression.Interpret(runtime);
            else if (node.nodeFunction != null)
            {
                // ZZZ Broken
                return node.nodeFunction.Interpret(runtime);
            }
            else if (node.nodeVariable != null)
                return node.nodeVariable.Interpret(runtime);

            return null;
        }
        
        public static object? Interpret(this Parser.AstFunctionCall node, Runtime runtime)
        {
            if (node.function != null)
                return runtime.fnTable.Call(node.function.Name, node.args, runtime);
            return null;
        }

        public static object? Interpret(this Parser.AstPowerExpression node, Runtime runtime)
        {
            if (node.powerExpression == null)
                return node.value?.Interpret(runtime);
            object? expPower = node.value?.Interpret(runtime);
            object? expBase = node.powerExpression.Interpret(runtime);
            if ((expPower?.GetType() == typeof(int) || expPower?.GetType() == typeof(double)) &&
                     (expBase?.GetType() == typeof(int) || expBase?.GetType() == typeof(double)))
            {
                double power = Convert.ToDouble(expPower);
                double value = Convert.ToDouble(expBase);
                if (expPower?.GetType() == typeof(int) && expBase?.GetType() == typeof(int))
                    return (int)Math.Pow(power, value);
                else
                    return Math.Pow(value, power);
            }
            else
            {
                throw new Exception("Power expression must use two numbers.");
            }
        }

        public static object? Interpret(this Parser.AstNegateExpression node, Runtime runtime)
        {            
            object? result = node.powerExpression?.Interpret(runtime);
            if (result == null)
                return null;
            else if (!node.negate)
                return result;
            else
            {
                if (result?.GetType() == typeof(int))
                    return -1 * (int)result;
                else if (result?.GetType() == typeof(double))
                    return -1.0 * (double)result;
                else
                    throw new Exception("Negate expression requires a numeric value.");
            }
        }

        public static object? Interpret(this Parser.AstMultiplyExpression node, Runtime runtime)
        {
            if (node.negateExpression != null && node.multiplyExpression == null)
            {
                return node.negateExpression.Interpret(runtime);
            }
            else if (node.negateExpression != null && node.multiplyExpression != null)
            {
                object? lhs = node.negateExpression.Interpret(runtime);
                object? rhs = node.multiplyExpression.Interpret(runtime);
                if (lhs != null && rhs != null)
                {
                    // String multiply?
                    if (node.op?.Type == Token_Type.TOKEN_MULTIPLY && lhs.GetType() == typeof(string) && (rhs.GetType() == typeof(int) || rhs.GetType() == typeof(double)))
                    {
                        int count = (int)rhs;
                        StringBuilder sb = new();
                        for (int i = 0; i < count; i++)
                            sb.Append((string)lhs);
                        return sb.ToString();
                    }
                    else if (lhs.GetType() == typeof(int) && rhs.GetType() == typeof(int))
                    {
                        if (node.op?.Type == Token_Type.TOKEN_MULTIPLY)
                            return (int)lhs * (int)rhs;
                        else // Divide
                            return (int)lhs / (int)rhs;
                    }
                    else // better be double/double, int/double, or double/int
                    {
                        if (node.op?.Type == Token_Type.TOKEN_MULTIPLY)
                            return Convert.ToDouble(lhs) * Convert.ToDouble(rhs);
                        else // Divide
                            return Convert.ToDouble(lhs) / Convert.ToDouble(rhs);
                    }

                }
                else
                {
                    throw new Exception("Multiply/Divide expression one/both sides of expression do not evaluate to a number.");
                }
            }
            else
                throw new Exception("Multiply/Divide expression is incomplete.");
        }
        public static object? Interpret(this Parser.AstAddExpression node, Runtime runtime)
        {
            if (node.multiplyExpression != null && node.addExpression  == null)
            {
                return node.multiplyExpression.Interpret(runtime);
            }
            else if (node.multiplyExpression != null && node.addExpression != null)
            {
                object? lhs = node.multiplyExpression.Interpret(runtime);
                object? rhs = node.addExpression.Interpret(runtime);
                if (lhs != null && rhs != null)
                {
                    if (lhs.GetType() == typeof(string) && node.op != null && node.op.Type == Token_Type.TOKEN_ADD)
                    {
                        return (string)lhs + rhs.ToString();
                    }
                    else if (lhs.GetType() == typeof(int) && rhs.GetType() == typeof(int))
                    {
                        if (node.op?.Type == Token_Type.TOKEN_ADD)
                            return (int)lhs + (int)rhs;
                        else // Subtract
                            return (int)lhs - (int)rhs;
                    }
                    else // better be double/double, int/double, or double/int
                    {
                        if (node.op?.Type == Token_Type.TOKEN_ADD)
                            return Convert.ToDouble(lhs) + Convert.ToDouble(rhs);
                        else // Subtract
                            return Convert.ToDouble(lhs) - Convert.ToDouble(rhs);
                    }
                }
                else
                {
                    throw new Exception("Add/Subtract expression one/both sides of expression do not evaluate to a number.");
                }
            }
            else
                throw new Exception("Add/Subtract expression is incomplete.");
        }

        public static object? Interpret(this Parser.AstCompareExpression node, Runtime runtime)
        {
            if (node.lhs != null && node.rhs == null)
            {
                return node.lhs.Interpret(runtime);
            }
            else if (node.lhs != null && node.rhs != null)
            {
                object? lhs = node.lhs.Interpret(runtime);
                object? rhs = node.rhs.Interpret(runtime);
                if (lhs != null && rhs != null && node.op != null)
                {
                    if (lhs.GetType() == typeof(bool) && rhs.GetType() == typeof(bool))
                    {
                        switch(node.op.Type)
                        {
                            case Token_Type.TOKEN_IS_EQUAL:
                                return lhs == rhs;
                            case Token_Type.TOKEN_NOT_EQUAL:
                                return lhs != rhs;
                            default:
                                throw new Exception("Booleans only allow equal and not equal checks.");
                        }
                    }
                    if (lhs.GetType() == typeof(int) && rhs.GetType() == typeof(int))
                    {
                        switch (node.op?.Type)
                        {
                            case Token_Type.TOKEN_IS_EQUAL:
                                return (int)lhs == (int)rhs;
                            case Token_Type.TOKEN_NOT_EQUAL:
                                return (int)lhs != (int)rhs;
                            case Token_Type.TOKEN_GREATER_THAN:
                                return (int)lhs > (int)rhs;
                            case Token_Type.TOKEN_GREATER_EQUAL:
                                return (int)lhs >= (int)rhs;
                            case Token_Type.TOKEN_LESS_THAN:
                                return (int)lhs < (int)rhs;
                            case Token_Type.TOKEN_LESS_EQUAL:
                                return (int)lhs <= (int)rhs;
                            default:
                                throw new Exception("Invaild comparison operation.");
                        }
                    }
                    else // better be double/double, int/double, or double/int
                    {
                        switch (node.op?.Type)
                        {
                            case Token_Type.TOKEN_IS_EQUAL:
                                return (double)lhs == (double)rhs;
                            case Token_Type.TOKEN_NOT_EQUAL:
                                return (double)lhs != (double)rhs;
                            case Token_Type.TOKEN_GREATER_THAN:
                                return (double)lhs > (double)rhs;
                            case Token_Type.TOKEN_GREATER_EQUAL:
                                return (double)lhs >= (double)rhs;
                            case Token_Type.TOKEN_LESS_THAN:
                                return (double)lhs < (double)rhs;
                            case Token_Type.TOKEN_LESS_EQUAL:
                                return (double)lhs <= (double)rhs;
                            default:
                                throw new Exception("Invaild comparison operation.");
                        }
                    }
                }
                else
                {
                    throw new Exception("Comparison expression one/both sides of expression do not evaluate to a number/boolean.");
                }
            }
            else
                throw new Exception("Comparison expression is incomplete.");
        }

        public static object? Interpret(this Parser.AstNotExpression node, Runtime runtime)
        {
            if (node.compare == null)
                return null;

            object? result = node.compare.Interpret(runtime);
            if (result == null)
                return null;

            if (node.negate == false)
                return result;                
            else
                if (result.GetType() == typeof(bool))
                    return !(bool)result;
                else if (result.GetType() == typeof(double))
                    return (double)result == 0.0;
                else if (result.GetType() == typeof(int))
                    return (int)result == 0;
                else
                    throw new Exception("Not can only be evaluated with a boolean, or numeric value.");
        }

        public static object? Interpret(this Parser.AstAndExpression node, Runtime runtime)
        {
            if (node.lhs != null && node.rhs == null)
            {
                return node.lhs.Interpret(runtime);
            }
            else if (node.lhs != null && node.rhs != null)
            {
                object? lhs = node.lhs.Interpret(runtime);
                object? rhs = node.rhs.Interpret(runtime);
                if (lhs != null && rhs != null)
                {
                    bool a, b;
                    if (lhs.GetType() == typeof(bool))
                        a = (bool)lhs;
                    else if (lhs.GetType() == typeof(int))
                        a = ((int)lhs) != 0;
                    else if (lhs.GetType() == typeof(double))
                        a = ((double)lhs) != 0.0;
                    else
                        throw new Exception("AND expression (left hand side) is not a boolean or numeric.");

                    if (rhs.GetType() == typeof(bool))
                        b = (bool)rhs;
                    else if (rhs.GetType() == typeof(int))
                        b = ((int)rhs) != 0;
                    else if (rhs.GetType() == typeof(double))
                        b = ((double)rhs) != 0.0;
                    else
                        throw new Exception("AND expression (right hand side) is not a boolean or numeric.");

                    return a && b;
                }
                else
                {
                    throw new Exception("AND expression one/both sides of expression do not evaluate to a number/boolean.");
                }
            }
            else
                throw new Exception("AND expression is incomplete.");
        }

        public static object? Interpret(this Parser.AstExpression node, Runtime runtime)
        {
            if (node == null)
                return "Not yet implemented.";

            if (node.lhs != null && node.rhs == null)
            {
                return node.lhs.Interpret(runtime);
            }
            else if (node.lhs != null && node.rhs != null)
            {
                object? lhs = node.lhs.Interpret(runtime);
                object? rhs = node.rhs.Interpret(runtime);
                if (lhs != null && rhs != null)
                {
                    bool a, b;
                    if (lhs.GetType() == typeof(bool))
                        a = (bool)lhs;
                    else if (lhs.GetType() == typeof(int))
                        a = ((int)lhs) != 0;
                    else if (lhs.GetType() == typeof(double))
                        a = ((double)lhs) != 0.0;
                    else
                        throw new Exception("OR expression (left hand side) is not a boolean or numeric.");

                    if (rhs.GetType() == typeof(bool))
                        b = (bool)rhs;
                    else if (rhs.GetType() == typeof(int))
                        b = ((int)rhs) != 0;
                    else if (rhs.GetType() == typeof(double))
                        b = ((double)rhs) != 0.0;
                    else
                        throw new Exception("OR expression (right hand side) is not a boolean or numeric.");

                    return a || b;
                }
                else
                {
                    throw new Exception("OR expression one/both sides of expression do not evaluate to a number/boolean.");
                }
            }
            else
                throw new Exception("OR expression is incomplete.");
        }

        public static object? Interpret(this Parser.AstStatement node, Runtime runtime)
        {
            if (node.stmtLet != null)
                return node.stmtLet.Interpret(runtime);
            else if (node.stmtFor != null)
                return node.stmtFor.Interpret(runtime);
            else if (node.stmtIf != null)
                return node.stmtIf.Interpret(runtime);
            else if (node.stmtComment != null)
                return null; // Nothing to interpret

            return null;
        }

        public static object? Interpret(this Parser.AstLet node, Runtime runtime)
        {
            if (node.expression == null)
                throw new ArgumentException("Let statement doesn't have a value to assign.");
            if (node.variable == null)
                throw new ArgumentException("Let statement doesn't have a variable to assign to.");
            object? value = node.expression.Interpret(runtime);
            runtime.symbolTable.Set(node.variable.Name, value);            
            return value;
        }

        public static object? Interpret(this Parser.AstComment node, Runtime runtime)
        {
            // Do nothing, it is a comment.
            return null;
        }

        public static object? Interpret(this Parser.AstStatements node, Runtime runtime)
        {
            if (node.statements != null)
            {
                object? result = null;
                foreach(AstStatement stmt in node.statements)
                {
                    result = stmt.Interpret(runtime);
                }
                return result;
            }
            return null;
        }

        public static object? Interpret(this Parser.AstLine node, Runtime runtime)
        {
            if (node.statements != null)
            {
                // Just interpret it.
                return node.statements.Interpret(runtime);
            }
            return null;
        }

        public static object? Interpret(this Parser.AstLines node, Runtime runtime)
        {
            if (node.lines != null)
            {
                object? result = null;
                foreach(AstLine line in node.lines)
                {
                    result = line.Interpret(runtime);
                }
                return result;
            }
            return null;
        }

        private static bool CheckBool(object? value)
        {
            if (value == null)
                return false;

            if (value.GetType() == typeof(bool))
                return (bool)value;

            if (value.GetType() == typeof(int))
                return (int)value > 0;


            if (value.GetType() == typeof(double))
                return (double)value > 0.0;

            return false;
        }

        public static object? Interpret(this Parser.AstFor node, Runtime runtime)
        {
            if (node.id == null || node.beginExp == null || node.endExp == null)
                return null;

            object? result = node.beginExp.Interpret(runtime);
            runtime.symbolTable.Set(node.id.Name, result);
            bool done = false;
            do
            {
                if (node.lines != null && node.lines.lines != null)
                {
                    foreach (AstLine line in node.lines.lines)
                    {
                        result = line.Interpret(runtime);
                    }
                }

                result = runtime.symbolTable.Get(node.id.Name);
                if (result != null && result.GetType() == typeof(int))
                    result = (int)result + node.step;
                else if (result != null && result.GetType() == typeof(double))
                    result = (double)result + node.step;
                else
                    throw new Exception($"Cannot perform numeric addition on variable \"{node.id.Name}\"");

                runtime.symbolTable.Set(node.id.Name, result);

                object? last = node.endExp.Interpret(runtime);

                if (last != null && last.GetType() == typeof(int))
                {
                    done = (int)last == (int)result;
                }
                else if (last != null && last.GetType() == typeof(double))
                {
                    done = (double)last == (double)result;
                }
                else
                    throw new Exception($"Cannont determine loop end condition");

            } while (!done);
            return result;
        }

        public static object? Interpret(this Parser.AstIf node, Runtime runtime)
        {
            if (node.exp == null)
                return null;

            object? result = node.exp.Interpret(runtime);
            if (CheckBool(result))
            {
                if (node.lines == null || node.lines.lines == null)
                    return null;

                foreach (AstLine line in node.lines.lines)
                {
                    result = line.Interpret(runtime);
                }
                return result;
            }
            // Now check elseif
            if (node.elseIfClauses != null && node.elseIfClauses.Count > 0)
            {
                foreach(Parser.AstConditionAndLines condLines in node.elseIfClauses)
                {
                    if (condLines.exp == null || condLines.lines == null)
                        continue;
                    result = condLines.exp.Interpret(runtime);
                    if (CheckBool(result) && condLines.lines != null && condLines.lines.lines != null)
                    {
                        foreach (AstLine line in condLines.lines.lines)
                        {
                            result = line.Interpret(runtime);
                        }
                        return result;
                    }
                }
            }

            if (node.elseLines != null && node.elseLines.lines != null)
            {
                foreach (AstLine line in node.elseLines.lines)
                {
                    result = line.Interpret(runtime);
                }
                return result;
            }    
            return null;
        }
    }
}
