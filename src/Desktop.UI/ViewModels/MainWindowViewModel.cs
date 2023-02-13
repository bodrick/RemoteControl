using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Native.Linux;
using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Desktop.UI.Controls;
using Immense.RemoteControl.Desktop.UI.Services;
using Immense.RemoteControl.Desktop.UI.Views;
using Immense.RemoteControl.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Immense.RemoteControl.Desktop.UI.ViewModels;

public interface IMainWindowViewModel
{
    ICommand ChangeServerCommand { get; }

    ICommand CloseCommand { get; }

    ICommand CopyLinkCommand { get; }

    double CopyMessageOpacity { get; set; }

    string Host { get; set; }

    bool IsCopyMessageVisible { get; set; }

    ICommand MinimizeCommand { get; }

    ICommand OpenOptionsMenu { get; }

    ICommand RemoveViewersCommand { get; }

    string StatusMessage { get; set; }

    ObservableCollection<IViewer> Viewers { get; }

    Task ChangeServerAsync();

    Task CopyLinkAsync();

    Task GetSessionIDAsync();

    Task InitAsync();

    Task PromptForHostNameAsync();

    Task RemoveViewersAsync(AvaloniaList<object>? list);
}

public class MainWindowViewModel : BrandedViewModelBase, IMainWindowViewModel
{
    private readonly IAppState _appState;
    private readonly IAvaloniaDispatcher _dispatcher;
    private readonly IDesktopHubConnection _hubConnection;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly IScreenCaster _screenCaster;
    private readonly IViewModelFactory _viewModelFactory;
    private readonly IEnvironmentHelper _environment;

    public MainWindowViewModel(
      IBrandingProvider brandingProvider,
      IAvaloniaDispatcher dispatcher,
      IAppState appState,
      IDesktopHubConnection hubConnection,
      IScreenCaster screenCaster,
      IViewModelFactory viewModelFactory,
      IEnvironmentHelper environmentHelper,
      ILogger<MainWindowViewModel> logger)
      : base(brandingProvider, dispatcher, logger)
    {
        _dispatcher = dispatcher;
        _appState = appState;
        _hubConnection = hubConnection;
        _screenCaster = screenCaster;
        _viewModelFactory = viewModelFactory;
        _environment = environmentHelper;
        _logger = logger;

        _appState.ViewerRemoved += ViewerRemoved;
        _appState.ViewerAdded += ViewerAdded;
        _appState.ScreenCastRequested += ScreenCastRequested;

        ChangeServerCommand = new AsyncRelayCommand(ChangeServerAsync);
        CopyLinkCommand = new AsyncRelayCommand(CopyLinkAsync);
        RemoveViewersCommand = new AsyncRelayCommand<AvaloniaList<object>>(RemoveViewersAsync, CanRemoveViewers);
    }

    public ICommand ChangeServerCommand { get; }

    public ICommand CloseCommand { get; } = new RelayCommand<Window>(window =>
    {
        window?.Close();
        Environment.Exit(0);
    });

    public ICommand CopyLinkCommand { get; }

    public double CopyMessageOpacity
    {
        get => Get<double>();
        set => Set(value);
    }

    public string Host
    {
        get => Get<string>() ?? string.Empty;
        set => Set(value);
    }

    public bool IsCopyMessageVisible
    {
        get => Get<bool>();
        set => Set(value);
    }

    public ICommand MinimizeCommand { get; } = new RelayCommand<Window>(window =>
    {
        if (window is not null)
        {
            window.WindowState = WindowState.Minimized;
        }
    });

    public ICommand OpenOptionsMenu { get; } = new RelayCommand<Button>(button =>
    {
        button?.ContextMenu?.Open(button);
    });

    public ICommand RemoveViewersCommand { get; }

    public string StatusMessage
    {
        get => Get<string>() ?? string.Empty;
        set => Set(value);
    }

    public ObservableCollection<IViewer> Viewers { get; } = new();

    public async Task ChangeServerAsync()
    {
        await PromptForHostNameAsync();
        await InitAsync();
    }

    public async Task CopyLinkAsync()
    {
        if (_dispatcher.CurrentApp?.Clipboard is null)
        {
            return;
        }
        await _dispatcher.CurrentApp.Clipboard.SetTextAsync($"{Host}/RemoteControl/Viewer?sessionID={StatusMessage.Replace(" ", "")}");

        CopyMessageOpacity = 1;
        IsCopyMessageVisible = true;
        await Task.Delay(1000);
        while (CopyMessageOpacity > 0)
        {
            CopyMessageOpacity -= .05;
            await Task.Delay(25);
        }
        IsCopyMessageVisible = false;
    }

    public async Task GetSessionIDAsync()
    {
        var sessionId = await _hubConnection.GetSessionIDAsync();
        await _hubConnection.SendAttendedSessionInfoAsync(Environment.MachineName);

        var formattedSessionID = "";
        for (var i = 0; i < sessionId.Length; i += 3)
        {
            formattedSessionID += $"{sessionId.Substring(i, 3)} ";
        }

        await _dispatcher.InvokeAsync(() =>
        {
            StatusMessage = formattedSessionID.Trim();
        });
    }

