using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using static uBasic.Parser;

namespace uBasic
{
    public static class Interpreter
    {
        const string OK = "OK";
        const string FOR_PREFIX = "#FOR_";
        const string NEXT_PREFIX = "#NEXT_";

        const string IF_PREFIX = "#IF_";
        const string IF_NEXT_PREFIX = "#IF_NEXT_";
        const string IF_END_IF_PREFIX = "#ENDIF_";

        public static object? Interpret(this Parser.AstNode node, Runtime runtime) { return null; }

        public static object? Interpret(this Parser.AstToken node, Runtime runtime)
        {
            if (node.Type == Token_Type.TOKEN_LIST)
            {
                bool showInternal = false;
                StringBuilder sb = new();
                for (int i = 0; i < runtime.program.Count; i++)
                {
                    AstStatement stmt = runtime.program[i];
                    if (runtime.lineLabels.ContainsValue(i))
                    {
                        string[] labels = (from pair in runtime.lineLabels
                                           where pair.Value == i
                                           select pair.Key).ToArray<string>();
                        foreach (string label in labels)
                            if (!label.StartsWith("#") || showInternal)
                                sb.AppendLine(label + ":");
                    }
                    string lineNum = "";
                    if (runtime.lineNumbers.ContainsValue(i))
                    {
                        lineNum = string.Join('\n', (from pair in runtime.lineNumbers
                                                     where pair.Value == i
                                                     select $"{pair.Key} "));
                    }
                    if (stmt.stmtIf != null && stmt.stmtIf.multiLine == false)
                    {
                        sb.Append($"{lineNum}{stmt} ");
                    }
                    else if (stmt.stmtPrint != null && stmt.stmtPrint.emitCrlf == false)
                    {
                        sb.AppendLine($"{lineNum}{stmt};");
                    }
                    else
                    {
                        sb.AppendLine($"{lineNum}{stmt}");
                    }
                }
                return sb.ToString();
            }
            else if (node.Type == Token_Type.TOKEN_RUN)
            {
                return runtime.Run() ?? OK;
            }
            return null;
        }
        public static bool Interpret(this Parser.AstBoolean node, Runtime runtime) { return node.Value; }
        public static int Interpret(this Parser.AstInteger node, Runtime runtime) { return node.Value; }
        public static double Interpret(this Parser.AstFloat node, Runtime runtime) { return node.Value; }
        public static string Interpret(this Parser.AstString node, Runtime runtime) { return node.Value; }
        public static object? Interpret(this Parser.AstVariable node, Runtime runtime) { return runtime.symbolTable.Get(node.Name); }
        public static object? Interpret(this Parser.AstConstant node, Runtime runtime)
        {
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
            if (node.nodeArrayAccess != null)
                return node.nodeArrayAccess.Interpret(runtime);
            else if (node.nodeConstant != null)
                return node.nodeConstant.Interpret(runtime);
            else if (node.nodeExpression != null)
                return node.nodeExpression.Interpret(runtime);
            else if (node.nodeFunctionCall != null)
                return node.nodeFunctionCall.Interpret(runtime);
            else if (node.nodeVariable != null)
                return node.nodeVariable.Interpret(runtime);

            return null;
        }

