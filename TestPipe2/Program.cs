using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
                    var srvBuff = new byte[256];
                    var srvL = svr.Read(srvBuff, 0, srvBuff.Length);
                    var svrMsg = Encoding.UTF8.GetString(srvBuff, 0, srvL);
                    Console.WriteLine("Server got message: {0}", svrMsg);
                    var response = Encoding.UTF8.GetBytes("We're done now");
                    svr.Write(response, 0, response.Length);

                    Console.WriteLine("It's all over");
                    while (!exeProcess.WaitForExit(1000)) ;
                    Console.WriteLine($"ret={exeProcess.ExitCode}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static int DoWork()
        {
            var clt = new NamedPipeClientStream("localhost", $"PipesOfPiece_{ Process.GetCurrentProcess().Id}", PipeDirection.InOut, PipeOptions.Asynchronous);

            clt.Connect();
            var inBuff = new byte[256];
            var read = clt.ReadAsync(inBuff, 0, inBuff.Length);
            var msg = Encoding.UTF8.GetBytes("Hello!");
            var write = clt.WriteAsync(msg, 0, msg.Length);
            Task.WaitAll(read, write);
            var cltMsg = Encoding.UTF8.GetString(inBuff, 0, read.Result);
            Console.WriteLine("Client got message: {0}", cltMsg);
            return 7;
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
