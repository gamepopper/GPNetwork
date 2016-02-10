using GPNetworkMessage;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

/*
 * Written by Tim Stoddard
 * Multiplayer Games & Software Engineering
 * Staffordshire University
 */

namespace GPNetworkClient
{
    public class NotConnectedException : Exception
    {
        public NotConnectedException()
            : base("TcpClient not connected.")
        { }

        public NotConnectedException(string message)
            : base(message)
        { }
    }

    public class TCPClient : IClient
    {
        private TcpClient client;
        private NetworkStream stream;
        private Thread thread;
        private BinaryFormatter binary = new BinaryFormatter();

        private int clientAmount = 1;
        private int clientID = 0;
        private bool isConnected = false;
        private Queue<AMessage> messageQueue = new Queue<AMessage>();

        public TCPClient() { }

        public bool Connect(string hostname, int port, string message="Client has connected")
        {
            try
            {
                client = new TcpClient(hostname, port);
                client.Client.ReceiveTimeout = TimeSpan.FromSeconds(10).Seconds;
                stream = client.GetStream();

                AMessage recieveMessage = ReceiveMessage();

                if (recieveMessage != null)
                {
                    if (recieveMessage.Type == MessageType.JOIN)
                    {
                        clientID = recieveMessage.ID;
                    }
                }
                else
                {
                    client.Close();
                    return false;
                }

                SendMessage(MessageType.JOIN, message);

                thread = new Thread(MessageReceiver);
                thread.Start();

                isConnected = true;
            }
            catch (Exception e)
            {
                messageQueue.Enqueue(new AMessage(MessageType.ERROR, clientID, e.Message + ": " + e.Source));
                if (client != null) client.Close();
                if (thread != null) thread.Abort();
                if (thread != null) thread.Join();
                return false;
            }

            return true;
        }

        public AMessage ReceiveMessage()
        {
            return binary.Deserialize(stream) as AMessage;
        }

        public void MessageReceiver()
        {
            if (!client.Connected)
                throw new NotConnectedException();

            try
            {
                while (isConnected)
                {
                    if (stream.DataAvailable)
                    {
                        AMessage recieveMessage = ReceiveMessage();

                        if (recieveMessage != null)
                        {
                            if (recieveMessage.Type == MessageType.CLIENTCOUNT) clientAmount = Convert.ToInt32(recieveMessage.Message);
                            messageQueue.Enqueue(recieveMessage);
                        }
                        stream.Flush();
                    }
                }
            }
            catch (Exception e)
            {
                messageQueue.Enqueue(new AMessage(MessageType.ERROR, clientID, e.Message + ": " + e.Source));
            }
        }

        public void SendMessage(MessageType type, string data)
        {
            if (data.Length > 0)
            {
                AMessage sendMessage = new AMessage(type, clientID, data);
                binary.Serialize(stream, sendMessage);
            }
        }

        public void SendMessageToOne(string data, int ClientID)
        {
            if (data.Length > 0)
            {
                AMessage sendMessage = new AMessage(MessageType.INFOTO1, ClientID, ClientID + ";" + data);
                binary.Serialize(stream, sendMessage);
            }
        }

        public void SendMessageExceptOne(string data, int ClientID)
        {
            if (data.Length > 0)
            {
                AMessage sendMessage = new AMessage(MessageType.INFOEXCEPT1, ClientID, ClientID + ";" + data);
                binary.Serialize(stream, sendMessage);
            }
        }

        public void Disconnect(string message = "Client Disconnected")
        {
            if (stream != null)
            {
                SendMessage(MessageType.LEAVE, message);
                messageQueue.Enqueue(new AMessage(MessageType.LEAVE, clientID, message));
            }

            isConnected = false;

            if (thread != null)
            {
                thread.Abort();
                thread.Join();
            }

            if (client != null)
            {
                client.Close();
            }
        }

        public int ClientID
        {
            get { return clientID; }
        }

        public int ClientAmount
        {
            get { return clientAmount; }
        }

        public bool IsConnected
        {
            get { return isConnected; }
        }

        public Queue<AMessage> Messages
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

