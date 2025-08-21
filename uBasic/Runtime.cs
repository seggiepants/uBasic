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
        public List<Parser.AstStatement> program;
        public Dictionary<int, int> lineNumbers;
        public Dictionary<string, int> lineLabels;
        public int instructionPointer;
        public bool running;
        private int counter;
        public Stack<int> forStack;
        public Stack<int> ifStack;
        public Stack<int> callStack;
        public List<Parser.AstData> dataSegment;
        int dataIndex, dataPtr;

        public Runtime()
        {
            fnTable = new FunctionTable();
            symbolTable = new SymbolTable();
            stack = new Stack<object?>();
            program = new List<Parser.AstStatement>();
            lineNumbers = new Dictionary<int, int>();
            lineLabels = new Dictionary<string, int>();
            instructionPointer = 0;
            running = false;
            counter = 1;
            forStack = new Stack<int>();
            ifStack = new Stack<int>();
            callStack = new Stack<int>();
            dataSegment = new();
            dataIndex = 0;
            dataPtr = 0;
        }

        public void Clear()
        {
            fnTable.Clear();
            symbolTable.Clear();
            stack.Clear();
            program.Clear();
            lineNumbers.Clear();
            lineLabels.Clear();
            instructionPointer = 0;
            running = false;
            counter = 1;
            forStack.Clear();
            ifStack.Clear();
            callStack.Clear();
            dataIndex = 0;
            dataPtr = 0;
        }

        public int NextCounter()
        {
            return counter++;
        }

        public void DataRestore()
        {
            dataIndex = 0;
            dataPtr = 0;
        }

        public Object? DataNext()
        {
            object? ret = null;
            if (dataSegment.Count > 0 && dataIndex < dataSegment.Count && dataSegment[dataIndex] != null && dataSegment[dataIndex].values != null)
            {
                ret = dataSegment[dataIndex].values[dataPtr].Get();
                dataPtr++;
                if (dataSegment[dataIndex] != null && dataSegment[dataIndex].values != null && dataPtr >= dataSegment[dataIndex].values.Count)
                {
                    dataIndex++;
                    dataPtr = 0;
                }                
            }
            return ret;
        }

        public object? Run()
        {
            instructionPointer = 0;
            running = true;
            object? result = null;
            while(running)
            {
                if (instructionPointer >= program.Count || instructionPointer < 0)
                    running = false;
                else
                {
                    int oldPointer = instructionPointer;
                    Parser.AstStatement stmt = program[instructionPointer];
                    result = stmt.Interpret(this);
                    // only increment pointer if it stayed put (no GOTO/branch)
                    if (oldPointer == instructionPointer)
                        instructionPointer++;
                    if (instructionPointer >= program.Count || instructionPointer < 0)
                        running = false;
                }
            }
            return result;
        }
    }
}
