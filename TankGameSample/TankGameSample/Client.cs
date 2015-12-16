using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Net;

namespace TankGameSample
{
    class Client
    {
        System.Net.Sockets.TcpClient clientSocket;
        private NetworkStream clientStream; 
        private TcpClient client; 
        private BinaryWriter writer; 
        public int connCount;

        private NetworkStream serverStream;       
        private TcpListener listener;        
        public string reply = ""; 

        public Client()
        {
            load();
        }
        private void load()
        {
            listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 7000);
            listener.Start();
        }

        public void connect()
        {
            try
            {
                clientSocket.Connect("127.0.0.1", 6000);
            }
            catch (Exception e)
            {
            }
        }


        public void send(String message)
        {
            Console.WriteLine(message);
            connCount = 0;
            clientSocket = new System.Net.Sockets.TcpClient();
            connect();
            if (clientSocket.Connected)
            {
                NetworkStream serverStream = clientSocket.GetStream();
                try
                {
                    byte[] outStream = System.Text.Encoding.ASCII.GetBytes(message);
                    serverStream.Write(outStream, 0, outStream.Length);
                    clientSocket.Close();
                    serverStream.Flush();
                    connCount++;
                }
                catch (ArgumentNullException e)
                {

                }
            }
        }

        public String receive()
        {

            if (connCount > 0)
            {
                Socket connection = listener.AcceptSocket();
                this.serverStream = new NetworkStream(connection);

                SocketAddress sockAdd = connection.RemoteEndPoint.Serialize();
                string s = connection.RemoteEndPoint.ToString();
                List<Byte> inputStr = new List<byte>();

                int asw = 0;
                while (asw != -1)
                {
                    asw = this.serverStream.ReadByte();
                    inputStr.Add((Byte)asw);
                }

                reply = Encoding.UTF8.GetString(inputStr.ToArray());
                this.serverStream.Close();


                return reply.Substring(0, reply.IndexOf("#"));
            }
            else
            {
                return "Not connected";
            }
        }

    }
}
