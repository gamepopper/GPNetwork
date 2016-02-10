using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using NetworkMessage;

namespace Server
{
    public class Server
    {
        private TcpListener listener;
        private BinaryFormatter binary = new BinaryFormatter();
        private List<Client> userList = new List<Client>();
        private int clientCount = 0;

        private Thread ListenThread;

        private bool listening = false;

        public Server(bool UseLoopback, int port)
        {
            IPAddress ipAddress = UseLoopback ? IPAddress.Loopback : LocalIPAddress();
            listener = new TcpListener(ipAddress, port);
            Console.WriteLine("Server has started at: " + ipAddress + " Port: " + port);
            ListenThread = new Thread(new ThreadStart(ListenMethod));
        }

        public void Start()
        {
            if (ListenThread != null && ListenThread.IsAlive)
            {
                Stop();
            }
            
            ListenThread.Start();
        }

        public void Stop()
        {
            foreach (Client client in userList)
            {
                client.Thread.Abort();
                client.Thread.Join();
            }

            ListenThread.Abort();
            ListenThread.Join();

            listener.Stop();
        }

        private void ListenMethod()
        {
            listening = true;
            userList.Clear();
            clientCount = 0;

            Console.WriteLine("Welcome to the chat zone server, awaiting users...");

            try
            {
                listener.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                listener.Stop();
            }

            while (listening)
            {
                if (!listener.Pending())
                {
                    Thread.Sleep(500);
                    continue;
                }

                try
                {
                    TcpClient tcp = listener.AcceptTcpClient();
                    NetworkStream stream = tcp.GetStream();

                    clientCount++;

                    Client client = new Client(tcp, stream, clientCount);

                    userList.Add(client);

                    foreach (Client user in userList)
                    {
                        binary.Serialize(user.Stream, new AMessage(MessageType.START, clientCount, "New user in the chat zone!" ));
                        user.Stream.Flush();
                    }

                    client.Thread = new Thread(new ParameterizedThreadStart(ClientMethod));
                    client.Thread.Start(client);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        private void ClientMethod(Object obj)
        {
            Client client = obj as Client;

            if (client != null)
            {
                try
                {
                    while (true)
                    {
                        AMessage recievedMessage = binary.Deserialize(client.Stream) as AMessage;
 
                        if (recievedMessage != null)
                        {
                            Console.WriteLine(recievedMessage.ID + " : " + recievedMessage.Message);

                            if (recievedMessage.Type == MessageType.QUIT && recievedMessage.ID == client.ClientNumber)
                            {
                                break;
                            }

                            foreach (Client user in userList)
                            {
                                binary.Serialize(user.Stream, recievedMessage);
                                user.Stream.Flush();
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error occured: " + e.Message);
                }
                finally
                {
                    client.Thread.Abort();
                    client.Thread.Join();
                    userList.Remove(client);
                }
            }
        }

        private IPAddress LocalIPAddress()
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return null;
            }

            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            return host
                .AddressList
                .LastOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        }

    }

    public class Client
    {
        public TcpClient tcpClient;
        public NetworkStream Stream;
        public Thread Thread;
        public int ClientNumber;

        public Client(TcpClient client, NetworkStream stream, int clientNumber) 
        {
            this.tcpClient = client;
            this.Stream = stream;
            this.ClientNumber = clientNumber;
        }
    }
}
