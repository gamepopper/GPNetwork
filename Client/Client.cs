using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using NetworkMessage;


namespace GPNetworkClient
{
    class NotConnectedException : Exception
    {
        public NotConnectedException()
            : base("TcpClient not connected.")
        { }

        public NotConnectedException(string message)
            : base(message)
        { }
    }

    public class Client
    {
        public TcpClient Client;
        public NetworkStream Stream;
        public Thread Thread;
        private BinaryFormatter binary = new BinaryFormatter();
        private AMessage lastMessage;

        public int ClientNumber = 0;
        public bool isConnected = false;

        public Client() { }

        public bool Connect(string hostname, int port)
        {
            try
            {
                Client = new TcpClient(hostname, port);
                Stream = Client.GetStream();

                AMessage recieveMessage = binary.Deserialize(Stream) as AMessage;
                if (recieveMessage != null)
                {
                    if (recieveMessage.Type == MessageType.START)
                    {
                        ClientNumber = recieveMessage.ID;
                        Console.WriteLine("Start the chat now.");
                    }
                }
                Thread = new Thread(RecieveMessages);
                Thread.Start();

                isConnected = true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
                Client.Close();
                Thread.Abort();
                Thread.Join();
                return false;
            }

            return true;
        }

        public void RecieveMessages()
        {
            if (!Client.Connected)
                throw new NotConnectedException();

            try
            {
                while (isConnected)
                {
                    if (Stream.DataAvailable)
                    {
                        AMessage recieveMessage = binary.Deserialize(Stream) as AMessage;

                        if (recieveMessage != null)
                        {
                            lastMessage = recieveMessage;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected Error: " + e.Message);
            }
        }

        public void SendMessage(AMessage sendMessage)
        {
            if (sendMessage.Message != "")
            {
                sendMessage.ID = ClientNumber;
                binary.Serialize(Stream, sendMessage);
            }
        }

        public void Disconnect()
        {
            SendMessage(new AMessage(MessageType.QUIT, ClientNumber, "Client has left the server" ));
            isConnected = false;
            Thread.Abort();
            Thread.Join();
            Client.Close();
        }

        public AMessage LastMessage
        {
            get
            {
                return lastMessage;
            }
            set
            {
                lastMessage = value;
            }
        }
    }
}
