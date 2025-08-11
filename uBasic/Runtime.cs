using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uBasic
{
    public class Runtime
    {
        public SymbolTable symbolTable;
        public Stack<object?> stack;
        public FunctionTable fnTable;
        public SortedList<int, Parser.AstStatements> program;

        public Runtime()
        {
            fnTable = new FunctionTable();
            symbolTable = new SymbolTable();
            stack = new Stack<object?>();
            program = new SortedList<int, Parser.AstStatements>();
        }

        public void Clear()
        {
            fnTable.Clear();
            symbolTable.Clear();
            stack.Clear();
            program.Clear();
        }
    }
}
