using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// File: Core/Services/ServerService.cs
namespace PetitionD.Core.Services;

public class ServerService(
    GmService gmService,
    WorldService worldService,
    NoticeService noticeService,
    ILogger<ServerService> logger) : IDisposable
{
    private readonly GmService _gmService = gmService;
    private readonly WorldService _worldService = worldService;
    private readonly NoticeService _noticeService = noticeService;
    private readonly ILogger<ServerService> _logger = logger;
    private bool _isRunning;

    public void Start()
    {
        if (_isRunning)
            return;

        try
        {
            _gmService.Start();
            _worldService.Start();
            _noticeService.Start();
            _isRunning = true;
            _logger.LogInformation("Server services started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start server services");
            Stop();
            throw;
        }
    }

    public void Stop()
    {
        if (!_isRunning)
            return;

        try
        {
            _gmService.Stop();
            _worldService.Stop();
            _noticeService.Stop();
            _isRunning = false;
            _logger.LogInformation("Server services stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during server shutdown");
            throw;
        }
    }

    public void Dispose()
    {
        Stop();
    }

    public bool IsRunning => _isRunning;
}