        public static object? Interpret(this Parser.AstFunctionCall node, Runtime runtime)
        {
            if (node.function != null)
                return runtime.fnTable.Call(node.function, node.args, runtime);
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
            if (node.multiplyExpression != null && node.addExpression == null)
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
                        switch (node.op.Type)
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
                    else if (lhs.GetType() == typeof(string) && rhs.GetType() == typeof(string))
                    {
                        switch (node.op?.Type)
                        {
                            case Token_Type.TOKEN_IS_EQUAL:
                                return (string)lhs == (string)rhs;
                            case Token_Type.TOKEN_NOT_EQUAL:
                                return (string)lhs != (string)rhs;
                            case Token_Type.TOKEN_GREATER_THAN:
                                return ((string)lhs).CompareTo((string)rhs) > 0;
                            case Token_Type.TOKEN_GREATER_EQUAL:
                                return ((string)lhs).CompareTo((string)rhs) > 0;
                            case Token_Type.TOKEN_LESS_THAN:
                                return ((string)lhs).CompareTo((string)rhs) < 0;
                            case Token_Type.TOKEN_LESS_EQUAL:
                                return ((string)lhs).CompareTo((string)rhs) <= 0;
                            default:
                                throw new Exception("Invaild comparison operation.");
                        }
                    }
                    else // better be double/double, int/double, or double/int
                    {
                        lhs = Convert.ToDouble(lhs);
                        rhs = Convert.ToDouble(rhs);
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
            else if (node.stmtData != null)
                return node.stmtData.Interpret(runtime);
            else if (node.stmtDim != null)
                return node.stmtDim.Interpret(runtime);
            else if (node.stmtEnd != null)
                return node.stmtEnd.Interpret(runtime);
            else if (node.stmtFor != null)
                return node.stmtFor.Interpret(runtime);
            else if (node.stmtForNext != null)
                return node.stmtForNext.Interpret(runtime);
            else if (node.stmtFunctionCall != null)
                return node.stmtFunctionCall.Interpret(runtime);
            else if (node.stmtGosub != null)
                return node.stmtGosub.Interpret(runtime);
            else if (node.stmtGoto != null)
                return node.stmtGoto.Interpret(runtime);
            else if (node.stmtIf != null)
                return node.stmtIf.Interpret(runtime);
            else if (node.stmtIfElseIf != null)
                return node.stmtIfElseIf.Interpret(runtime);
            else if (node.stmtIfElse != null)
                return node.stmtIfElse.Interpret(runtime);
            else if (node.stmtIfEndIf != null)
                return node.stmtIfEndIf.Interpret(runtime);
            else if (node.stmtInput != null)
                return node.stmtInput.Interpret(runtime);
            else if (node.stmtPrint != null)
                return node.stmtPrint.Interpret(runtime);
            else if (node.stmtReturn != null)
                return node.stmtReturn.Interpret(runtime);
            else if (node.stmtComment != null)
                return null; // Nothing to interpret

            return null;
        }

        public static object? Interpret(this Parser.AstLet node, Runtime runtime)
        {
            if (node.expression == null)
                throw new ArgumentException("Let statement doesn't have a value to assign.");
            if (node.variable == null && node.arrayRef == null)
                throw new ArgumentException("Let statement doesn't have a variable to assign to.");
            object? value = node.expression.Interpret(runtime);
            if (node.variable != null)
                runtime.symbolTable.Set(node.variable.Name, value);
            else if (node.arrayRef != null)
            {
                if (node.arrayRef.exps != null && node.arrayRef.exps.expList != null)
                {
                    int[] rank = (from exp in node.arrayRef.exps.expList
                                  select Convert.ToInt32(exp.Interpret(runtime))).ToArray<int>();
                    if (rank.Length > 4)
                        throw new Exception($"Too many indexes to array ${node.variable}");
                    object? arr = runtime.symbolTable.Get(node.arrayRef.variable);
                    if (arr != null)
                    {
                        if (rank.Length == 1)
                            (arr as object?[])[rank[0]] = value;
                        else if (rank.Length == 2)
                            (arr as object?[,])[rank[0], rank[1]] = value;
                        else if (rank.Length == 3)
                            (arr as object?[,,])[rank[0], rank[1], rank[2]] = value;
                        else if (rank.Length == 4)
                            (arr as object?[,,,])[rank[0], rank[1], rank[2], rank[3]] = value;
                        else
                            throw new Exception($"Too many indexes to array ${node.variable}");
                    }
                    else
                        throw new Exception($"Variable {node.arrayRef.variable} not found.");
                }
                else
                    throw new Exception($"Array access on variable {node.arrayRef.variable} not passed index(s)");
            }
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
                for (int i = 0; i < node.statements.Count; i++)
                {
                    AstStatement stmt = node.statements[i];
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
                foreach (AstLine line in node.lines)
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

            object? result;
            if (node.calledFromNext)
            {
                if (node.id == null)
                    return null;
                result = runtime.symbolTable.Get(node.id.Name);
                if (result != null && result.GetType() == typeof(int))
                    result = (int)result + node.step;
                else if (result != null && result.GetType() == typeof(double))
                    result = (double)result + node.step;
                else
                    throw new Exception($"Cannot perform numeric addition on variable \"{node.id.Name}\"");
                runtime.symbolTable.Set(node.id.Name, result);
            }
            else
            {
                result = node.beginExp.Interpret(runtime);
                runtime.symbolTable.Set(node.id.Name, result);
            }

            object? last = node.endExp.Interpret(runtime);
            bool done = false;
            if (result != null && last != null && last.GetType() == typeof(int))
            {
                if (node.step > 0)
                    done = (int)result > (int)last;
                else if (node.step < 0)
                    done = (int)result < (int)last;
                else
                    done = (int)result == (int)last;
            }
            else if (result != null && last != null && last.GetType() == typeof(double))
            {
                if (node.step > 0)
                    done = (double)result > (double)last;
                else if (node.step < 0)
                    done = (double)result < (double)last;
                else
                    done = (double)result == (double)last;
            }
            else
                throw new Exception($"Cannont determine loop end condition");

            if (done)
            {
                // jump to 1 past next.
                string nextLabel = node.label.Replace(FOR_PREFIX, NEXT_PREFIX);
                if (runtime.lineLabels.ContainsKey(nextLabel))
                {
                    runtime.instructionPointer = runtime.lineLabels[nextLabel] + 1;
                }
            }
            node.calledFromNext = false;
            return result;
        }

