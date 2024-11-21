// File: Infrastructure/Network/NetworkService.cs
using NC.ToolNet.Net.Server;
using System.Net;

namespace PetitionD.Infrastructure.Network;

public class NetworkService(
    int port,
    ISessionManager sessionManager,
    ILogger<NetworkService> logger) : INetworkBase
{
    private readonly ListenerSocket _listener = new();
    private bool _isRunning;

    public bool IsRunning => _isRunning;

    public void Start()
    {
        try
        {
            _listener.Start(new IPEndPoint(IPAddress.Any, port));
            _isRunning = true;
            logger.LogInformation("Network service started on port {Port}", port);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start network service on port {Port}", port);
            throw;
        }
    }

    public void Stop()
    {
        try
        {
            foreach (var session in sessionManager.GetAllSessions())
            {
                session.Stop();
            }
            _listener.Stop();
            _isRunning = false;
            logger.LogInformation("Network service stopped");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to stop network service");
            throw;
        }
    }
}