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

        public Runtime()
        {
            fnTable = new FunctionTable();
            symbolTable = new SymbolTable();
            stack = new Stack<object?>();
        }

        public void Clear()
        {
            fnTable.Clear();
            symbolTable.Clear();
            stack.Clear();
        }
    }
}
