using GPNetworkMessage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

/*
 * Written by Tim Stoddard
 * Multiplayer Games & Software Engineering
 * Staffordshire University
 */

namespace GPNetworkServer
{
    public class UDPServer : IServer
    {
        private string ipAddressValue = "";
        private int portNumber = 0;
        private Queue<string> messageQueue = new Queue<string>();

        private UdpClient listener = null;
        private List<Client> userList = new List<Client>();
        private Thread listenThread = null;
        private bool isListening = false;

        public UDPServer() { }

        public UDPServer(string hostname, int port)
        {
            ipAddressValue = hostname;
            portNumber = port;
            listener = new UdpClient(new IPEndPoint(IPAddress.Parse(ipAddressValue), portNumber));
            listenThread = new Thread(ListenMethod);
        }

        public UDPServer(bool UseLoopback, int port)
        {
            IPAddress ipAddress = UseLoopback ? IPAddress.Loopback : LocalIPAddress();
            ipAddressValue = ipAddress.ToString();
            portNumber = port;
            listener = new UdpClient(new IPEndPoint(IPAddress.Parse(ipAddressValue), portNumber));
            listenThread = new Thread(ListenMethod);
        }

        public void SendMessageToAll(AMessage sendMessage)
        {
            if (sendMessage.Type == MessageType.INFOEXCEPT1 || sendMessage.Type == MessageType.INFOTO1) sendMessage.Type = MessageType.INFO;

            string messageString = sendMessage.ID + ";" + sendMessage.Type.ToString() + ";" + sendMessage.Message;
            Byte[] sendData = Encoding.ASCII.GetBytes(messageString);

            foreach (Client user in userList)
            {
                listener.Send(sendData, sendData.Length, user.endPoint);
            }
        }

        public void SendMessageToAllButOne(AMessage sendMessage, int ClientNumber)
        {
            sendMessage.Type = MessageType.INFO;

            string messageString = sendMessage.ID + ";" + sendMessage.Type.ToString() + ";" + sendMessage.Message;
            Byte[] sendData = Encoding.ASCII.GetBytes(messageString);

            foreach (Client user in userList)
            {
                if (ClientNumber != user.ClientNumber) listener.Send(sendData, sendData.Length, user.endPoint);
            }
        }

        public void SendMessageToOne(AMessage sendMessage, int ClientNumber)
        {
            sendMessage.Type = MessageType.INFO;

            string messageString = sendMessage.ID + ";" + sendMessage.Type.ToString() + ";" + sendMessage.Message;
            Byte[] sendData = Encoding.ASCII.GetBytes(messageString);

            foreach (Client user in userList)
            {
                if (user.ClientNumber == ClientNumber)
                {
                    listener.Send(sendData, sendData.Length, user.endPoint);
                    break;
                }
            }
        }

        public void Start()
        {
            isListening = true;

            if (listenThread != null && listenThread.IsAlive)
            {
                if (listenThread != null)
                {
                    listenThread.Abort();
                    listenThread.Join();
                }
            }
            else
            {
                listenThread.Start();
            }
        }

        public void Stop()
        {
            SendMessageToAll(new AMessage(MessageType.LEAVE, -1, "Server disconnected"));
            isListening = false;
            userList.Clear();

            if (listener != null)
            {
                listener.Client.Shutdown(SocketShutdown.Both);
                listener.Client.Close();
                listener = null;
            }
        }

        private void ListenMethod()
        {
            try
            {
                while (isListening)
                {
                    //Recieve messages from anywhere
                    AMessage recievedMessage = new AMessage(MessageType.ANY, 0, "");

                    IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);
                    byte[] recievedData = listener.Receive(ref endpoint);

                    //If recievedData is null, it is assumed the connection is closed.
                    if (recievedData == null || recievedData.Length == 0)
                        return;

                    string message = Encoding.ASCII.GetString(recievedData);
                    string[] recievedString = message.Split(';');

                    recievedMessage.ID = Convert.ToInt32(recievedString[0]);
                    recievedMessage.Type = (MessageType)Enum.Parse(typeof(MessageType), recievedString[1]);
                    recievedMessage.Message = message.Substring((recievedString[0] + ";" + recievedString[1] + ";").Length);

                    if (recievedMessage.Type == MessageType.JOIN) //If MessageType is JOIN, set up new client
                    {
                        Client client = new Client(endpoint, AssignClientID(userList.Count + 1));

                        recievedMessage.ID = client.ClientNumber;
                        string messageString = recievedMessage.ID + ";" + recievedMessage.Type.ToString() + ";" + recievedMessage.Message;
                        Byte[] sendData = Encoding.ASCII.GetBytes(messageString);
                        listener.Send(sendData, sendData.Length, endpoint);

                        SendMessageToAll(recievedMessage);

                        userList.Add(client);

                        messageQueue.Enqueue("Client " + client.ClientNumber + " Connected");
                    }
                    else if (recievedMessage.Type == MessageType.LEAVE) //If Message is LEAVE, remove client and send to all
                    {
                        foreach (Client user in userList)
                        {
                            if (user.ClientNumber == recievedMessage.ID)
                            {
                                userList.Remove(user);
                                break;
                            }
                        }

                        messageQueue.Enqueue("Client " + recievedMessage.ID + " Removed");

                        SendMessageToAll(recievedMessage);
                    }
                    else if (recievedMessage.Type == MessageType.CLIENTCOUNT) //If Message is INFO, send to all
                    {
                        messageQueue.Enqueue("Client Count Request From: " + recievedMessage.ID);
                        recievedMessage.Message = "" + userList.Count;
                        SendMessageToAll(recievedMessage);
                    }
                    else if (recievedMessage.Type == MessageType.INFOTO1) //If Message is INFOTO1, send to one
                    {
                        string id = recievedMessage.Message.Split(';')[0];
                        recievedMessage.Message = recievedMessage.Message.Substring(id.Length + 1);

                        messageQueue.Enqueue("Client " + recievedMessage.ID + " - " + recievedMessage.Message + " - To: " + id);

                        SendMessageToOne(recievedMessage, Convert.ToInt32(id));
                    }
                    else if (recievedMessage.Type == MessageType.INFOEXCEPT1) //If Message is INFOEXCEPT1, send to all except one.
                    {
                        string id = recievedMessage.Message.Split(';')[0];
                        recievedMessage.Message = recievedMessage.Message.Substring(id.Length + 1);

                        messageQueue.Enqueue("Client " + recievedMessage.ID + " - " + recievedMessage.Message + " - Except: " + id);

                        SendMessageToAllButOne(recievedMessage, Convert.ToInt32(id));
                    }
                    else //If all else, send to every client.
                    {
                        messageQueue.Enqueue("Client " + recievedMessage.ID + " - " + recievedMessage.Message + " - To All");
                        SendMessageToAll(recievedMessage);
                    }
                }
            }
            catch (SocketException e)
            {
                if (isListening == true) messageQueue.Enqueue(e.Message + ": " + e.TargetSite);
            }
            finally
            {
                
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
            public IPEndPoint endPoint;
            public int ClientNumber;

            public Client(IPEndPoint endPoint, int clientNumber)
            {
                this.endPoint = endPoint;
                this.ClientNumber = clientNumber;
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

/*
 * The MIT License (MIT)
 * 
 * Copyright (c) 2015 Tim Stoddard
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * 
 */
