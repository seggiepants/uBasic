using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace uBasic
{
    // Func<Stack<int>, int> fn
    public class FunctionTable
    {
        static Dictionary<string, Func<Stack<object?>, object?>> fnTable = new();
        static Random r;

        public FunctionTable()
        {
            Clear();
            r = new Random();
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
            fnTable.Add("RND", RND);
            fnTable.Add("INT", INT);
            fnTable.Add("CHDIR", CHDIR);
            fnTable.Add("FILES", FILES);
            fnTable.Add("KILL", KILL);
            fnTable.Add("MKDIR", MKDIR);
            fnTable.Add("RMDIR", RMDIR);
            fnTable.Add("NAME", NAME);
            fnTable.Add("CURDIR", CURDIR);
            fnTable.Add("TAB", TAB);
            fnTable.Add("TAB$", TAB); 
            fnTable.Add("CURSOR_LEFT", CURSOR_LEFT);
            fnTable.Add("CURSOR_TOP", CURSOR_TOP);
            fnTable.Add("CONSOLE_WIDTH", CONSOLE_WIDTH);
            fnTable.Add("CONSOLE_HEIGHT", CONSOLE_HEIGHT);
            fnTable.Add("COLOR", COLOR);
            fnTable.Add("RESET_COLOR", RESET_COLOR);
            fnTable.Add("LOCATE", LOCATE);
            fnTable.Add("CLS", CLS);
            fnTable.Add("ASC", ASC);
            fnTable.Add("BEEP", BEEP);
            fnTable.Add("SOUND", SOUND);
        }

        // To be implemented:
        // File Copy?
        // INSTR
        // INSTRREV
        // STRING/STRING$
        // REPLACE/REPLACE$
        // STRREVERSE/STRREVERSE$
        // ASC
        // CHR/CHR$
        // SQR -- Square Root
        // FIX -- Convert to Integer dumping the decimal part.
        // ATN -- Arc Tangent
        // ATAN2 -- Arc Tangent -- Nicer
        // MOD -> This needs to be at root level with +-*/
        // SGN -- 1 = Positive, 0 = Zero, -1 = Negative
        // EXP -- e ^ X
        // LOG -- natural logarithm
        // BEEP
        // SOUND
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

        public static object? ASC(Stack<object?> stack)
        {
            bool success;
            if (stack.Count > 0)
            {
                string? operand = GetOperand<string>(stack, out success);
                if (operand != null && success)
                {
                    stack.Pop();
                    return (int)operand[0];
                }
                return 0;
            }
            else
            {
                throw new Exception("Parameter Error ASC requires a string to operate on.");
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

        private static object? RND(Stack<object?> stack)
        {
            object? throwAway;

            if (stack.Count > 0)
            {
                throwAway = stack.Pop();
                if (throwAway != null && (throwAway.GetType() == typeof(int) || throwAway.GetType() == typeof(Int32) || throwAway.GetType() == typeof(Int64)))
                {
                    int value = (int)throwAway;
                    if (value == 0)
                        r = new Random();
                    else if (value < 0)
                        r = new Random(Math.Abs(value));
                }
            }
            return r.NextDouble();            
        }

        private static object? INT(Stack<object?> stack)
        {
            bool success;
            object? operand;

            operand = GetOperand<double>(stack, out success);
            if (operand != null && success)
            {
                stack.Pop();
                try
                {
                    int ret = (int) Math.Floor(Convert.ToDouble(operand));
                    return ret;
                }
                catch { };
            }
            return 0;
        }

        // Console operations
        private static object? TAB(Stack<object?> stack)
        {
            // TAB(TabStop)
            bool success;
            object? operand;

            operand = GetOperand<int>(stack, out success);
            if (operand != null && success)
            {
                stack.Pop();
                try
                {
                    int ret = Convert.ToInt32(operand);
                    int left = Console.CursorLeft;
                    if ((int)operand - left <= 0)
                        return "";
                    else
                        return "".PadRight((int) operand - left, ' ');
                }
                catch { }                
            }
            return "";
        }

        public static object? CLS(Stack<object?> stack)
        {
            Console.Clear();
            return null;
        }

        private static object? CURSOR_LEFT(Stack<object?> stack)
        {
            return Console.CursorLeft;
        }

        private static object? CURSOR_TOP(Stack<object?> stack)
        {
            return Console.CursorTop;
        }

        private static object? CONSOLE_WIDTH(Stack<object?> stack)
        {
            return Console.WindowWidth;
        }

        private static object? CONSOLE_HEIGHT(Stack<object?> stack)
        {
            return Console.WindowHeight;
        }

        private static object? COLOR(Stack<object?> stack)
        {
            // COLOR fg, [bg]
            int? fg = null;
            int? bg= null;
            if (stack.Count >= 1 && stack.Peek().GetType() == typeof(int))
            {
                fg = (int?)stack.Pop();
            }

            if (stack.Count >= 1 && stack.Peek().GetType() == typeof(int))
            {
                bg = (int?)stack.Pop();
            }
            
            if (fg != null)
            {
                if (fg >= 0 && fg <= 15)
                    Console.ForegroundColor = (ConsoleColor)((int)fg);

                if (bg >= 0 && bg <= 15)
                    Console.BackgroundColor = (ConsoleColor)((int)bg);
            }
            return null;
        }

        private static object? RESET_COLOR(Stack<object?> stack)
        {
            Console.ResetColor();
            return null;
        }

        private static object? LOCATE(Stack<object?> stack)
        {
            // COLOR fg, [bg]
            int? x = null;
            int? y = null;
            if (stack.Count >= 2 && stack.Peek().GetType() == typeof(int))
            {
                x = (int?)stack.Pop();
                y = (int?)stack.Pop();
            }

            if (x != null && y != null)
            {
                Console.SetCursorPosition((int)x, (int)y);
            }
            else
                throw new Exception("Invalid arguments expected x, y");

            return null;
        }


        // All of the file handling functions are going to be functions now 
        // This requires them to call with parenthesis but it frees up the lexer and parser sooo much.
        private static object? CHDIR(Stack<object?> stack)
        {
            // CHDIR pathname$
            string? pathName = null;
            if (stack.Count > 0 && stack.Peek().GetType() == typeof(string))
                pathName = (string?)stack.Pop();
            if (pathName == null)
            {
                throw new Exception("No operand of the correct type supplied.");
            }

            if (pathName != null && Directory.Exists(pathName))
                Directory.SetCurrentDirectory(pathName);
            return Directory.GetCurrentDirectory();
        }

        private static object? CURDIR(Stack<object?> stack)
        {
            // CHDIR pathname$
            return Directory.GetCurrentDirectory();
        }

        private static object? FILES(Stack<object?> stack)
        {
            // FILES fileSpec$
            // ZZZ - I kind of want to return a string array instead of print to the console.
            string? fileSpec = null;
            string path = "", pattern = "";
            if (stack.Count > 0 && stack.Peek().GetType() == typeof(string))
                fileSpec = (string?)stack.Pop();
            if (fileSpec == null)
            {
                fileSpec = "*.*";
            }

            if (fileSpec != null)
            {
                int index = fileSpec.LastIndexOf(Path.DirectorySeparatorChar);
                if (index == -1)
                {
                    path = ".";
                    pattern = fileSpec;
                }
                else
                {
                    path = fileSpec.Substring(0, index);
                    pattern = fileSpec.Substring(index + 1);
                }
            }
            return string.Join('\n', Directory.GetFiles(path, pattern));
        }

        private static object? KILL(Stack<object?> stack)
        {
            // KILL fileSpec$ -- may include *, and ? wildcards.
            string? fileSpec = null;
            if (stack.Count > 0 && stack.Peek().GetType() == typeof(string))
                fileSpec = (string?)stack.Pop();
            if (fileSpec == null)
            {
                fileSpec = "*.*";
            }

            if (fileSpec != null)
            {
                string path, pattern;
                int index = fileSpec.LastIndexOf(Path.DirectorySeparatorChar);
                if (index == -1)
                {
                    path = ".";
                    pattern = fileSpec;
                }
                else
                {
                    path = fileSpec.Substring(0, index);
                    pattern = fileSpec.Substring(index + 1);
                }
                foreach (string file in Directory.GetFiles(path, pattern))
                {
                    File.Delete(file);
                }
            }
            return null;
        }

        private static object? MKDIR(Stack<object?> stack)
        {
            // MKDIR pathname$
            string? pathName = null;
            if (stack.Count > 0 && stack.Peek().GetType() == typeof(string))
                pathName = (string?)stack.Pop();
            if (pathName == null)
            {
                throw new Exception("No operand of the correct type supplied.");
            }

            if (pathName != null && Directory.Exists(pathName))
            {
                if (Directory.Exists(pathName))
                    throw new Exception("Directory already exists");

                Directory.CreateDirectory(pathName);
            }
            return pathName;
        }

        private static object? NAME(Stack<object?> stack)
        {
            // NAME oldspec$ AS newspec$ -- may include path so this works as move too.
            string? source = null;
            string? target = null;
            if (stack.Count >= 2 && stack.Peek().GetType() == typeof(string))
            {
                source = (string?)stack.Pop();
                target = (string?)stack.Pop();
            }

            if (source == null)
            {
                throw new Exception("No source file of the correct type supplied.");
            }
            if (target == null)
            {
                throw new Exception("No target file of the correct type supplied.");
            }

            if (!File.Exists(source))
                throw new Exception("Source file not found.");

            if (File.Exists(target))
                throw new Exception("Target file already exists.");

            File.Move(source, target);

            return target;
        }

        private static object? RMDIR(Stack<object?> stack)
        {
            // RMDIR pathname$
            string? pathName = null;
            if (stack.Count > 0 && stack.Peek().GetType() == typeof(string))
                pathName = (string?)stack.Pop();
            if (pathName == null)
            {
                throw new Exception("No operand of the correct type supplied.");
            }

            if (pathName != null && Directory.Exists(pathName))
            {
                DirectoryInfo di = new(pathName);
                if (di.GetFiles().Length + di.GetDirectories().Length > 0)
                    throw new Exception("Directory not empty");

                Directory.Delete(pathName);
            }
            return null;
        }

        private static object? BEEP(Stack<object?> stack)
        {
            // BEEP
            Console.Beep();
            return null;
        }

        private static object? SOUND(Stack<object?> stack)
        {
            // RMDIR pathname$
            int? frequency = null;
            int? duration = null;
            Type[] allowed =
            {
                typeof(int), typeof(Int32), typeof(Int64), typeof(long)
            };

            if (stack.Count >= 2 && allowed.Contains(stack.Peek().GetType()))
            {
                frequency = (int?)stack.Pop();
            }
            if (stack.Count >= 1 && allowed.Contains(stack.Peek().GetType()))
            {
                duration = (int?)stack.Pop();
            }
            if (frequency != null && duration != null)
                Console.Beep((int)frequency, (int)duration);
            return null;
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
