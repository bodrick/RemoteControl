using System.IO.Pipes;
using System.Text.Json;
using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Immense.RemoteControl.Desktop.Shared.Services;

public interface IChatHostService
{
    Task StartChatAsync(string requesterID, string organizationName);
}
public class ChatHostService : IChatHostService
{
    private readonly IChatUiService _chatUiService;
    private readonly ILogger<ChatHostService> _logger;

    private NamedPipeServerStream? _namedPipeStream;
    private StreamReader? _reader;
    private StreamWriter? _writer;

    public ChatHostService(IChatUiService chatUiService, ILogger<ChatHostService> logger)
    {
        _chatUiService = chatUiService;
        _logger = logger;
    }

    public async Task StartChatAsync(string pipeName, string organizationName)
    {
        _namedPipeStream = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 10, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        _writer = new StreamWriter(_namedPipeStream);
        _reader = new StreamReader(_namedPipeStream);

        var cts = new CancellationTokenSource(10000);
        try
        {
            await _namedPipeStream.WaitForConnectionAsync(cts.Token);
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("A chat session was attempted, but the client failed to connect in time.");
            Environment.Exit(0);
        }

        _chatUiService.ChatWindowClosed += OnChatWindowClosed;

        _chatUiService.ShowChatWindow(organizationName, _writer);

        _ = Task.Run(ReadFromStreamAsync);
    }

    private void OnChatWindowClosed(object? sender, EventArgs e)
    {
        try
        {
            _namedPipeStream?.Dispose();
        }
        catch { }
    }

    private async Task ReadFromStreamAsync()
    {
        while (_namedPipeStream?.IsConnected == true)
        {
            try
            {
                var messageJson = await _reader!.ReadLineAsync();
                if (!string.IsNullOrWhiteSpace(messageJson))
                {
                    var chatMessage = JsonSerializer.Deserialize<ChatMessage>(messageJson);
                    if (chatMessage is null)
                    {
                        _logger.LogWarning("Deserialized message was null.  Value: {value}", messageJson);
                        continue;
                    }
                    await _chatUiService.ReceiveChatAsync(chatMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while reading from chat IPC stream.");
            }
        }
    }
}
