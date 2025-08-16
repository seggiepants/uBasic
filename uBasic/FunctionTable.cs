using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace uBasic
{
    // Func<Stack<int>, int> fn
    public class FunctionTable
    {
        static Dictionary<string, Func<Stack<object?>, object?>> fnTable = new();

        public FunctionTable()
        {
            Clear();
        }

        public object? Call(string name, Parser.AstExpressionList? args, Runtime runtime)
        {
            bool hasFunction = fnTable.TryGetValue(name.ToUpperInvariant(), out Func<Stack<object?>, object?>? fn);
            if (hasFunction && fn != null)
            {
                if (args != null && args.expList != null)
                {
                    List<Parser.AstExpression> argsRev = new List<Parser.AstExpression>();
                    argsRev.AddRange(args.expList);
                    argsRev.Reverse();
                    foreach(Parser.AstExpression exp in argsRev)
                    {
                        runtime.stack.Push(exp.Interpret(runtime));
                    }
                }
                return fn(runtime.stack);
            }
            else
            {
                throw new Exception($"Function not found: \"{name}\"");
            }            
        }

        public void Clear()
        {
            fnTable.Clear();
            fnTable.Add("LEN", LEN);
            fnTable.Add("STR", STR);
            fnTable.Add("STR$", STR);
            fnTable.Add("MID", MID);
            fnTable.Add("MID$", MID);
            fnTable.Add("LEFT", LEFT);
            fnTable.Add("LEFT$", LEFT);
            fnTable.Add("RIGHT", RIGHT);
            fnTable.Add("RIGHT$", RIGHT);
            fnTable.Add("UCASE", UCASE);
            fnTable.Add("UCASE$", UCASE);
            fnTable.Add("LCASE", LCASE);
            fnTable.Add("LCASE$", LCASE);
            fnTable.Add("SPACE", SPACE);
            fnTable.Add("SPACE$", SPACE);
            fnTable.Add("LTRIM", LTRIM);
            fnTable.Add("LTRIM$", LTRIM);
            fnTable.Add("RTRIM", RTRIM);
            fnTable.Add("RTRIM$", RTRIM);
            fnTable.Add("TRIM", TRIM);
            fnTable.Add("TRIM$", TRIM);
            fnTable.Add("SIN", SIN);
            fnTable.Add("COS", COS);
            fnTable.Add("TAN", TAN);
            fnTable.Add("ABS", ABS);
        }

        // To be implemented:
        // INSTR
        // INSTRREV
        // STRING/STRING$
        // REPLACE/REPLACE$
        // STRREVERSE/STRREVERSE$
        // ASC
        // CHR/CHR$
        // SQR -- Square Root
        // INT -- Convert to Integer
        // FIX -- Convert to Integer dumping the decimal part.
        // ABS -- Absolute Value
        // RND -- Random Number Generator.
        // ATN -- Arc Tangent
        // ATAN2 -- Arc Tangent -- Nicer
        // MOD -> This needs to be at root level with +-*/
        // SGN -- 1 = Positive, 0 = Zero, -1 = Negative
        // EXP -- e ^ X
        // LOG -- natural logarithm
        // INPUT -> Move to parser level non-standard
        // PRINT -> Move to parser level non-standard
        // RND -- Random number generator.
        // -- MORE TO COME --

        public static object? LEN(Stack<object?> stack)
        {
            bool success;
            if (stack.Count > 0)
            {
                string? operand = GetOperand<string>(stack, out success);
                if (operand != null && success)
                { 
                    stack.Pop();
                    return operand.Length;
                }
                // ZZZ add arrays
                // ZZZ should be able to get the length of other dimensions in an array by passing the axis to work on 0 - x, 1 - y, 2 - z, etc.
                return 0;
            }
            else
            {
                throw new Exception("Parameter Error LEN requires a string or array to operate on.");
            }
        }

        public static object? STR(Stack<object?> stack)
        {
            bool success;
            object? text;

            success = stack.TryPop(out text);
            if (text == null || !success)
                throw new Exception("STR - Invalid operands.");

            if (text.GetType() == typeof(string))
                return text;
            else
                return text.ToString();
        }

        public static object? LTRIM(Stack<object?> stack)
        {

            bool success;
            string? text;

            text = GetOperand<string>(stack, out success);
            if (text != null && success)
                stack.Pop();
            else
                throw new Exception("LTRIM - Invalid operands.");

            return text.TrimStart();

        }

        public static object? RTRIM(Stack<object?> stack)
        {

            bool success;
            string? text;

            text = GetOperand<string>(stack, out success);
            if (text != null && success)
                stack.Pop();
            else
                throw new Exception("RTRIM - Invalid operands.");

            return text.TrimEnd();
        }

        public static object? TRIM(Stack<object?> stack)
        {

            bool success;
            string? text;

            text = GetOperand<string>(stack, out success);
            if (text != null && success)
                stack.Pop();
            else
                throw new Exception("TRIM - Invalid operands.");

            return text.Trim();
        }

        public static object? MID(Stack<object?> stack)
        {
            bool success;
            int? charCount = -1;
            int? charIndex = 0;
            string? text;

            text = GetOperand<string>(stack, out success);
            if (text != null && success)
                stack.Pop();
            else
                throw new Exception("MID - Invalid operands.");

            charIndex = GetOperand<int>(stack, out success);
            if (charIndex != null && success)
                stack.Pop();
            else
                throw new Exception("MID - Invalid operands.");

            charCount = GetOperand<int>(stack, out success);
            if (charCount != null && success)
                stack.Pop();

            if (text != null && charIndex != null)
            {
                if (charCount == null)
                    return text.Substring((int)charIndex - 1);
                return text.Substring((int)charIndex - 1, (int)charCount);
            }
            else
            {
                return "";
            }
        }

        public static object? LEFT(Stack<object?> stack)
        {
            bool success;
            int? charCount = 0;
            string? text;

            text = GetOperand<string>(stack, out success);
            if (text != null && success)
                stack.Pop();
            else
                throw new Exception("LEFT - Invalid operands.");

            charCount = GetOperand<int>(stack, out success);
            if (charCount != null && success)
                stack.Pop();
            else
                throw new Exception("LEFT - Invalid operands.");

            if (text != null && charCount != null)
            {
                return text.Substring(0, (int)charCount);
            }
            else
            {
                return "";
            }
        }

        public static object? RIGHT(Stack<object?> stack)
        {
            bool success;
            int? charCount = 0;
            string? text;

            text = GetOperand<string>(stack, out success);
            if (text != null && success)
                stack.Pop();
            else
                throw new Exception("RIGHT - Invalid operands.");

            charCount = GetOperand<int>(stack, out success);
            if (charCount != null && success)
                stack.Pop();
            else
                throw new Exception("RIGHT - Invalid operands.");

            if (text != null && charCount != null)
            {
                if (charCount >= text.Length)
                    return text;

                return text.Substring(text.Length - (int)charCount);
            }
            else
            {
                return "";
            }
        }

        public static object? UCASE(Stack<object?> stack)
        {
            bool success;
            string? text;

            text = GetOperand<string>(stack, out success);
            if (text != null && success)
                stack.Pop();
            else
                throw new Exception("UCASE - Invalid operands.");

            if (text != null)
            {
                return text.ToUpperInvariant();
            }
            else
            {
                return "";
            }
        }

        public static object? LCASE(Stack<object?> stack)
        {
            bool success;
            string? text;

            text = GetOperand<string>(stack, out success);
            if (text != null && success)
                stack.Pop();
            else
                throw new Exception("LCASE - Invalid operands.");

            if (text != null)
            {
                return text.ToLowerInvariant();
            }
            else
            {
                return "";
            }
        }

        public static object? SIN(Stack<object?> stack)
        {
            bool success;
            double? operand;

            operand = GetOperand<double>(stack, out success);
            if (operand != null && success)
                stack.Pop();
            else
            {
                int? secondChance = GetOperand<int?>(stack, out success);
                if (secondChance != null && success)
                {
                    stack.Pop();
                    operand = Convert.ToDouble(secondChance);
                }
                else
                    throw new Exception("SIN - Invalid operand.");
            }

            if (operand != null)
            {
                return Math.Sin((double)operand);
            }
            else
            {
                return 0.0;
            }
        }

        public static object? COS(Stack<object?> stack)
        {
            bool success;
            double? operand;

            operand = GetOperand<double>(stack, out success);
            if (operand != null && success)
                stack.Pop();
            else
            {
                int? secondChance = GetOperand<int?>(stack, out success);
                if (secondChance != null && success)
                {
                    stack.Pop();
                    operand = Convert.ToDouble(secondChance);
                }
                else
                    throw new Exception("COS - Invalid operand.");
            }

            if (operand != null)
            {
                return Math.Cos((double)operand);
            }
            else
            {
                return 0.0;
            }
        }

        public static object? TAN(Stack<object?> stack)
        {
            bool success;
            double? operand;

            operand = GetOperand<double>(stack, out success);
            if (operand != null && success)
                stack.Pop();
            else
            {
                int? secondChance = GetOperand<int?>(stack, out success);
                if (secondChance != null && success)
                {
                    stack.Pop();
                    operand = Convert.ToDouble(secondChance);
                }
                else
                    throw new Exception("TAN - Invalid operand.");
            }

            if (operand != null)
            {
                return Math.Tan((double)operand);
            }
            else
            {
                return 0.0;
            }
        }

        public static object? ABS(Stack<object?> stack)
        {
            bool success;
            double? operand;

            operand = GetOperand<double>(stack, out success);
            if (operand != null && success)
            { 
                stack.Pop();
                return Math.Abs((double)operand);
            }
            else
            {
                int? secondChance = GetOperand<int>(stack, out success);
                if (secondChance != null && success)
                {
                    stack.Pop();
                    return Math.Abs((int)secondChance);
                }
                else
                    throw new Exception("ABS - Invalid operand.");
            }
        }

        public static object? SPACE(Stack<object?> stack)
        {
            bool success;
            int? operand;

            operand = GetOperand<int>(stack, out success);
            if (operand != null && success)
            {
                stack.Pop();
                return "".PadRight((int)operand, ' ');
            }
            else
            {
                double? secondChance = GetOperand<double>(stack, out success);
                if (secondChance != null && success)
                {
                    stack.Pop();
                    return "".PadRight(Convert.ToInt32(secondChance), ' ');
                }
                else
                    throw new Exception("SPACE - Invalid operand.");
            }
        }
        private static T? GetOperand<T>(Stack<object?> stack, out bool success)
        {
            object? value;
            success = false;
            if (stack.Count > 0)
            {
                value = stack.Peek();
                if (value != null)
                {
                    if (value.GetType() == typeof(T))
                    {
                        success = true;
                        return (T)value;
                    }
                }
            }
            return default;
        }
    }
}
