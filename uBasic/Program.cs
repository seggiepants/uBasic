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
                        Basic basic = new Basic();
                        for (int i = 0; i < tokens.Count; i++)
                        {
                            if (tokens[i].Type == Token_Type.TOKEN_INTEGER || tokens[i].Type == Token_Type.TOKEN_LIST || tokens[i].Type == Token_Type.TOKEN_RUN)
                                runtime = program;
                            else
                            {
                                runtime = repl;
                                repl.program.Clear();
                                repl.stack.Clear();
                            }

                            int lineNum = runtime.program.Count;
                            Tuple<int, Parser.AstLine?> line = Basic.ParseLine(tokens, i, runtime);
                            if (line.Item2 != null && line.Item2 != null)
                            {
                                if (line.Item2.statements != null && line.Item2.statements.statements != null)
                                {
                                    if (line.Item2.line != null)
                                        runtime.lineNumbers.Add((int)line.Item2.line, lineNum);
                                    else
                                        //Console.WriteLine(line.Item2.Interpret(runtime));
                                        Console.WriteLine(runtime.Run());
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
                                        Console.WriteLine($"Unrecognized token/syntax error at Line: {tokens[i].LineNumber}, Column: {tokens[i].LineNumber} :: {tokens[i].Text}");
                                }
                            }
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