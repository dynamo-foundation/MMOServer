﻿using System;
using System.Net;
using System.Threading;

namespace MMOServer
{


    public class WebServer
    {
        public void run()
        {
            if (!HttpListener.IsSupported)
            {
                Console.WriteLine("HTTP Listener not supported");
                return;
            }

            HttpListener listener = new HttpListener();

            listener.Prefixes.Add(Global.WebServerURL());

            listener.Start();
            Console.WriteLine("Listening...");

            while (!Global.Shutdown)
            {
                Global.UpdateRand(17);
                HttpListenerContext context = listener.GetContext();
                WebWorker worker = new WebWorker();
                worker.context = context;
                Thread t1 = new Thread(new ThreadStart(worker.run));
                t1.Start();
            }

            listener.Stop();
        }

    }
}
