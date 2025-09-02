using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace uBasic
{
    public enum Token_Type
    {
        TOKEN_NONE = 0,
        TOKEN_FLOAT,
        TOKEN_INTEGER,
        TOKEN_TRUE,
        TOKEN_FALSE,
        TOKEN_COMMA,
        TOKEN_IS_EQUAL,
        TOKEN_NOT_EQUAL,
        TOKEN_SET_EQUAL,
        TOKEN_LESS_EQUAL,
        TOKEN_GREATER_EQUAL,
        TOKEN_LESS_THAN,
        TOKEN_GREATER_THAN,
        TOKEN_IDENTIFIER,
        TOKEN_LPAREN,
        TOKEN_RPAREN,
        TOKEN_LBRACKET,
        TOKEN_RBRACKET,
        TOKEN_ADD,
        TOKEN_SUBTRACT,
        TOKEN_MULTIPLY,
        TOKEN_DIVIDE,
        TOKEN_POWER,
        TOKEN_NOT,
        TOKEN_OR,
        TOKEN_AND,
        TOKEN_FOR,
        TOKEN_TO,
        TOKEN_STEP,
        TOKEN_NEXT,
        TOKEN_IF,
        TOKEN_THEN,
        TOKEN_ELSEIF,
        TOKEN_ELSE,
        TOKEN_END,
        TOKEN_DIM,
        TOKEN_LET,
        TOKEN_GOTO,
        TOKEN_GOSUB,
        TOKEN_LIST,
        TOKEN_LOAD,
        TOKEN_RETURN,
        TOKEN_RUN,
        TOKEN_INPUT,
        TOKEN_STRING,
        TOKEN_COMMENT,
        TOKEN_COLON,
        TOKEN_SEMICOLON,
        TOKEN_DATA,
        TOKEN_READ,
        TOKEN_RESTORE,
        TOKEN_PRINT,
        TOKEN_NEWLINE,
        TOKEN_OPEN,
        TOKEN_CLOSE,
        TOKEN_OUTPUT,
        TOKEN_FREEFILE,
        TOKEN_FILENUM,
        TOKEN_AS,
        TOKEN_APPEND,
        TOKEN_WHITE_SPACE,
    }
    public class Token
    {
        public int LineNumber { get; set; } = 0;
        public int ColumnNumber { get; set; } = 0;
        public Token_Type Type { get; set; } = Token_Type.TOKEN_NONE;
        public string Text { get; set; } = "";
    }
    public class Lexer
    {
        List<Tuple<Token_Type, Regex>> matches = new List<Tuple<Token_Type, Regex>>()
        {
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_NEWLINE, new Regex(@"^( |\t)*(\r|\n)+")),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_WHITE_SPACE, new Regex(@"^( |\t)+")),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_LET, new Regex(@"^let(?=( |\t|\r|\n))", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_GOTO, new Regex(@"^goto(?=( |\t))", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_LIST, new Regex(@"^list(?=( |\t|\r|\n|\z))", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_LOAD, new Regex(@"^load(?=( |\t|\r|\n|\z))", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_FOR, new Regex(@"^for(?=( |\t|\r|\n|\z))", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_TO, new Regex(@"^to(?=( |\t|\r|\n|\z))", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_NEXT, new Regex(@"^next(?=( |\t|\r|\n|\z))", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_STEP, new Regex(@"^step(?=( |\t|\r|\n|\z))", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_IF, new Regex(@"^if(?=( |\t|\r|\n|\z))", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_THEN, new Regex(@"^then(?=( |\t|\r|\n|\z))", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_ELSEIF, new Regex(@"^elseif(?=( |\t|\r|\n|\z))", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_ELSE, new Regex(@"^else(?=( |\t|\r|\n|\z))", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_END, new Regex(@"^end(?=( |\t|\r|\n|\z))", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_READ, new Regex(@"^read(?=( |\t|\r|\n|\z))", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_DATA, new Regex(@"^data(?=( |\t|\r|\n|\z))", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_RESTORE, new Regex(@"^restore(?=( |\t|\r|\n|\z))", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_DIM, new Regex(@"^dim(?=( |\t|\r|\n|\z))", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_RUN, new Regex(@"^run(?=( |\t|\r|\n|\z))", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_GOSUB, new Regex(@"^gosub(?=( |\t|\r|\n|\z))", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_RETURN, new Regex(@"^return(?=( |\t|\r|\n|\z))", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_OPEN, new Regex(@"^open(?=( |\t|\r|\n|\z))", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_CLOSE, new Regex(@"^close(?=( |\t|\r|\n|\z))", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_INPUT, new Regex(@"^input(?=( |\t|\r|\n|\z))", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_OUTPUT, new Regex(@"^output(?=( |\t|\r|\n|\z))", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_FREEFILE, new Regex(@"^freefile(?=( |\t|\r|\n|\z))", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_FILENUM, new Regex(@"^#\d+(?=( |\t|\r|\n|\z))", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_AS, new Regex(@"^as(?=( |\t|\r|\n|\z))", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_APPEND, new Regex(@"^append(?=( |\t|\r|\n|\z))", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_PRINT, new Regex(@"^print(?=( |\t|\r|\n|\z))", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_COMMENT, new Regex(@"^'(?'comment'[^(\n|\r)]*)", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_COMMENT, new Regex(@"^REM(?=( |\t|\r|\n))(?'comment'[^(\n|\r)]*)")),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_FLOAT, new Regex(@"^[-]?(\d+)?\.\d+")),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_INTEGER, new Regex(@"^[-]?\d+")),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_TRUE, new Regex(@"^true", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_FALSE, new Regex(@"^false", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_COMMA, new Regex(@"^,")),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_NOT, new Regex(@"^not(?=( |\t|\r|\n))", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_AND, new Regex(@"^and(?=( |\t|\r|\n))", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_OR, new Regex(@"^or(?=( |\t|\r|\n))", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_IDENTIFIER, new Regex(@"^[a-zA-Z_][a-zA-Z_0-9]*[#|%|$]?", RegexOptions.IgnoreCase)),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_ADD, new Regex(@"^\+")),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_SUBTRACT, new Regex(@"^\-")),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_MULTIPLY, new Regex(@"^\*")),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_DIVIDE, new Regex(@"^\/")),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_POWER, new Regex(@"^\^")),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_LPAREN, new Regex(@"^\(")),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_RPAREN, new Regex(@"^\)")),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_LBRACKET, new Regex(@"^\[")),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_RBRACKET, new Regex(@"^\]")),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_IS_EQUAL, new Regex(@"^==")),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_NOT_EQUAL, new Regex(@"^(!=|<>)")),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_LESS_EQUAL, new Regex(@"^<=")),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_GREATER_EQUAL, new Regex(@"^>=")),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_SET_EQUAL, new Regex(@"^=")),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_LESS_THAN, new Regex(@"^<")),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_GREATER_THAN, new Regex(@"^>")),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_COLON, new Regex(@"^:")),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_SEMICOLON, new Regex(@"^;")),
            new Tuple<Token_Type, Regex>(Token_Type.TOKEN_STRING, new Regex(@"^""(?:[^""\\]|\\.)*""")),
        };

        public IEnumerable<Token> Tokenize(string input)
        {
            int index = 0;
            int lineNumber = 1;
            int columnNumber = 1;
            while (index < input.Length)
            {
                /*bool foundWS = false;
                // Consume empty lines and whitespace.                
                do
                {
                    foundWS = false;
                    if (index + 2 < input.Length && input.Substring(index, 2) == "\r\n")
                    {
                        lineNumber++;
                        columnNumber = 1;
                        index += 2;
                        foundWS = true;
                    }
                    else if (input[index] == '\r' || input[index] == '\n')
                    {
                        lineNumber++;
                        columnNumber = 1;
                        index++;
                        foundWS = true;
                    }
                    else if (input[index] == ' ' || input[index] == '\t')
                    {
                        columnNumber++;
                        index++;
                        foundWS = true;
                    }

                } while (foundWS && index < input.Length);
                */
                bool foundMatch = false;
                Token t = new();
                int remainder = Math.Max(1, input.Length - index);
                foreach (Tuple<Token_Type, Regex> match in matches)
                {
                    if (foundMatch) break;
                    Match m = match.Item2.Match(input, index, remainder);
                    if (m.Success)
                    {                        
                        t.ColumnNumber = columnNumber;
                        t.LineNumber = lineNumber;
                        t.Text = match.Item1 == Token_Type.TOKEN_COMMENT ? m.Groups["comment"].Value : m.Value;
                        t.Type = match.Item1;
                        columnNumber += t.Text.Length;
                        if (t.Type == Token_Type.TOKEN_NEWLINE)
                        {
                            columnNumber = 1;
                            lineNumber++;
                        }
                        index += m.Value.Length;
                        foundMatch = true;                        
                        break;
                    }
                }
                if (foundMatch && t.Type != Token_Type.TOKEN_WHITE_SPACE)
                {
                    yield return t;
                }
                if (!foundMatch)
                {
                    Console.WriteLine($"Error: Unmatched character '{input[index]}' on line: {lineNumber}, column: {columnNumber}");
                    index++;
                }
            }
        }
    }
}
