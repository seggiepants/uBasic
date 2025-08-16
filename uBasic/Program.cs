using System.ComponentModel.Design;
using System.Reflection.Emit;

namespace uBasic
{
    public class uBasic
    {
        public static void Main(String[] args)
        {

            
            bool EOF = false;
            Lexer lex = new();
            Runtime runtime;
            Runtime program = new Runtime();
            Runtime repl = new Runtime();
            repl.symbolTable = program.symbolTable;
            Basic basic = new Basic();

            while (!EOF)
            {
                Console.Write("> ");
                string? instruction = Console.ReadLine();
                if (instruction == null)
                    EOF = true;
                else
                {
                    try
                    {
                        List<Token> tokens = new();
                        foreach(Token t in lex.Tokenize(instruction))
                        {
                            //Console.WriteLine($"Type: {t.Type}, Value: \"{t.Text}\" :: line: {t.LineNumber} column: {t.ColumnNumber}");
                            tokens.Add(t);
                        }
                        if (tokens.Count > 0)
                        {
                            if (tokens[0].Type == Token_Type.TOKEN_INTEGER ||
                                tokens[0].Type == Token_Type.TOKEN_LOAD ||
                                tokens[0].Type == Token_Type.TOKEN_LIST ||
                                tokens[0].Type == Token_Type.TOKEN_RUN)
                            {
                                runtime = program;
                            }
                            else
                            {
                                runtime = repl;
                                repl.program.Clear();
                                repl.stack.Clear();
                            }
                        }
                        else
                        {
                            runtime = repl;
                            repl.program.Clear();
                            repl.stack.Clear();
                        }
                        for (int i = 0; i < tokens.Count; i++)
                        {

                            int lineNum = runtime.program.Count;
                            Tuple<int, Parser.AstLine?> line = Basic.ParseLine(tokens, i, runtime);
                            if (line.Item2 != null)
                            {
                                if (line.Item2.statements != null && line.Item2.statements.statements != null)
                                {
                                    if (line.Item2.line != null)
                                        runtime.lineNumbers.Add((int)line.Item2.line, lineNum);
                                    //else
                                        //Console.WriteLine(line.Item2.Interpret(runtime));
                                        //Console.WriteLine(runtime.Run());
                                }
                                //else
                                    //Console.WriteLine(line.Item2.Interpret(runtime));
                                i = line.Item1 - 1;
                            }
                            else
                            {
                                Tuple<int, Parser.AstExpression?> node = Basic.ParseExpression(tokens, i, runtime);
                                if (node.Item2 != null)
                                {
                                    //Console.WriteLine(node.Item2);
                                    Console.WriteLine(node.Item2.Interpret(runtime));
                                    i = node.Item1 - 1; // for loop adds the 1 back on.
                                }
                                else if (tokens[i].Type == Token_Type.TOKEN_LOAD)
                                {
                                    i++; // Eat the string.
                                }
                                else
                                { 
                                    Tuple<int, Parser.AstToken?> parsedToken = Basic.ParseToken(tokens, i, runtime);
                                    object? result = null;
                                    if (parsedToken.Item2 != null)
                                    {
                                        result = parsedToken.Item2.Interpret(runtime);
                                        if (result != null)
                                            Console.WriteLine(result);
                                    }
                                    if (parsedToken == null || result == null)
                                        Console.WriteLine($"Unrecognized token/syntax error at Line: {tokens[i].LineNumber}, Column: {tokens[i].ColumnNumber} :: {tokens[i].Text}");
                                }
                            }
                        }

                        while (Basic.labels.Count > 0)
                        {
                            if (runtime.lineLabels.ContainsKey(Basic.labels.Peek()))
                                Basic.labels.Pop();
                            else
                                runtime.lineLabels.Add(Basic.labels.Pop(), runtime.program.Count);
                        }


                        if (tokens != null && tokens.Count > 0 && runtime.program.Count > 0 && runtime == repl)
                        {
                            runtime.Run();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }
        }
    }
}