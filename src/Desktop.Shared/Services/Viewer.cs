using System.Collections.Concurrent;
using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Shared.Models;
using Microsoft.Extensions.Logging;
using Immense.RemoteControl.Shared.Helpers;
using Immense.RemoteControl.Shared.Models.Dtos;
using Immense.RemoteControl.Desktop.Shared.ViewModels;
using Microsoft.AspNetCore.SignalR.Client;
using Immense.RemoteControl.Desktop.Shared.Native.Win32;
using Immense.RemoteControl.Shared.Services;

namespace Immense.RemoteControl.Desktop.Shared.Services;

public interface IViewer : IDisposable
{
    IScreenCapturer Capturer { get; }

    double CurrentFps { get; }

    double CurrentMbps { get; }

    bool DisconnectRequested { get; set; }

    bool HasControl { get; set; }

    int ImageQuality { get; }

    bool IsConnected { get; }

    bool IsStalled { get; }

    string Name { get; set; }

    ConcurrentQueue<SentFrame> PendingSentFrames { get; }

    TimeSpan RoundTripLatency { get; }

    string ViewerConnectionID { get; set; }

    void ApplyAutoQuality();

    void CalculateFps();

    void DequeuePendingFrame();

    Task SendAudioSampleAsync(byte[] audioSample);

    Task SendClipboardTextAsync(string clipboardText);

    Task SendCursorChangeAsync(CursorInfo cursorInfo);

    Task SendDesktopStreamAsync(IAsyncEnumerable<byte[]> asyncEnumerable, Guid streamId);

    Task SendFileAsync(FileUpload fileUpload, CancellationToken cancelToken, Action<double> progressUpdateCallback);

    Task SendScreenCaptureAsync(ScreenCaptureDto screenCapture);

    Task SendScreenDataAsync(string selectedDisplay, IEnumerable<string> displayNames, int screenWidth, int screenHeight);

    Task SendScreenSizeAsync(int width, int height);

    Task SendViewerConnectedAsync();

    Task SendWindowsSessionsAsync();
}

public class Viewer : IViewer
{
    public const int DefaultQuality = 80;

    private readonly IAudioCapturer _audioCapturer;
    private readonly IClipboardService _clipboardService;
    private readonly IDesktopHubConnection _desktopHubConnection;
    private readonly ConcurrentQueue<DateTimeOffset> _fpsQueue = new();
    private readonly ILogger<Viewer> _logger;
    private readonly ConcurrentQueue<SentFrame> _receivedFrames = new();
    private readonly ISystemTime _systemTime;

    public Viewer(
        IDesktopHubConnection casterSocket,
        IScreenCapturer screenCapturer,
        IClipboardService clipboardService,
        IAudioCapturer audioCapturer,
        ISystemTime systemTime,
        ILogger<Viewer> logger)
    {
        Capturer = screenCapturer;
        _desktopHubConnection = casterSocket;
        _clipboardService = clipboardService;
        _audioCapturer = audioCapturer;
        _systemTime = systemTime;
        _logger = logger;

        _clipboardService.ClipboardTextChanged += ClipboardService_ClipboardTextChanged;
        _audioCapturer.AudioSampleReady += AudioCapturer_AudioSampleReady;
    }

    public IScreenCapturer Capturer { get; }

    public double CurrentFps { get; private set; }

    public double CurrentMbps { get; private set; }

    public bool DisconnectRequested { get; set; }

    public bool HasControl { get; set; } = true;

    public int ImageQuality { get; private set; } = DefaultQuality;

    public bool IsConnected => _desktopHubConnection.IsConnected;

    public bool IsStalled
    {
        get
        {
            return PendingSentFrames.TryPeek(out var result) && DateTimeOffset.Now - result.Timestamp > TimeSpan.FromSeconds(15);
        }
    }

    public string Name { get; set; } = string.Empty;

    public ConcurrentQueue<SentFrame> PendingSentFrames { get; } = new();

    public TimeSpan RoundTripLatency { get; private set; }

    public string ViewerConnectionID { get; set; } = string.Empty;

    public void ApplyAutoQuality()
    {
        if (ImageQuality < DefaultQuality)
        {
            ImageQuality = Math.Min(DefaultQuality, ImageQuality + 2);
        }

        // Limit FPS.
        _ = WaitHelper.WaitFor(() =>
            !PendingSentFrames.TryPeek(out var result) || DateTimeOffset.Now - result.Timestamp > TimeSpan.FromMilliseconds(50),
            TimeSpan.FromSeconds(5));

        // Delay based on roundtrip time to prevent too many frames from queuing up on slow connections.
        _ = WaitHelper.WaitFor(() => PendingSentFrames.Count < 1 / RoundTripLatency.TotalSeconds,
            TimeSpan.FromSeconds(5));

        // Wait until oldest pending frame is within the past 1 second.
        _ = WaitHelper.WaitFor(() =>
            !PendingSentFrames.TryPeek(out var result) || DateTimeOffset.Now - result.Timestamp < TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(5));
    }

    public void CalculateFps()
    {
        _fpsQueue.Enqueue(_systemTime.Now);

        while (_fpsQueue.TryPeek(out var oldestTime) &&
            _systemTime.Now - oldestTime > TimeSpan.FromSeconds(1))
        {
            _fpsQueue.TryDequeue(out _);
        }

        CurrentFps = _fpsQueue.Count;
    }

