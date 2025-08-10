using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uBasic
{
    public class SymbolTable
    {
        List<Dictionary<string, object?>> frames;

        public SymbolTable()
        {
            frames = new List<Dictionary<string, object?>>();
            Clear();
        }

        public void Clear()
        {
            frames.Clear();
            frames.Add(new Dictionary<string, object?>());
        }

        public void Push()
        {
            Dictionary<string, object?> frame = new();
            frames.Add(frame);
        }

        public void Pop()
        {
            if (frames.Count > 0)
            {
                frames.RemoveAt(frames.Count - 1);
            }
            else
            {
                throw new ArgumentOutOfRangeException("No stack frames are present.");
            }
        }

        public bool Contains(string name)
        {
            Dictionary<string, object?> frame;
            string key = name.ToUpperInvariant().Trim();
            for (int index = frames.Count - 1; index >= 0; index--)
            {
                frame = frames[index];
                if (frame.TryGetValue(key, out object? value))
                    return true;
            }
            return false;
        }

        public object? Get(string name)
        {
            Dictionary<string, object?> frame;
            string key = name.ToUpperInvariant().Trim();
            for (int index = frames.Count - 1; index >= 0; index--)
            {
                frame = frames[index];
                if (frame.TryGetValue(key, out object? value))
                    return value;
            }
            throw new ArgumentException($"Variable \"{key}\" not found.");
        }

        public void Set(string name, object? value)
        {
            string key = name.ToUpperInvariant().Trim();
            Dictionary<string, object?> frame;
            // If there is a variable of that name already in the stack frames update it.
            for (int index = frames.Count - 1; index >= 0; index--)
            {
                frame = frames[index];
                if (frame.ContainsKey(key))
                {
                    frame[key] = value;
                    return;
                }
            }
            // Otherwise add it to the current stack frame
            frame = frames[frames.Count - 1];
            frame[key] = value;
        }

    }
}
