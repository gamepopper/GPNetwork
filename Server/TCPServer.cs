using GPNetworkMessage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace GPNetworkServer
{
    public class TCPServer : IServer
    {
        private string ipAddressValue = "";
        private int portNumber = 0;
        private Queue<string> messageQueue = new Queue<string>();

        private TcpListener tcpListener = null;
        private BinaryFormatter binary = new BinaryFormatter();
        private List<Client> userList = new List<Client>();
        private Thread listenThread = null;
        private bool isListening = false;

        public TCPServer() { }

        public TCPServer(string hostname, int port)
        {
            ipAddressValue = hostname;
            portNumber = port;
            tcpListener = new TcpListener(IPAddress.Parse(hostname), port);
            listenThread = new Thread(new ThreadStart(ListenMethod));
        }

        public TCPServer(bool UseLoopback, int port)
        {
            IPAddress ipAddress = UseLoopback ? IPAddress.Loopback : LocalIPAddress();
            ipAddressValue = ipAddress.ToString();
            portNumber = port;
            tcpListener = new TcpListener(ipAddress, port);
            listenThread = new Thread(new ThreadStart(ListenMethod));
        }

        public void Start()
        {
            if (listenThread != null && listenThread.IsAlive)
            {
                listenThread.Abort();
                listenThread.Join();
            }
            else
            {
                listenThread.Start();
            }
        }

        public void Stop()
        {
            SendMessageToAll(new AMessage(MessageType.LEAVE, -1, "Server disconnected"));

            userList.Clear();

            isListening = false;

            if (tcpListener != null) tcpListener.Stop();
        }

        //Oehlert, P., 2011. StackOverflow: Proper way to stop TcpListener. http://stackoverflow.com/questions/365370/proper-way-to-stop-tcplistener

        private void ListenMethod()
        {
            isListening = true;
            userList.Clear();

            try
            {
                tcpListener.Start();
            }
            catch (Exception e)
            {
                messageQueue.Enqueue(e.Message + ": " + e.TargetSite);
                tcpListener.Stop();
            }
            try
            {
                while (isListening)
                {
                    //Oehlert, P., 2011
                    if (!tcpListener.Pending())
                    {
                        Thread.Sleep(500);
                        continue;
                    }


                    TcpClient tcp = tcpListener.AcceptTcpClient();
                    NetworkStream stream = tcp.GetStream();

                    int clientNumber = AssignClientID(userList.Count + 1);

                    Client client = new Client(tcp, stream, clientNumber);

                    binary.Serialize(stream, new AMessage(MessageType.JOIN, clientNumber, "Client Connected Successfully"));
                    messageQueue.Enqueue("Client " + client.ClientNumber + " Connected");

                    AMessage joinedMessage = binary.Deserialize(stream) as AMessage;
                    foreach (Client user in userList)
                    {
                        binary.Serialize(user.Stream, joinedMessage);
                        user.Stream.Flush();
                    }
                    userList.Add(client);

                    client.Thread = new Thread(new ParameterizedThreadStart(ClientMethod));
                    client.Thread.Start(client);

                }
            }
            catch (Exception e)
            {
                messageQueue.Enqueue(e.Message + ": " + e.TargetSite);
                Stop();
            }
        }

        private int AssignClientID(int initID)
        {
            if (initID > 1)
            {
                List<int> CurrentIDs = new List<int>();
                foreach (Client user in userList)
                {
                    CurrentIDs.Add(user.ClientNumber);
                }
                CurrentIDs.Sort();

                if (CurrentIDs.Contains(initID))
                {
                    for (int i = initID; i >= 1; i--)
                    {
                        if (!CurrentIDs.Contains(i))
                        {
                            initID = i;
                            break;
                        }
                    }
                }
            }
            return initID;
        }

        private void ClientMethod(Object obj)
        {
            Client client = obj as Client;

            if (client != null)
            {
                try
                {
                    while (client.Connected)
                    {
                        AMessage recievedMessage = binary.Deserialize(client.Stream) as AMessage;

                        if (recievedMessage != null)
                        {
                            if (recievedMessage.Type == MessageType.LEAVE && recievedMessage.ID == client.ClientNumber)
                            {
                                client.Connected = false;
                                userList.Remove(client);
                            }

                            if (recievedMessage.Type == MessageType.CLIENTCOUNT)
                            {
                                messageQueue.Enqueue("Client Count Request From: " + recievedMessage.ID);
                                recievedMessage.Message = "" + userList.Count;
                                SendMessageToAll(recievedMessage);
                            }
                            else if (recievedMessage.Type == MessageType.INFOTO1)
                            {
                                string id = recievedMessage.Message.Split(';')[0];
                                recievedMessage.Message = recievedMessage.Message.Substring(id.Length + 1);

                                messageQueue.Enqueue("Client " + recievedMessage.ID + " - " + recievedMessage.Message + " - To: " + id);

                                SendMessageToOne(recievedMessage, Convert.ToInt32(id));
                            }
                            else if (recievedMessage.Type == MessageType.INFOEXCEPT1)
                            {
                                string id = recievedMessage.Message.Split(';')[0];
                                recievedMessage.Message = recievedMessage.Message.Substring(id.Length + 1);

                                messageQueue.Enqueue("Client " + recievedMessage.ID + " - " + recievedMessage.Message + " - Except: " + id);

                                SendMessageToAllButOne(recievedMessage, Convert.ToInt32(id));
                            }
                            else
                            {
                                messageQueue.Enqueue("Client " + recievedMessage.ID + " - " + recievedMessage.Message + " - To All");
                                SendMessageToAll(recievedMessage);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    messageQueue.Enqueue(e.Message + ": " + e.TargetSite);
                }
                finally
                {
                    userList.Remove(client);
                    messageQueue.Enqueue("Client " + client.ClientNumber + " Removed");
                }
            }
        }

        public void SendMessageToAll(AMessage sendMessage)
        {
            if (sendMessage.Type == MessageType.INFOEXCEPT1 || sendMessage.Type == MessageType.INFOTO1) sendMessage.Type = MessageType.INFO;

            foreach (Client user in userList)
            {
                binary.Serialize(user.Stream, sendMessage);
                user.Stream.Flush();
            }
        }

        public void SendMessageToOne(AMessage sendMessage, int ClientNumber)
        {
            sendMessage.Type = MessageType.INFO;
            foreach (Client user in userList)
            {
                if (user.ClientNumber == ClientNumber)
                {
                    binary.Serialize(user.Stream, sendMessage);
                    user.Stream.Flush();
                    break;
                }
            }
        }

        public void SendMessageToAllButOne(AMessage sendMessage, int ClientNumber)
        {
            sendMessage.Type = MessageType.INFO;
            foreach (Client user in userList)
            {
                if (user.ClientNumber != ClientNumber)
                {
                    binary.Serialize(user.Stream, sendMessage);
                    user.Stream.Flush();
                    break;
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

        private class Client
        {
            public TcpClient tcpClient;
            public NetworkStream Stream;
            public Thread Thread;
            public int ClientNumber;
            public bool Connected;

            public Client(TcpClient client, NetworkStream stream, int clientNumber)
            {
                this.tcpClient = client;
                this.Stream = stream;
                this.ClientNumber = clientNumber;
                this.Connected = true;
            }

            ~Client()
            {
                Connected = false;
                tcpClient.Close();
                Stream.Close();
                Thread.Abort();
                Thread.Join();
            }
        }

        public string IPAddressValue
        {
            get { return ipAddressValue; }
        }

        public int PortNumber
        {
            get { return portNumber; }
        }

        public Queue<string> MessageQueue
        {
            get { return messageQueue; }
        }
    }
}
