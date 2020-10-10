using Server.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Server.Implementations
{
    public class WebSocketsClient
    {
        public TcpClient Client { get; private set; }
        public WebSocketsServer Server { get; private set; }
        public string Nickname { get; private set; }
        public WebSocketsClient(WebSocketsServer server,TcpClient client, int Id)
        {
            this.Server = server;
            this.Client = client;

            Nickname = Id.ToString();
        }

        public void Process()
        {
            NetworkStream stream = Client.GetStream();

            try
            {
                // enter to an infinite cycle to be able to handle every change in stream
                while (true)
                {
                    while (!stream.DataAvailable) ;
                        while (Client.Available < 3) ; // match against "get"

                    byte[] bytes = new byte[Client.Available];
                    stream.Read(bytes, 0, Client.Available);
                    string s = Encoding.UTF8.GetString(bytes);

                Console.WriteLine();
                    Console.WriteLine($"Process          {this.Nickname}   PID: {Thread.GetCurrentProcessorId()}");
                    Console.WriteLine("Process");
                Console.WriteLine();

                if (Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase)) 
                    {
                        Handshake(s, stream);
                    } 
                    else 
                    {
                        DecodeMessage(bytes);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Client.Process(): " + ex.Message);
            }
            finally
            {
                if (stream != null)
                    stream.Close();
                if (Client != null)
                    Client.Close();
            }
        }
        private void Handshake(string message, NetworkStream stream)
        {
            ConsoleLogger.Write($"Handshaking with user {Nickname}\n") ;

            // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
            // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
            // 3. Compute SHA-1 and Base64 hash of the new value
            // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
            string swk = Regex.Match(message, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
            string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
            string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

            // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
            byte[] response = Encoding.UTF8.GetBytes(
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Connection: Upgrade\r\n" +
                "Upgrade: websocket\r\n" +
                "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

            stream.Write(response, 0, response.Length);

        }

        private void DecodeMessage(byte[] bytes)
        {
            String incomingData = String.Empty;
            Byte secondByte = bytes[1];
            Int32 dataLength = secondByte & 127;
            Int32 indexFirstMask = 2;
            if (dataLength == 126)
                indexFirstMask = 4;
            else if (dataLength == 127)
                indexFirstMask = 10;

            IEnumerable<Byte> keys = bytes.Skip(indexFirstMask).Take(4);
            Int32 indexFirstDataByte = indexFirstMask + 4;

            Byte[] decoded = new Byte[bytes.Length - indexFirstDataByte];
            for (Int32 i = indexFirstDataByte, j = 0; i < bytes.Length; i++, j++)
            {
                decoded[j] = (Byte)(bytes[i] ^ keys.ElementAt(j % 4));
            }

             incomingData = Encoding.UTF8.GetString(decoded, 0, decoded.Length);

            HandleMessage(incomingData);
        }
    

        private void HandleMessage(string message)
        {
            if (message.StartsWith("/")) 
            {
                HandleCommand(message.Remove(0, 1));
            }
            else
            {
                ConsoleLogger.Write($"User {Nickname} got message: " + message);
                BroadcastMessage($"User {Nickname}: " + message);
            }
        }
        private void HandleCommand(string commandLine)
        {
            if (commandLine.Contains("change_name="))
            {
                string nickname = commandLine.Substring(12);
                ConsoleLogger.Write($"User {Nickname} changed nickname to {nickname}");
                Nickname = nickname;

                BroadcastMessage($"User {Nickname} changed nickname to {nickname}");
            }
            else
            {
                ConsoleLogger.Write($"User {Nickname} typed unknown command " + $"/{commandLine}");

                BroadcastMessage($"User {Nickname} typed unknown command " + $"/{commandLine}");
            }
        }

        private void BroadcastMessage(string message)
        {
            var users = Server.Clients
                .Where(n => n.Nickname != Nickname)
                .ToList();

            Byte[] response;
            Byte[] bytesRaw = Encoding.UTF8.GetBytes(message);
            Byte[] frame = new Byte[10];

            Int32 indexStartRawData = -1;
            Int32 length = bytesRaw.Length;

            frame[0] = (Byte)129;
            if (length <= 125)
            {
                frame[1] = (Byte)length;
                indexStartRawData = 2;
            }
            else if (length >= 126 && length <= 65535)
            {
                frame[1] = (Byte)126;
                frame[2] = (Byte)((length >> 8) & 255);
                frame[3] = (Byte)(length & 255);
                indexStartRawData = 4;
            }
            else
            {
                frame[1] = (Byte)127;
                frame[2] = (Byte)((length >> 56) & 255);
                frame[3] = (Byte)((length >> 48) & 255);
                frame[4] = (Byte)((length >> 40) & 255);
                frame[5] = (Byte)((length >> 32) & 255);
                frame[6] = (Byte)((length >> 24) & 255);
                frame[7] = (Byte)((length >> 16) & 255);
                frame[8] = (Byte)((length >> 8) & 255);
                frame[9] = (Byte)(length & 255);

                indexStartRawData = 10;
            }

            response = new Byte[indexStartRawData + length];

            Int32 i, reponseIdx = 0;

            //Add the frame bytes to the reponse
            for (i = 0; i < indexStartRawData; i++)
            {
                response[reponseIdx] = frame[i];
                reponseIdx++;
            }

            //Add the data bytes to the response
            for (i = 0; i < length; i++)
            {
                response[reponseIdx] = bytesRaw[i];
                reponseIdx++;
            }
            Console.WriteLine();
            Console.WriteLine($"BroadcastMessage    {this.Nickname}         PID: {Thread.GetCurrentProcessorId()}    ");
            Console.WriteLine("BroadcastMessage");
            Console.WriteLine();


            foreach (var user in users)
            {
                var userStream = user.Client.GetStream();


                Console.WriteLine("message was sent to " + user.Nickname   +$"         PID: { Thread.GetCurrentProcessorId()}"    );
                userStream.Write(response, 0, response.Length);

            }
        }
    }
}