        public static object? Interpret(this Parser.AstForNext node, Runtime runtime)
        {
            string forLabel = node.label.Replace(NEXT_PREFIX, FOR_PREFIX);
            if (runtime.lineLabels.ContainsKey(forLabel))
            {
                int line = runtime.lineLabels[forLabel];
                AstStatement? forStmt = runtime.program[line];
                if (forStmt != null && forStmt.stmtFor != null)
                {
                    forStmt.stmtFor.calledFromNext = true;
                    runtime.instructionPointer = line;
                }
            }
            return null;
        }

        public static object? Interpret(this Parser.AstIf node, Runtime runtime)
        {
            if (node.exp == null)
                return null;

            object? result = node.exp.Interpret(runtime);
            if (!CheckBool(result))
            {
                // find else if
                string nextLabel = node.label.Replace(IF_PREFIX, IF_NEXT_PREFIX) + "_1";
                if (!runtime.lineLabels.ContainsKey(nextLabel))
                {
                    // find endif
                    nextLabel = node.label.Replace(IF_PREFIX, IF_END_IF_PREFIX);
                    if (!runtime.lineLabels.ContainsKey(nextLabel))
                    {
                        return null;
                    }
                }
                // label should be ok now.
                runtime.instructionPointer = runtime.lineLabels[nextLabel];
            }
            return result;
        }
        public static object? Interpret(this Parser.AstIfElseIf node, Runtime runtime)
        {
            if (node.exp == null)
                return null;

            object? result = node.exp.Interpret(runtime);
            if (!CheckBool(result))
            {
                // find next else if
                int lastUnderscore = node.label.LastIndexOf('_');
                if (lastUnderscore == -1)
                    return null;
                string prefix = node.label.Substring(0, lastUnderscore);

                if (!int.TryParse(node.label.Substring(lastUnderscore + 1), out int counter))
                    return null;

                string nextLabel = $"{prefix}_{counter + 1}";
                if (!runtime.lineLabels.ContainsKey(nextLabel))
                {
                    // find endif
                    nextLabel = prefix.Replace(IF_NEXT_PREFIX, IF_END_IF_PREFIX);
                    if (!runtime.lineLabels.ContainsKey(nextLabel))
                    {
                        return null; // Error
                    }
                }
                // label should be ok now.
                runtime.instructionPointer = runtime.lineLabels[nextLabel];
            }

            return result;
        }

        public static object? Interpret(this Parser.AstIfElse node, Runtime runtime)
        {
            return OK;
        }

        public static object? Interpret(this Parser.AstIfEndIf node, Runtime runtime)
        {
            return OK;
        }

        public static object? Interpret(this Parser.AstGoto node, Runtime runtime)
        {
            if (node.line != -1)
            {
                if (runtime.lineNumbers.ContainsKey(node.line))
                    runtime.instructionPointer = runtime.lineNumbers[node.line];
                else
                {
                    runtime.instructionPointer = runtime.program.Count + 1;
                    return null;
                }
            }
            else if (node.label != "")
            {
                if (runtime.lineLabels.ContainsKey(node.label))
                    runtime.instructionPointer = runtime.lineLabels[node.label];
                else
                {
                    runtime.instructionPointer = runtime.program.Count + 1;
                    return null;
                }
            }
            return OK;
        }

