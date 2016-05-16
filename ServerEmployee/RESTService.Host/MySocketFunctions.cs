using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;


namespace RESTService.Host
{
    static class MySocketFunctions
    {

        public static string socketReadLine(Socket handler)
        {
            string data = null;
            byte[] bytes = new Byte[1];
            while (true)
            {
                bytes = new byte[1];
                int bytesRec = -1;
                bytesRec = handler.Receive(bytes);
                data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                if (data.IndexOf("\n") > -1)
                {
                    data = data.Replace("\n", "");
                    break;
                }


            }
            return data;

        }
        public static int socketWriteLine(Socket sender, string message)
        {
            byte[] msg = Encoding.ASCII.GetBytes(message + "\n");

            // Send the data through the socket.
            int bytesSent = sender.Send(msg);
            return bytesSent;

        }
        public static void socketReceiveFile(Socket handler, string reference)
        {
            int CHUNK = 1000000;
            byte[] data = null;
            int effective_received = 0;
            int total_byte = 0;
            int total_read = 0;
            FileStream file = File.Create(reference);
            try
            {
                total_byte = Int32.Parse(socketReadLine(handler));
                Console.WriteLine(reference + " file of " + total_byte + " Byte");
                if (total_byte >= 0)
                {
                    while (total_byte != 0)
                    {
                        if (total_byte > CHUNK)
                            data = new byte[CHUNK];
                        else
                            data = new byte[total_byte];
                        effective_received = handler.Receive(data);

                        total_read += effective_received;

                        total_byte -= effective_received;

                        if ((total_read < total_byte) && (effective_received == 0))
                            throw new SocketException();

                        byte[] towrite = new byte[effective_received];
                        char[] s = new char[effective_received];
                        for (int i = 0; i < effective_received; i++)
                        {
                            towrite[i] = data[i];
                            s[i] = (char)data[i];

                        }

                        file.Write(towrite, 0, effective_received);

                    }


                }
            }
            catch (SocketException)
            {
                Console.WriteLine("eccezione.chiudo file e cancello ");
                file.Close(); throw new SocketException();
            }

            Console.WriteLine("xXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXx");

            Console.WriteLine("Total read " + total_read.ToString());
            file.Close();

        }
        public static void socketSendFile(Socket handler, string reference)
        {
            int CHUNK = 1000000;

            int effective_sent = 0;
            FileInfo f1 = new FileInfo(reference);
            byte[] data = new byte[CHUNK];
            int total_byte = (int)f1.Length;

            if (total_byte >= 0)
            {
                FileStream file = null;
                try
                {
                    socketWriteLine(handler, total_byte.ToString());
                    file = File.Open(reference, FileMode.Open);
                    while (total_byte != 0)
                    {

                        int read = file.Read(data, 0, CHUNK);
                        byte[] tosend = new byte[read];
                        for (int i = 0; i < read; i++)
                            tosend[i] = data[i];

                        effective_sent = handler.Send(tosend);
                        total_byte -= effective_sent;


                    }
                    file.Close();

                }
                catch (SocketException)
                {
                    Console.WriteLine("interrupted Upload");
                    if (file != null)
                        file.Close();
                }
            }


        }

    }
}
