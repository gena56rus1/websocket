using Server.Helpers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server.Implementations
{
    public class WebSocketsServer
    {
        private TcpListener listener;
        public List<WebSocketsClient> Clients; 

        private readonly int port;
        private int Id;

        public WebSocketsServer(int port)
        {
            this.port = port;
            Clients = new List<WebSocketsClient>();
            Id = 5;
        }
        
        public void RemoveUser(WebSocketsClient user)
        {
            Clients.Remove(user);
            ConsoleLogger.Write($"{user.Nickname} deleted from server collection");

            user.Client.GetStream().Close();
        }
        
        public void Start()
        {
            try
            {
                listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
                listener.Start();

                Console.WriteLine("Server is ready to get connections\n");

                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();

                    WebSocketsClient clientObj = new WebSocketsClient(this,client, Id++);

                    Clients.Add(clientObj);

                    Thread clientThread = new Thread(new ThreadStart(clientObj.Process));
                    clientThread.Start();
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error in Server.Start():" + ex.Message);
            }
            finally
            {
                if (listener != null)
                    listener.Stop();
            }
        }
    }
}
