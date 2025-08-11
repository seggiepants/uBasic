namespace uBasic
{
    public class uBasic
    {
        public static void Main(String[] args)
        {

            
            bool EOF = false;
            Lexer lex = new();
            Runtime runtime = new Runtime();
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
                            Tuple<int, Parser.AstLines?> lines = Basic.ParseLines(tokens, i);
                            if (lines.Item2 != null && lines.Item2.lines != null)
                            {
                                foreach (Parser.AstLine line in lines.Item2.lines)
                                {
                                    if (line.line != null && line.statements != null)
                                        runtime.program.Add((int)line.line, line.statements);
                                    else
                                        Console.WriteLine(line.Interpret(runtime));
                                }
                                i = lines.Item1 - 1;
                            }
                            else
                            {
                                Tuple<int, Parser.AstExpression?> node = Basic.ParseExpression(tokens, i);
                                if (node.Item2 != null)
                                {
                                    //Console.WriteLine(node.Item2);
                                    Console.WriteLine(node.Item2.Interpret(runtime));
                                    i = node.Item1 - 1; // for loop adds the 1 back on.
                                }
                                else
                                {
                                    Tuple<int, Parser.AstToken?> parsedToken = Basic.ParseToken(tokens, i);
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