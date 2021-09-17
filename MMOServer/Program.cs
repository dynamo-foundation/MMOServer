using System;
using System.Diagnostics;
using System.Threading;

namespace MMOServer
{
    class Program
    {
        static void Main(string[] args)
        {




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
