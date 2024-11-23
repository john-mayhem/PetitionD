// File: Infrastructure/Network/Sessions/ISession.cs
using NC.ToolNet.Net.Server;
using System.Net;
using System.Net.Sockets;

namespace PetitionD.Infrastructure.Network.Sessions;

public interface ISession
{
    string Id { get; }
    bool IsConnected { get; }
    void Start(Socket socket);
    void Stop();
    void Send(byte[] data);
    EndPoint? RemoteEndPoint { get; }
    event Action<byte[], bool> PacketLogged;
}