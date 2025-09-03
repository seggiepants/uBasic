using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uBasic
{
    public enum FileMode
    {
        INPUT,
        OUTPUT,
        APPEND
    };

    public class FileReference
    {
        public string fileName { get; set; }
        StreamReader? sr;
        StreamWriter? sw;        

        public FileReference()
        {
            sr = null;
            sw = null;
            fileName = "";
        }

        ~FileReference()
        {
            Cleanup();            
        }

        private void Cleanup()
        {
            if (sr != null)
            {
                sr.Close();
                sr.Dispose();
                sr = null;
            }
            else if (sw != null)
            {
                sw.Flush();
                sw.Close();
                sw.Dispose();
                sw = null;
            }
        }

        public void OpenInput(string fileName)
        {
            if (!File.Exists(fileName))
                throw new FileNotFoundException($"File not found: \"{fileName}\"");
            Cleanup();
            sr = new StreamReader(fileName);
        }

        public void OpenOutput(string fileName, bool append)
        {
            Cleanup();
            sw = new StreamWriter(fileName, append);
        }

        public void Close()
        {
            Cleanup();
        }

        public bool IsOutput() { return sw != null; }
        public bool IsInput() { return sr != null; }

        public bool IsEOF()
        {
            if (sr != null)
                return sr.EndOfStream;
            return false;
        }

        public void Write(string text)
        {
            if (sw != null)
                sw.Write(text);
        }

        public void WriteLine(string text)
        {
            if (sw != null)
                sw.WriteLine(text);
        }

        public char Read()
        {
            if (sr != null && !sr.EndOfStream)
                return (char)sr.Read();
            return '\0';
        }

        public string Read(int numChars)
        {
            if (sr != null && !sr.EndOfStream)
            {
                char[] buffer = new char[numChars];
                sr.Read(buffer, 0, numChars);
                return new string(buffer);
            }
            return "";
        }

        public string ReadLine()
        {
            if (sr != null && !sr.EndOfStream)
                return sr.ReadLine() ?? "";
            return "";
        }

    }

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
        public Dictionary<int, FileReference> fileTable;

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
            fileTable = new();
        }

        ~Runtime()
        {
            fnTable.Clear();
            symbolTable.Clear();
            stack.Clear();
            program.Clear();
            lineNumbers.Clear();
            lineLabels.Clear();
            forStack.Clear();
            ifStack.Clear();
            callStack.Clear();
            dataSegment.Clear();
            FileCloseAll();
            fileTable.Clear();
        }

        public int FreeFile()
        {
            if (fileTable.Keys.Count == 0) return 1;

            return (from int key in fileTable.Keys
                    select key).Max() + 1;
        }

        public void FileClose(int handle)
        {
            if (fileTable.ContainsKey(handle))
            {
                fileTable[handle].Close();
                fileTable.Remove(handle);
            }
            else
                throw new Exception($"No file with handle #{handle} found.");
        }

        public void FileCloseAll()
        {
            foreach(KeyValuePair<int, FileReference> pair in fileTable)
            {
                pair.Value.Close();
            }
            fileTable.Clear();
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

        public void DataRestore(string? label, int? line)
        {
            dataIndex = 0;
            dataPtr = 0;
            int index = -1;
            if (line != null && lineNumbers.ContainsKey((int)line))
            {
                index = lineNumbers[(int)line];
            }
            else if (label != null && lineLabels.ContainsKey((string)label))
            {
                index = lineLabels[(string)label];
            }
            if (index != -1)
            {
                bool found = false;
                while (!found && index < program.Count)
                {
                    if (program[index].stmtData != null)
                    {
                        for (int i = 0; i < dataSegment.Count; i++)
                        {
                            if (dataSegment[i] == program[index].stmtData)
                            {
                                found = true;
                                dataPtr = 0;
                                dataIndex = i;
                                break;
                            }
                        }
                    }
                    index++;
                }
            }            
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
            if (ret.GetType() == typeof(Parser.AstString))
                return (ret as Parser.AstString).Value;
            else if (ret.GetType() == typeof(Parser.AstBoolean))
                return (ret as Parser.AstBoolean).Value;
            else if (ret.GetType() == typeof(Parser.AstInteger))
                return (ret as Parser.AstInteger).Value;
            else if (ret.GetType() == typeof(Parser.AstFloat))
                return (ret as Parser.AstFloat).Value;
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
