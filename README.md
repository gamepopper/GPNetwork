# GPNetwork
These is the Server and Client libraries I wrote last year for a University module involving Multiplayer Games and Network Programming.

The libraries are demonstrated here:
[Knock Out - Multiplayer Games and Software Engineering](https://www.youtube.com/watch?v=YAqeyxuENik)

The intention was to create an abstract approach that would mean either **Transmission Control Protocol (TCP)** or **User Datagram Protocol (UDP)** communication between network and server could be used with very little change in code. Despite being fully interchangeable (the chat client shown in the video works for both TCP and UDP networks), you cannot allow a server and client of different protocols to communicate with each other.

Messages are sent between the client and server via the AMessage abstract class, this is done in TCP using Serialization and UDP by sending the data as a string in the form of bytes.

## Usage
To use it, first you create an application that sets up a server. This is done in the constructor, and starting the server using one of the classes that implements the IServer interface with the Start() function:

```
//server = new TCPServer(doLoopBack, portNumber); //Use this if you want the program to find an IP Address or use the default local IP Address
server = new TCPServer(ipAddressString, portNumber);
server.Start();
```

Then in an application for the client, you set up a class with an IClient interface, then call the Connect function, passing in an IP/Host Address, Port Number and a message to indicate the client connecting to the server (default is "Client has connected").

```
client.Connect("127.0.0.1", 123, "User has joined the chat");
```
Assuming the server is running and the clients are connected to it successfully, clients should be able to send messages to each other. This is done by setting up an AMessage object with data in the form of a string. You can set this message to be sent to all clients, any clients except one or one client only by defining the message type.

```
client.SendMessageExceptOne("Name;" + username, Client.ClientID);
client.SendMessageToOne("Name;" + username, Client.ClientID);
client.SendMessage(MessageType.INFO, username + ": " + SendMessageText.Text);
```

The framework can only send and recieve messages in the format of strings, this is mostly because I couldn't get sending serialized objects working correctly at the time. Despite that it is possible to send infomation like numbers and booleans by converting them into strings. For Knock Out, I sent movement and positional data for objects by combining them into a string separated by semi-colons.

## Conclusion

Obviously, this isn't the only way to write a Server and Client in C#, nor is it the only way to approach writing a TCP or UDP network. These libraries were written with intention for anyone to understand an implementation, and for those to critique the implementation on their own whims.