        public static object? Interpret(this Parser.AstPrint node, Runtime runtime)
        {
            if (node.exps == null || node.exps.exps == null || node.exps.exps.Count == 0)
            {
                if (node.emitCrlf)
                    Console.WriteLine("");
                else
                    Console.Write("");
            }
            else
            {
                foreach (AstExpression exp in node.exps.exps)
                    Console.Write(exp.Interpret(runtime));

                if (node.emitCrlf)
                    Console.WriteLine("");
            }
            return OK;
        }

        public static object? Interpret(this Parser.AstPrintList node, Runtime runtime)
        {
            if (node.exps == null || node.exps.Count == 0)
                return "";
            else
            {
                StringBuilder sb = new();
                foreach (Parser.AstExpression exp in node.exps)
                {
                    sb.Append(exp.Interpret(runtime));
                }
                return sb.ToString();
            }
        }

        public static object? Interpret(this Parser.AstInput node, Runtime runtime)
        {
            if (node.ids == null || node.ids.ids == null || node.ids.ids.Count == 0)
                return "";

            string? inputLine = null;

            while (inputLine == null)
            {
                if (node.prompt.Length > 0)
                    Console.Write(node.prompt);

                inputLine = Console.ReadLine();
                if (inputLine != null)
                {
                    if (node.ids.ids.Count == 1)
                    {
                        SetInputVariable(node.ids.ids[0].Name, inputLine, runtime);
                    }
                    else
                    {
                        string[] parts = inputLine.Split(',', node.ids.ids.Count, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        if (parts.Length != node.ids.ids.Count)
                        {
                            Console.WriteLine($"Expected {node.ids.ids.Count} arguments but got {parts.Length}.");
                            inputLine = "";
                        }
                        else
                        {
                            for (int i = 0; i < node.ids.ids.Count; i++)
                            {
                                SetInputVariable(node.ids.ids[i].Name, parts[i], runtime);
                            }
                        }
                    }
                }
            }
            return OK;
        }

        private static void SetInputVariable(string ID, string value, Runtime runtime)
        {
            bool preferString = ID.EndsWith("$");
            bool preferFloat = ID.EndsWith("#");
            bool preferInt = ID.EndsWith("%");
            bool preferBool = false;

            object? variable = null;
            if (runtime.symbolTable.Contains(ID))
                variable = runtime.symbolTable.Get(ID);
            if (variable != null)
            {
                if (variable.GetType() == typeof(AstString) || variable.GetType() == typeof(string))
                    preferString = true;
                else if (variable.GetType() == typeof(AstFloat) || variable.GetType() == typeof(float) || variable.GetType() == typeof(double))
                    preferFloat = true;
                else if (variable.GetType() == typeof(AstInteger) || variable.GetType() == typeof(int) || variable.GetType() == typeof(Int32) || variable.GetType() == typeof(Int64) || variable.GetType() == typeof(long))
                    preferInt = true;
                else if (variable.GetType() == typeof(AstBoolean) || variable.GetType() == typeof(bool) || variable.GetType() == typeof(Boolean))
                    preferBool = true;
            }

            // preference and it parses correctly.
            bool floatSuccess = Double.TryParse(value, out double floatValue);
            if (preferFloat && floatSuccess)
            {
                runtime.symbolTable.Set(ID, floatValue);
                return;
            }

            bool intSuccess = Int32.TryParse(value, out int intValue);
            if (preferInt && intSuccess)
            {
                runtime.symbolTable.Set(ID, intValue);
                return;
            }

            bool boolSuccess = Boolean.TryParse(value, out bool boolValue);
            if (preferBool && boolSuccess)
            {
                runtime.symbolTable.Set(ID, boolValue);
                return;
            }

            if (preferString)
            {
                runtime.symbolTable.Set(ID, value);
                return;
            }

            // no preference take hardest parsed correctly first
            if (floatSuccess && !Double.IsInteger(floatValue))
            {
                runtime.symbolTable.Set(ID, floatValue);
                return;
            }
            else if (intSuccess)
            {
                runtime.symbolTable.Set(ID, intValue);
                return;
            }
            else if (boolSuccess)
            {
                runtime.symbolTable.Set(ID, boolValue);
                return;
            }

            runtime.symbolTable.Set(ID, value);
        }

