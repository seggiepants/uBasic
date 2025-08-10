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
                            Tuple<int, Parser.AstStatement?> stmt = Basic.ParseStatement(tokens, i);
                            if (stmt.Item2 != null)
                            {
                                Console.WriteLine(stmt.Item2.Interpret(runtime));
                                i = stmt.Item1 - 1;
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