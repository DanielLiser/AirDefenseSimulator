using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HaganaSimulator
{
    public class TcpBroadcaster
    {
        private TcpListener listener;
        private StreamWriter clientStream;

        public void StartServer(int port)
        {
            Task.Run(() =>
            {
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                Console.WriteLine($"[TCP] Server started on port {port}. Waiting for Python to connect...");

                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("[TCP] Python client connected successfully!");

                var networkStream = client.GetStream();
                clientStream = new StreamWriter(networkStream) { AutoFlush = true };
            });
        }

        public void SendEvent(object payload)
        {
            if (clientStream != null)
            {
                try
                {
                    string json = System.Text.Json.JsonSerializer.Serialize(payload);
                    clientStream.WriteLine(json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TCP Error] Connection lost: {ex.Message}");
                    clientStream = null;
                }
            }
        }
    }
}
