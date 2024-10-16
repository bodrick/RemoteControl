﻿using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Enums;
using Immense.RemoteControl.Desktop.Shared.Native.Win32;
using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Immense.RemoteControl.Desktop.UI.Services;

internal class AppStartup : IAppStartup
{
    private readonly IAppState _appState;
    private readonly IKeyboardMouseInput _inputService;
    private readonly IDesktopHubConnection _desktopHub;
    private readonly IClipboardService _clipboardService;
    private readonly IChatHostService _chatHostService;
    private readonly ICursorIconWatcher _cursorIconWatcher;
    private readonly IAvaloniaDispatcher _dispatcher;
    private readonly IIdleTimer _idleTimer;
    private readonly IShutdownService _shutdownService;
    private readonly ILogger<AppStartup> _logger;

    public AppStartup(
        IAppState appState,
        IKeyboardMouseInput inputService,
        IDesktopHubConnection desktopHub,
        IClipboardService clipboardService,
        IChatHostService chatHostService,
        ICursorIconWatcher iconWatcher,
        IAvaloniaDispatcher dispatcher,
        IIdleTimer idleTimer,
        IShutdownService shutdownService,
        ILogger<AppStartup> logger)
    {
        _appState = appState;
        _inputService = inputService;
        _desktopHub = desktopHub;
        _clipboardService = clipboardService;
        _chatHostService = chatHostService;
        _cursorIconWatcher = iconWatcher;
        _dispatcher = dispatcher;
        _idleTimer = idleTimer;
        _shutdownService = shutdownService;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        if (_appState.Mode is AppMode.Unattended or AppMode.Attended)
        {
            _clipboardService.BeginWatching();
            _inputService.Init();
            _cursorIconWatcher.OnChange += CursorIconWatcher_OnChange;
        }

        switch (_appState.Mode)
        {
            case AppMode.Unattended:
                _dispatcher.StartUnattended();
                await StartScreenCastingAsync().ConfigureAwait(false);
                break;
            case AppMode.Attended:
                _dispatcher.StartAttended();
                break;
            case AppMode.Chat:
                await _chatHostService
                    .StartChatAsync(_appState.PipeName, _appState.OrganizationName)
                    .ConfigureAwait(false);
                break;
            default:
                break;
        }
    }

    private async Task StartScreenCastingAsync()
    {
        if (!await _desktopHub.ConnectAsync(_dispatcher.AppCancellationToken, TimeSpan.FromSeconds(30)))
        {
            await _shutdownService.ShutdownAsync();
            return;
        }

        var result = await _desktopHub.SendUnattendedSessionInfoAsync(
            _appState.SessionId,
            _appState.AccessKey,
            Environment.MachineName,
            _appState.RequesterName,
            _appState.OrganizationName);

        if (!result.IsSuccess)
        {
            _logger.LogError(result.Exception, "An error occurred while trying to establish a session with the server.");
            await _shutdownService.ShutdownAsync();
            return;
        }

        if (Win32Interop.GetCurrentDesktop(out var currentDesktopName))
        {
            _logger.LogInformation("Setting initial desktop to {currentDesktopName}.", currentDesktopName);
        }
        else
        {
            _logger.LogWarning("Failed to get initial desktop name.");
        }

        if (!Win32Interop.SwitchToInputDesktop())
        {
            _logger.LogWarning("Failed to set initial desktop.");
        }

        if (_appState.ArgDict.ContainsKey("relaunch"))
        {
            _logger.LogInformation("Resuming after relaunch.");
            var viewersString = _appState.ArgDict["viewers"];
            var viewerIDs = viewersString.Split(",".ToCharArray());
            await _desktopHub.NotifyViewersRelaunchedScreenCasterReadyAsync(viewerIDs);
        }
        else
        {
            await _desktopHub.NotifyRequesterUnattendedReadyAsync();
        }

        _idleTimer.Start();
    }

    private async void CursorIconWatcher_OnChange(object? sender, CursorInfo cursor)
    {
        if (_appState?.Viewers?.Any() == true)
        {
            foreach (var viewer in _appState.Viewers.Values)
            {
                await viewer.SendCursorChangeAsync(cursor);
            }
        }
    }
}
