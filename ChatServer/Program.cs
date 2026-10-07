using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ChatServer
{
    class ConnectionHandler
    {
        public Socket ClientSocket { get; private set; }
        private NetworkStream ns;
        private StreamReader reader;
        private StreamWriter writer;
        public string Username { get; private set; } = "Anonymous";

        private static readonly Dictionary<string, ConnectionHandler> ActiveClients = new Dictionary<string, ConnectionHandler>();
        private static readonly object LockObject = new object();

        public ConnectionHandler(Socket socket)
        {
            this.ClientSocket = socket;
        }

        public void HandleConnection()
        {
            try
            {
                ns = new NetworkStream(ClientSocket);
                reader = new StreamReader(ns);
                writer = new StreamWriter(ns);

                string requestedUser = reader.ReadLine();
                if (string.IsNullOrEmpty(requestedUser)) requestedUser = "User_" + Guid.NewGuid().ToString().Substring(0, 4);

                lock (LockObject)
                {
                    while (ActiveClients.ContainsKey(requestedUser))
                    {
                        requestedUser += "_1";
                    }
                    Username = requestedUser;

                    // Sync list of existing users to the new client
                    foreach (var existingUser in ActiveClients.Keys)
                    {
                        SendMessageToSelf($"SERVER: {existingUser} joined the chat room.");
                    }

                    ActiveClients.Add(Username, this);
                }

                Console.WriteLine($"[JOIN] {Username} connected.");
                BroadcastMessage($"SERVER: {Username} joined the chat room.", null);

                string message;
                while (true)
                {
                    message = reader.ReadLine();
                    if (message == null || message.Trim().ToLower() == "exit") break;

                    if (message.StartsWith("/pm "))
                    {
                        string[] parts = message.Split(new[] { ' ' }, 3);
                        if (parts.Length >= 3)
                        {
                            SendPrivateMessage(parts[1], parts[2]);
                        }
                        else
                        {
                            SendMessageToSelf("SERVER System: Use format /pm <username> <message>");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[{Username}]: {message}");
                        BroadcastMessage($"{Username}: {message}", null);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {Username}: {ex.Message}");
            }
            finally
            {
                Disconnect();
            }
        }

        private void SendPrivateMessage(string targetUser, string msg)
        {
            lock (LockObject)
            {
                if (ActiveClients.TryGetValue(targetUser, out ConnectionHandler targetHandler))
                {
                    targetHandler.SendMessageToSelf($"[PRIVATE from {Username}]: {msg}");
                    this.SendMessageToSelf($"[PRIVATE to {targetUser}]: {msg}");
                    Console.WriteLine($"[PRIVATE] {Username} -> {targetUser}: {msg}");
                }
                else
                {
                    SendMessageToSelf($"SERVER System: User '{targetUser}' not found.");
                }
            }
        }

        private void SendMessageToSelf(string msg)
        {
            try
            {
                writer.WriteLine(msg);
                writer.Flush();
            }
            catch { }
        }

        private static void BroadcastMessage(string msg, ConnectionHandler excludeClient)
        {
            lock (LockObject)
            {
                foreach (var pair in ActiveClients)
                {
                    if (pair.Value != excludeClient)
                    {
                        pair.Value.SendMessageToSelf(msg);
                    }
                }
            }
        }

        private void Disconnect()
        {
            lock (LockObject)
            {
                if (ActiveClients.ContainsKey(Username))
                {
                    ActiveClients.Remove(Username);
                }
            }

            Console.WriteLine($"[LEAVE] {Username} disconnected.");
            BroadcastMessage($"SERVER: {Username} left the chat room.", null);

            try { reader?.Close(); } catch { }
            try { writer?.Close(); } catch { }
            try { ns?.Close(); } catch { }
            try { ClientSocket?.Close(); } catch { }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            int port = 8765;
            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, port);

            server.Bind(endpoint);
            server.Listen(10);
            Console.WriteLine($"=== Chat Broadcast & PM Server Running on Port {port} ===");

            while (true)
            {
                try
                {
                    Socket clientSocket = server.Accept();
                    ConnectionHandler handler = new ConnectionHandler(clientSocket);
                    Thread clientThread = new Thread(new ThreadStart(handler.HandleConnection));
                    clientThread.IsBackground = true;
                    clientThread.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Accept Error: " + ex.Message);
                }
            }
        }
    }
}