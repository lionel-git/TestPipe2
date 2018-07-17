using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestPipe2
{
    public static class PipeStreamHelper
    {
        private static readonly int ChunkSize = 65536; // Max data exchanged with pipestream ?

        static byte[] GetBytes(int length)
        {
            var b = new byte[4];
            for (int i = 0; i < b.Length; i++)
            {
                b[i] = (byte)(length & 0xFF);
                length = length >> 8;
            }
            return b;
        }

        static int GetInt(byte[] b)
        {
            int l = 0;
            for (int i = b.Length - 1; i >= 0; i--)
                l = (l << 8) + b[i];
            return l;
        }

        public static void WriteData(PipeStream p, byte[] data)
        {
            var b = GetBytes(data.Length);
            p.Write(b, 0, b.Length);
            int pos = 0;
            while (pos < data.Length)
            {
                int sendSize = Math.Min(data.Length - pos, ChunkSize);
                p.Write(data, pos, sendSize);
                pos += sendSize;
            }
        }

        public static byte[] ReadData(PipeStream p)
        {
            var b = new byte[4];
            var l = p.Read(b, 0, b.Length);
            if (l != b.Length)
                throw new Exception($"Invalid length read: {l}");
            int n = GetInt(b);
            Console.WriteLine($"Length={n}");
            var buffer = new byte[n];
            int pos = 0;
            while (pos<n)
            {
                var readSize = p.Read(buffer, pos, n - pos);
               // Console.WriteLine($"Read: {readSize}");
                pos += readSize;
            }
            return buffer;
        }

        public static void WaitForDebug()
        {
            while (File.Exists(@"c:\tmp\debug.txt"))
            {
                Console.WriteLine("Wating for debugger...");
                Thread.Sleep(1000);
            }
        }
    }
}
