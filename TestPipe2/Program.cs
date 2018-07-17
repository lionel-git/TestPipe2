using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// Test git 5

namespace TestPipe2
{
    class Program
    {
        private static readonly string ArgChild = "--child";

        static void LaunchChild()
        {
            Console.WriteLine("Process0");
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = "TestPipe2.exe";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = ArgChild;

            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using-statement will close.
                Console.WriteLine("Process");
                using (Process exeProcess = Process.Start(startInfo))
                {
                    var svr = new NamedPipeServerStream($"PipesOfPiece_{exeProcess.Id}", PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte);
                    svr.WaitForConnection();

                    var data=PipeStreamHelper.ReadData(svr);

                    var response = Encoding.UTF8.GetBytes("OK from server");
                    PipeStreamHelper.WriteData(svr, response);

                    Console.WriteLine($"Hash on server = {PipeStreamHelper.GetHash(data)}");

                    while (!exeProcess.WaitForExit(1000)) ;
                    Console.WriteLine($"retCode={exeProcess.ExitCode}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

       


        static int DoWork()
        {
            //PipeStreamHelper.WaitForDebug();
            //Debugger.Launch();

            var clt = new NamedPipeClientStream("localhost", $"PipesOfPiece_{ Process.GetCurrentProcess().Id}", PipeDirection.InOut, PipeOptions.None);

            clt.Connect();

            var sw = new Stopwatch();


            var inBuff = new byte[20*1024*1024+175566];
            for (int i = 0; i < inBuff.Length; i++)
                inBuff[i] = (byte)(2 * i + 1);
            Console.WriteLine($"hash on client = {PipeStreamHelper.GetHash(inBuff)}");
   
            sw.Restart();
            PipeStreamHelper.WriteData(clt, inBuff);
            var data=PipeStreamHelper.ReadData(clt);
            sw.Stop();
            Console.WriteLine($"ms={sw.ElapsedMilliseconds} {(1000.0*(double)inBuff.Length/sw.ElapsedMilliseconds)/(1024*1024)} Mo/s");

            // Task.WaitAll(read, write);
            var cltMsg = Encoding.UTF8.GetString(data, 0, data.Length);
            Console.WriteLine("Client got message: {0}", cltMsg);
            return -7;
        }
    

        static int Main(string[] args)
        {
            Console.WriteLine("Hello world");
            if (args.Length >= 1 && args[0] == ArgChild)
                return DoWork();
            else
            {
                LaunchChild();
                return 23;
            }
        }
    }
}
