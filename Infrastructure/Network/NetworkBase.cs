// File: Infrastructure/Network/NetworkBase.cs
using NC.ToolNet.Net.Server;
using System.Net;
using System.Net.Sockets;

namespace PetitionD.Infrastructure.Network;

public abstract class NetworkBase : INetworkService
{
    protected readonly int Port;
    protected readonly ListenerSocket Listener;
    protected bool IsActive;

    protected NetworkBase(int port)
    {
        Port = port;
        Listener = new ListenerSocket();
        Listener.Accepted += OnSocketAccepted;
    }

    public bool IsRunning => IsActive;

    public virtual void Start()
    {
        try
        {
            Listener.Start(new IPEndPoint(IPAddress.Any, Port));
            IsActive = true;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to start network service on port {Port}: {ex.Message}");
        }
    }

    public virtual void Stop()
    {
        try
        {
            Listener.Stop();
            IsActive = false;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to stop network service: {ex.Message}");
        }
    }

    protected abstract void OnSocketAccepted(ListenerSocket listener, Socket socket);
}