    public async Task InitAsync()
    {
        if (!_environment.IsDebug && Libc.geteuid() != 0)
        {
            await MessageBox.ShowAsync("Please run with sudo.", "Sudo Required", MessageBoxType.OK);
            Environment.Exit(0);
        }

        StatusMessage = "Initializing...";

        await InstallDependenciesAsync();

        StatusMessage = "Retrieving...";

        Host = _appState.Host;

        while (string.IsNullOrWhiteSpace(Host))
        {
            Host = "https://";
            await PromptForHostNameAsync();
        }

        _appState.Host = Host;
        _appState.Mode = Shared.Enums.AppMode.Attended;

        try
        {
            var result = await _hubConnection.ConnectAsync(_dispatcher.AppCancellationToken, TimeSpan.FromSeconds(10));

            if (result && _hubConnection.Connection is not null)
            {
                _hubConnection.Connection.Closed += async (ex) =>
                {
                    await _dispatcher.InvokeAsync(() =>
                    {
                        Viewers.Clear();
                        StatusMessage = "Disconnected";
                    });
                };

                _hubConnection.Connection.Reconnecting += async (ex) =>
                {
                    await _dispatcher.InvokeAsync(() =>
                    {
                        Viewers.Clear();
                        StatusMessage = "Reconnecting";
                    });
                };

                _hubConnection.Connection.Reconnected += async (id) =>
                {
                    await GetSessionIDAsync();
                };
            }
            await ApplyBrandingAsync();

            await GetSessionIDAsync();

            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during initialization.");
        }

        // If we got here, something went wrong.
        StatusMessage = "Failed";
        await MessageBox.ShowAsync("Failed to connect to server.", "Connection Failed", MessageBoxType.OK);
    }

    public async Task PromptForHostNameAsync()
    {
        var viewModel = _viewModelFactory.CreateHostNamePromptViewModel();
        var prompt = new HostNamePrompt()
        {
            DataContext = viewModel
        };

        if (!string.IsNullOrWhiteSpace(Host))
        {
            viewModel.Host = Host;
        }

        await prompt.ShowDialog(_dispatcher.MainWindow);
        var result = prompt.ViewModel?.Host?.Trim()?.TrimEnd('/');

        if (!Uri.TryCreate(result, UriKind.Absolute, out var serverUri) ||
            (serverUri.Scheme != Uri.UriSchemeHttp && serverUri.Scheme != Uri.UriSchemeHttps))
        {
            _logger.LogWarning("Server URL is not valid.");
            await MessageBox.ShowAsync("Server URL must be a valid Uri (e.g. https://example.com).", "Invalid Server URL", MessageBoxType.OK);
            return;
        }

        Host = result;
    }

    public async Task RemoveViewersAsync(AvaloniaList<object>? list)
    {
        if (list is null)
        {
            return;
        }
        var viewerList = list ?? new AvaloniaList<object>();
        foreach (var viewer in viewerList.Cast<Viewer>())
        {
            await _hubConnection.DisconnectViewerAsync(viewer, true);
        }
    }

    private bool CanRemoveViewers(AvaloniaList<object>? obj) => obj?.Any() == true;

    private async Task InstallDependenciesAsync()
    {
        if (OperatingSystem.IsLinux())
        {
            try
            {
                var psi = new ProcessStartInfo()
                {
                    FileName = "sudo",
                    Arguments = "bash -c \"apt-get -y install libx11-dev ; " +
                        "apt-get -y install libxrandr-dev ; " +
                        "apt-get -y install libc6-dev ; " +
                        "apt-get -y install libgdiplus ; " +
                        "apt-get -y install libxtst-dev ; " +
                        "apt-get -y install xclip\"",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                };

                await Task.Run(() => Process.Start(psi)?.WaitForExit());
            }
            catch
            {
                _logger.LogError("Failed to install dependencies.");
            }
        }

    }

    private void ScreenCastRequested(object? sender, ScreenCastRequest screenCastRequest)
    {
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var result = await MessageBox.ShowAsync($"You've received a connection request from {screenCastRequest.RequesterName}.  Accept?", "Connection Request", MessageBoxType.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                _screenCaster.BeginScreenCasting(screenCastRequest);
            }
        });
    }

    private async void ViewerAdded(object? sender, IViewer viewer)
    {
        await _dispatcher.InvokeAsync(() =>
        {
            Viewers.Add(viewer);
        });
    }

    private async void ViewerRemoved(object? sender, string viewerID)
    {
        await _dispatcher.InvokeAsync(() =>
        {
            var viewer = Viewers.FirstOrDefault(x => x.ViewerConnectionID == viewerID);
            if (viewer != null)
            {
                Viewers.Remove(viewer);
            }
        });
    }
}
