// File: Infrastructure/Network/Sessions/BaseSession.cs
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using NC.ToolNet.Networking.Server;

namespace PetitionD.Infrastructure.Network.Sessions;

public abstract class BaseSession : ASocket, ISession
{
    protected readonly ILogger Logger;

    protected BaseSession(ILogger logger) : base()
    {
        Logger = logger;
    }

    public string Id { get; } = Guid.NewGuid().ToString();
    public bool IsConnected { get; private set; }

    public void Start(Socket socket)
    {
        try
        {
            BeginReceive(socket);
            IsConnected = true;
            OnSessionStarted();
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to start session: {Message}", ex.Message);
            throw;
        }
    }

    public new void Stop()
    {
        try
        {
            base.Stop();
            IsConnected = false;
            OnSessionStopped();
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to stop session: {Message}", ex.Message);
            throw;
        }
    }

    protected sealed override void OnConnectFailed(Exception e)
    {
        Logger.LogError("Connection failed: {Message}", e.Message);
        IsConnected = false;
    }

    protected sealed override void OnConnected()
    {
        Logger.LogInformation("Session {Id} connected", Id);
        IsConnected = true;
    }

    protected sealed override void OnDisconnected(Exception e)
    {
        Logger.LogInformation("Session {Id} disconnected: {Message}", Id, e.Message);
        IsConnected = false;
        OnSessionStopped();
    }

    protected virtual void OnSessionStarted() { }
    protected virtual void OnSessionStopped() { }

    protected override void OnReceived(byte[] packet)
    {
        try
        {
            PacketLogged?.Invoke(packet, false); // Log incoming packet
            HandlePacket(packet);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error handling received packet");
        }
    }

    public override void Send(byte[] data)
    {
        try
        {
            PacketLogged?.Invoke(data, true); // Log outgoing packet
            base.Send(data);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error sending packet");
        }
    }

    public event Action<byte[], bool> PacketLogged = delegate { };  // Initialize with empty delegate

    protected void OnPacketLogged(byte[] data, bool isOutgoing)
    {
        PacketLogged?.Invoke(data, isOutgoing);
    }

    protected abstract void HandlePacket(byte[] packet);
    public new EndPoint? RemoteEndPoint => base.RemoteEndPoint;

}