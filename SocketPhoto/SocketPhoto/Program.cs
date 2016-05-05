using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
namespace SocketPhoto
{
    class Program
    {
        static void Main(string[] args)
        {
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 11000);
            Socket listener = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(localEndPoint);
            listener.Listen(10);
            printPhoto();
            
            while (true)
            {

                Console.WriteLine("Waiting for a connection...");
                Socket handler = listener.Accept();
                ServerProcessor processor = new ServerProcessor(handler);
                Console.WriteLine("Accepted a connection...");
                Thread thread = new Thread(new ThreadStart(processor.SERVICE));
                thread.Start();

            }
        }

        private static void printPhoto()
        {
            Console.WriteLine("----------------------------");
            Console.WriteLine("..... Socket Photo .....");
            Console.WriteLine("----------------------------");
        }
    }
}
