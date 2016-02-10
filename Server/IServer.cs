using System;
using System.Collections.Generic;
namespace GPNetworkServer
{
    public interface IServer
    {
        string IPAddressValue { get; }
        int PortNumber { get; }
        Queue<string> MessageQueue { get; }
        void SendMessageToAll(GPNetworkMessage.AMessage sendMessage);
        void SendMessageToAllButOne(GPNetworkMessage.AMessage sendMessage, int ClientNumber);
        void SendMessageToOne(GPNetworkMessage.AMessage sendMessage, int ClientNumber);
        void Start();
        void Stop();
    }
}
