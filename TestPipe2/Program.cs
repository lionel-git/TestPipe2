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

                    var data=PipeStreamHelper.ReadData(svr);
                    int checkSum = 0;
                    for (int i=0;i<data.Length;i++)
                        checkSum+=data[i];
                    Console.WriteLine($"CheckSum = {checkSum}");

                    var response = Encoding.UTF8.GetBytes("We're done now");
                    PipeStreamHelper.WriteData(svr, response);
                    Console.WriteLine("It's all over");
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
            PipeStreamHelper.WaitForDebug();

            var clt = new NamedPipeClientStream("localhost", $"PipesOfPiece_{ Process.GetCurrentProcess().Id}", PipeDirection.InOut, PipeOptions.None);

            clt.Connect();

            var inBuff = new byte[10*1024*1024+1755662];
            int checkSum = 0;
            for (int i = 0; i < inBuff.Length; i++)
            {
                inBuff[i] = (byte)(2 * i + 1);
                checkSum += inBuff[i];
            }
            Console.WriteLine($"CheckSum={checkSum}");
            PipeStreamHelper.WriteData(clt, inBuff);


            var data=PipeStreamHelper.ReadData(clt);


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
