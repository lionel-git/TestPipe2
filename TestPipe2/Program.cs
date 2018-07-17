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
                    //Client
                    var client = new NamedPipeClientStream($"PipesOfPiece_{exeProcess.Id}");
                    client.Connect(2000);

                    StreamReader reader = new StreamReader(client);
                    Console.WriteLine("Connected");

                    while (!exeProcess.WaitForExit(1000))
                    {
                        Console.WriteLine(exeProcess.Id);
                        Console.WriteLine("Start readline");
                        var line = reader.ReadToEnd();
                        Console.WriteLine($"Read line={line}");
                    }
                    Console.WriteLine($"Process finished {exeProcess.Id} : {exeProcess.ExitCode}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static int DoWork()
        {
            var server = new NamedPipeServerStream($"PipesOfPiece_{ Process.GetCurrentProcess().Id}");
            server.WaitForConnection();
            StreamWriter writer = new StreamWriter(server);

            try
            {
                Console.WriteLine("Start Runner");

                Thread.Sleep(10000);
                Console.WriteLine("Calc finished");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                writer.WriteLine(e.Message);
                writer.WriteLine("Suite1");
                writer.WriteLine("Suite2");
                writer.Flush();
                return -1;
            }

            writer.WriteLine("OK");
            writer.Flush();
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
