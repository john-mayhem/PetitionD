// File: Infrastructure/Network/INetworkBase.cs
using System.Net.Sockets;
using NC.ToolNet.Networking.Server;

namespace PetitionD.Infrastructure.Network;

public interface INetworkBase
{
    void Start();
    void Stop();
    bool IsRunning { get; }
}