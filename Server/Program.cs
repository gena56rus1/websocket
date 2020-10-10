using Server.Implementations;

namespace Server
{
    class Program
    {
        public static void Main(string[] args)
        {
            WebSocketsServer server = new WebSocketsServer(5000);
            server.Start();
        }
    }
}