        public static object? Interpret(this Parser.AstEnd node, Runtime runtime)
        {
            runtime.running = false;
            return OK;
        }

        public static object? Interpret(this Parser.AstGosub node, Runtime runtime)
        {
            if (node.line != -1)
            {
                if (runtime.lineNumbers.ContainsKey(node.line))
                {
                    runtime.callStack.Push(runtime.instructionPointer);
                    runtime.instructionPointer = runtime.lineNumbers[node.line];
                }
                else
                {
                    runtime.instructionPointer = runtime.program.Count + 1;
                    return null;
                }
            }
            else if (node.label != "")
            {
                if (runtime.lineLabels.ContainsKey(node.label))
                {
                    runtime.callStack.Push(runtime.instructionPointer);
                    runtime.instructionPointer = runtime.lineLabels[node.label];
                }
                else
                {
                    runtime.instructionPointer = runtime.program.Count + 1;
                    return null;
                }
            }
            return OK;
        }

        public static object? Interpret(this Parser.AstReturn node, Runtime runtime)
        {
            if (runtime.callStack.Count == 0)
                throw new Exception("Call stack is empty");
            runtime.instructionPointer = runtime.callStack.Pop() + 1;
            return OK;
        }

        public static object? Interpret(this Parser.AstArrayAccess node, Runtime runtime)
        {
            if (!runtime.symbolTable.Contains(node.variable))
                throw new Exception($"Variable \"{node.variable}\" not found.");
            object? arr = runtime.symbolTable.Get(node.variable);
            if (arr != null && node.exps != null && node.exps.expList != null)
            {
                int rank = node.exps.expList.Count;
                if (rank > 4)
                    throw new Exception($"Too many indexes to array ${node.variable}");

                int[] expResult = (from exp in node.exps.expList
                                   select Convert.ToInt32(exp.Interpret(runtime))).ToArray<int>();
                if (rank == 1)
                    return (arr as object?[])[expResult[0]];
                else if (rank == 2)
                    return (arr as object?[,])[expResult[0], expResult[1]];
                else if (rank == 3)
                    return (arr as object?[,,])[expResult[0], expResult[1], expResult[2]];
                else if (rank == 4)
                    return (arr as object?[,,,])[expResult[0], expResult[1], expResult[2], expResult[3]];
                else
                    throw new Exception($"Too many indexes to array ${node.variable}");
            }
            return null;
        }

        public static object? Interpret(this Parser.AstDim node, Runtime runtime)
        {
            if (node.name == null)
                return null;

            //if (runtime.symbolTable.Contains(node.name))
            //  throw new Exception($"Variable \"{node.name}\" has already been declared.");

            if (node.rank != null && node.rank.expList != null)
            {
                int rank = node.rank.expList.Count;
                if (rank > 4)
                    throw new Exception($"Too many indexes to array \"{node.name}\"");

                int[] expResult = (from exp in node.rank.expList
                                   select Convert.ToInt32(exp.Interpret(runtime))).ToArray<int>();

                if (rank == 1)
                    runtime.symbolTable.Set(node.name, new object?[expResult[0]]);
                else if (rank == 2)
                    runtime.symbolTable.Set(node.name, new object?[expResult[0], expResult[1]]);
                else if (rank == 3)
                    runtime.symbolTable.Set(node.name, new object?[expResult[0], expResult[1], expResult[2]]);
                else if (rank == 4)
                    runtime.symbolTable.Set(node.name, new object?[expResult[0], expResult[1], expResult[2], expResult[3]]);
                else
                    throw new Exception($"Too many indexes to array\"${node.name}\"");
            }
            else
            {
                throw new Exception($"Missing array indexes for variable \"{node.name}\"");
            }
            return null;
        }

        public static object? Interpret(this Parser.AstData node, Runtime runtime)
        {
            return null;
        }
    }
}
