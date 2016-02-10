using System;
using System.Collections.Generic;
using GPNetworkMessage;

namespace GPNetworkClient
{
    public interface IClient
    {
        int ClientID { get; }
        int ClientAmount { get; }
        bool IsConnected { get; }
        Queue<AMessage> Messages { get; }
        bool Connect(string hostname, int port, string message = "Client has connected");
        void Disconnect(string message = "Client Disconnected");
        AMessage ReceiveMessage();
        void MessageReceiver();
        void SendMessage(GPNetworkMessage.MessageType type, string data);
        void SendMessageExceptOne(string data, int ClientID);
        void SendMessageToOne(string data, int ClientID);
    }
}