    public void DequeuePendingFrame()
    {
        if (PendingSentFrames.TryDequeue(out var frame))
        {
            RoundTripLatency = _systemTime.Now - frame.Timestamp;
            _receivedFrames.Enqueue(new SentFrame(frame.FrameSize, _systemTime.Now));
        }

        while (_receivedFrames.TryPeek(out var oldestFrame) &&
            _systemTime.Now - oldestFrame.Timestamp > TimeSpan.FromSeconds(1))
        {
            _receivedFrames.TryDequeue(out _);
        }

        CurrentMbps = (double)_receivedFrames.Sum(x => x.FrameSize) / 1024 / 1024 * 8;
    }

    public void Dispose()
    {
        DisconnectRequested = true;
        Disposer.TryDisposeAll(Capturer);
        GC.SuppressFinalize(this);
    }

    public async Task SendAudioSampleAsync(byte[] audioSample)
    {
        var dto = new AudioSampleDto(audioSample);
        await TrySendToViewerAsync(dto, DtoType.AudioSample, ViewerConnectionID);
    }

    public async Task SendClipboardTextAsync(string clipboardText)
    {
        var dto = new ClipboardTextDto(clipboardText);
        await TrySendToViewerAsync(dto, DtoType.ClipboardText,ViewerConnectionID);
    }

    public async Task SendCursorChangeAsync(CursorInfo cursorInfo)
    {
        if (cursorInfo is null)
        {
            return;
        }

        var dto = new CursorChangeDto(cursorInfo.ImageBytes, cursorInfo.HotSpot.X, cursorInfo.HotSpot.Y, cursorInfo.CssOverride);
        await TrySendToViewerAsync(dto, DtoType.CursorChange, ViewerConnectionID);
    }

    public async Task SendDesktopStreamAsync(IAsyncEnumerable<byte[]> stream, Guid streamId)
    {
        if (_desktopHubConnection.Connection is not null)
        {
            await _desktopHubConnection.Connection.SendAsync("SendDesktopStream", stream, streamId);
        }
    }

    public async Task SendFileAsync(FileUpload fileUpload, CancellationToken cancelToken, Action<double> progressUpdateCallback)
    {
        try
        {
            var messageId = Guid.NewGuid().ToString();
            var fileDto = new FileDto()
            {
                EndOfFile = false,
                FileName = fileUpload.DisplayName,
                MessageId = messageId,
                StartOfFile = true
            };

            await TrySendToViewerAsync(fileDto, DtoType.File, ViewerConnectionID);

            using var fs = File.OpenRead(fileUpload.FilePath);
            using var br = new BinaryReader(fs);
            while (fs.Position < fs.Length)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    return;
                }

                fileDto = new FileDto()
                {
                    Buffer = br.ReadBytes(40_000),
                    FileName = fileUpload.DisplayName,
                    MessageId = messageId
                };

                await TrySendToViewerAsync(fileDto, DtoType.File, ViewerConnectionID);

                progressUpdateCallback((double)fs.Position / fs.Length);
            }

            fileDto = new FileDto()
            {
                EndOfFile = true,
                FileName = fileUpload.DisplayName,
                MessageId = messageId,
                StartOfFile = false
            };

            await TrySendToViewerAsync(fileDto, DtoType.File, ViewerConnectionID);

            progressUpdateCallback(1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending file.");
        }
    }

    public async Task SendScreenCaptureAsync(ScreenCaptureDto screenCapture)
    {
        PendingSentFrames.Enqueue(new SentFrame(screenCapture.ImageBytes.Length, _systemTime.Now));

        await TrySendToViewerAsync(screenCapture, DtoType.ScreenCapture, ViewerConnectionID);
    }

    public async Task SendScreenDataAsync(
        string selectedDisplay,
        IEnumerable<string> displayNames,
        int screenWidth,
        int screenHeight)
    {
        var dto = new ScreenDataDto()
        {
            MachineName = Environment.MachineName,
            DisplayNames = displayNames,
            SelectedDisplay = selectedDisplay,
            ScreenWidth = screenWidth,
            ScreenHeight = screenHeight
        };
        await TrySendToViewerAsync(dto, DtoType.ScreenData, ViewerConnectionID);
    }

    public async Task SendScreenSizeAsync(int width, int height)
    {
        var dto = new ScreenSizeDto(width, height);
        await TrySendToViewerAsync(dto, DtoType.ScreenSize, ViewerConnectionID);
    }

    public async Task SendViewerConnectedAsync()
    {
        await _desktopHubConnection.SendViewerConnectedAsync(ViewerConnectionID);
    }

    public async Task SendWindowsSessionsAsync()
    {
        if (OperatingSystem.IsWindows())
        {
            var dto = new WindowsSessionsDto(Win32Interop.GetActiveSessions());
            await TrySendToViewerAsync(dto, DtoType.WindowsSessions, ViewerConnectionID);
        }
    }

    private async void AudioCapturer_AudioSampleReady(object? sender, byte[] sample)
    {
        await SendAudioSampleAsync(sample);
    }

    private async void ClipboardService_ClipboardTextChanged(object? sender, string clipboardText)
    {
        await SendClipboardTextAsync(clipboardText);
    }

    private async Task TrySendToViewerAsync<T>(T dto, DtoType type, string viewerConnectionId)
    {
        try
        {
            foreach (var chunk in DtoChunker.ChunkDto(dto, type))
            {
                await _desktopHubConnection.SendDtoToViewerAsync(chunk, viewerConnectionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending to viewer.");
        }
    }
}
