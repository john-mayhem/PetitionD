// File: Infrastructure/Network/INetworkBase.cs
using System.Net.Sockets;
using NC.ToolNet.Net.Server;

namespace PetitionD.Infrastructure.Network;

public interface INetworkService
{
    void Start();
    void Stop();
    bool IsRunning { get; }
}