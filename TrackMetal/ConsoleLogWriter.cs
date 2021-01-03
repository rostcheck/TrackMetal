using System;
using MetalAccounting;

namespace TrackMetal
{
    public class ConsoleLogWriter : ILogWriter
    {
        public ConsoleLogWriter()
        {
        }

        public void WriteEntry(string s)
        {
            Console.WriteLine(s);
        }
    }
}
