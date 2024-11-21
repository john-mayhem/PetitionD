// File: Infrastructure/Network/Sessions/BaseSession.cs
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using NC.ToolNet.Net.Server;

namespace PetitionD.Infrastructure.Network.Sessions;

public abstract class BaseSession(ILogger logger) : ASocket, ISession
{
    protected readonly ILogger Logger = logger;  // Store logger as a field
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
}