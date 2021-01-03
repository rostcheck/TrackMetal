using System;
namespace MetalAccounting
{
    public interface ILogWriter
    {
        void WriteEntry(string s);
    }
}
