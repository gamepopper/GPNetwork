# GPNetwork
These is the Server and Client libraries I wrote last year for a University module involving Multiplayer Games and Network Programming.

The libraries are demonstrated here:
[Knock Out - Multiplayer Games and Software Engineering](https://www.youtube.com/watch?v=YAqeyxuENik)

The intention was to create an abstract approach that would mean either **Transmission Control Protocol (TCP)** or **User Datagram Protocol (UDP)** communication between network and server could be used with very little change in code. Despite being fully interchangeable (the chat client shown in the video works for both TCP and UDP networks), you cannot allow a server and client of different protocols to communicate with each other.

Messages are sent between the client and server via the AMessage abstract class, this is done in TCP using Serialization and UDP by sending the data as a string in the form of bytes.

Obviously, this isn't the only way to write a Server and Client in C#, nor is it the only way to approach writing a TCP or UDP network. These libraries were written with intention for anyone to understand an implementation, and for those to critique the implementation on their own whims.
