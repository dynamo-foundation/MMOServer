using System;
using System.Diagnostics;
using System.Threading;

namespace MMOServer
{
    class Program
    {
        static void Main(string[] args)
        {


            string address = "dy1qzvx3yfrucqa2ntsw8e7dyzv6u6dl2c2wjvx5jy";
            string signature = "Jyv5LFiSCHKmY+DRAK324jlw4PguGdwMG6jdMDO6HQVRUDf4bGMJW+wWFtHoFPMU54lkHISn+ZCQ0Fo3f/Ie6sk=";
            string message = "123456";
            string sArgs =  address + " " + signature + " " + message;

            Process p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = @"node.exe",
                    Arguments = "verify_address.js " + sArgs,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = @"C:\Users\user\source\repos\MMOServer\MMOServer"
                }
            };
            p.Start();
            while (!p.HasExited)
            {
                Thread.Sleep(100);
            }
            Console.WriteLine(p.StandardError.ReadToEnd());
            Console.WriteLine(p.StandardOutput.ReadToEnd());

            uint loops = 0;

            Global.LoadSettings();

            Console.WriteLine("Starting Web server...");
            WebServer server = new WebServer();
            Thread t1 = new Thread(new ThreadStart(server.run));
            t1.Start();


            while (!Global.Shutdown)
            {
                Thread.Sleep(100);
                loops++;
                Global.UpdateRand(loops);
            }
        }
    }
}
