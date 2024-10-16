﻿using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Enums;
using Immense.RemoteControl.Desktop.Shared.Native.Win32;
using Immense.RemoteControl.Shared.Helpers;
using Immense.RemoteControl.Shared.Models.Dtos;
using MessagePack;
using Microsoft.Extensions.Logging;

namespace Immense.RemoteControl.Desktop.Shared.Services;

public interface IDtoMessageHandler
{
    Task ParseMessageAsync(IViewer viewer, byte[] message);
}

public class DtoMessageHandler : IDtoMessageHandler
{
    private readonly IAudioCapturer _audioCapturer;

    private readonly IClipboardService _clipboardService;

    private readonly IFileTransferService _fileTransferService;

    private readonly IKeyboardMouseInput _keyboardMouseInput;

    private readonly ILogger<DtoMessageHandler> _logger;

    public DtoMessageHandler(
        IKeyboardMouseInput keyboardMouseInput,
        IAudioCapturer audioCapturer,
        IClipboardService clipboardService,
        IFileTransferService fileTransferService,
        ILogger<DtoMessageHandler> logger)
    {
        _keyboardMouseInput = keyboardMouseInput;
        _audioCapturer = audioCapturer;
        _clipboardService = clipboardService;
        _fileTransferService = fileTransferService;
        _logger = logger;
    }

    public async Task ParseMessageAsync(IViewer viewer, byte[] message)
    {
        try
        {
            var wrapper = MessagePackSerializer.Deserialize<DtoWrapper>(message);

            switch (wrapper.DtoType)
            {
                case DtoType.MouseMove:
                case DtoType.MouseDown:
                case DtoType.MouseUp:
                case DtoType.Tap:
                case DtoType.MouseWheel:
                case DtoType.KeyDown:
                case DtoType.KeyUp:
                case DtoType.CtrlAltDel:
                case DtoType.ToggleBlockInput:
                case DtoType.ClipboardTransfer:
                case DtoType.KeyPress:
                case DtoType.SetKeyStatesUp:
                    {
                        if (!viewer.HasControl)
                        {
                            return;
                        }
                    }

                    break;
                default:
                    break;
            }

            switch (wrapper.DtoType)
            {
                case DtoType.SelectScreen:
                    SelectScreen(wrapper, viewer);
                    break;
                case DtoType.MouseMove:
                    MouseMove(wrapper, viewer);
                    break;
                case DtoType.MouseDown:
                    MouseDown(wrapper, viewer);
                    break;
                case DtoType.MouseUp:
                    MouseUp(wrapper, viewer);
                    break;
                case DtoType.Tap:
                    Tap(wrapper, viewer);
                    break;
                case DtoType.MouseWheel:
                    MouseWheel(wrapper);
                    break;
                case DtoType.KeyDown:
                    KeyDown(wrapper);
                    break;
                case DtoType.KeyUp:
                    KeyUp(wrapper);
                    break;
                case DtoType.CtrlAltDel:
                    CtrlAltDel();
                    break;
                case DtoType.ToggleAudio:
                    ToggleAudio(wrapper);
                    break;
                case DtoType.ToggleBlockInput:
                    ToggleBlockInput(wrapper);
                    break;
                case DtoType.ClipboardTransfer:
                    await ClipboardTransferAsync(wrapper);
                    break;
                case DtoType.KeyPress:
                    await KeyPressAsync(wrapper);
                    break;
                case DtoType.File:
                    await DownloadFileAsync(wrapper);
                    break;
                case DtoType.WindowsSessions:
                    await GetWindowsSessionsAsync(viewer);
                    break;
                case DtoType.SetKeyStatesUp:
                    SetKeyStatesUp();
                    break;
                case DtoType.FrameReceived:
                    HandleFrameReceived(viewer);
                    break;
                case DtoType.OpenFileTransferWindow:
                    OpenFileTransferWindow(viewer);
                    break;
                default:
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while parsing message.");
        }
    }

    private void CtrlAltDel()
    {
        if (OperatingSystem.IsWindows())
        {
            // Might as well try both.
            User32.SendSAS(false);
            User32.SendSAS(true);
        }
    }

    private async Task ClipboardTransferAsync(DtoWrapper wrapper)
    {
        if (!DtoChunker.TryComplete<ClipboardTransferDto>(wrapper, out var dto))
        {
            return;
        }

        if (dto!.TypeText)
        {
            _keyboardMouseInput.SendText(dto.Text);
        }
        else
        {
            await _clipboardService.SetTextAsync(dto.Text);
        }
    }

    private async Task DownloadFileAsync(DtoWrapper wrapper)
    {
        if (!DtoChunker.TryComplete<FileDto>(wrapper, out var dto))
        {
            return;
        }

        await _fileTransferService.ReceiveFileAsync(dto!.Buffer,
            dto.FileName,
            dto.MessageId,
            dto.EndOfFile,
            dto.StartOfFile);
    }

    private async Task GetWindowsSessionsAsync(IViewer viewer)
    {
        await viewer.SendWindowsSessionsAsync();
    }

    private void HandleFrameReceived(IViewer viewer)
    {
        viewer.DequeuePendingFrame();
    }

    private void KeyDown(DtoWrapper wrapper)
    {
        if (!DtoChunker.TryComplete<KeyDownDto>(wrapper, out var dto))
        {
            return;
        }

        if (dto?.Key is null)
        {
            _logger.LogWarning("Key input is empty.");
            return;
        }

        _keyboardMouseInput.SendKeyDown(dto.Key);
    }

    private async Task KeyPressAsync(DtoWrapper wrapper)
    {
        if (!DtoChunker.TryComplete<KeyPressDto>(wrapper, out var dto))
        {
            return;
        }

        if (dto?.Key is null)
        {
            _logger.LogWarning("Key input is empty.");
            return;
        }

        _keyboardMouseInput.SendKeyDown(dto.Key);
        await Task.Delay(1);
        _keyboardMouseInput.SendKeyUp(dto.Key);
    }

    private void KeyUp(DtoWrapper wrapper)
    {
        if (!DtoChunker.TryComplete<KeyUpDto>(wrapper, out var dto))
        {
            return;
        }

        if (dto?.Key is null)
        {
            _logger.LogWarning("Key input is empty.");
            return;
        }

        _keyboardMouseInput.SendKeyUp(dto.Key);
    }

    private void MouseDown(DtoWrapper wrapper, IViewer viewer)
    {
        if (!DtoChunker.TryComplete<MouseDownDto>(wrapper, out var dto))
        {
            return;
        }

        _keyboardMouseInput.SendMouseButtonAction(dto!.Button, ButtonAction.Down, dto.PercentX, dto.PercentY, viewer);
    }

    private void MouseMove(DtoWrapper wrapper, IViewer viewer)
    {
        if (!DtoChunker.TryComplete<MouseMoveDto>(wrapper, out var dto))
        {
            return;
        }

        _keyboardMouseInput.SendMouseMove(dto!.PercentX, dto.PercentY, viewer);
    }

    private void MouseUp(DtoWrapper wrapper, IViewer viewer)
    {
        if (!DtoChunker.TryComplete<MouseUpDto>(wrapper, out var dto))
        {
            return;
        }

        _keyboardMouseInput.SendMouseButtonAction(dto!.Button, ButtonAction.Up, dto.PercentX, dto.PercentY, viewer);
    }

    private void MouseWheel(DtoWrapper wrapper)
    {
        if (!DtoChunker.TryComplete<MouseWheelDto>(wrapper, out var dto))
        {
            return;
        }

        _keyboardMouseInput.SendMouseWheel(-(int)dto!.DeltaY);
    }

    private void OpenFileTransferWindow(IViewer viewer)
    {
        _fileTransferService.OpenFileTransferWindow(viewer);
    }

    private void SelectScreen(DtoWrapper wrapper, IViewer viewer)
    {
        if (!DtoChunker.TryComplete<SelectScreenDto>(wrapper, out var dto))
        {
            return;
        }

        viewer.Capturer.SetSelectedScreen(dto!.DisplayName);
    }

    private void SetKeyStatesUp()
    {
        _keyboardMouseInput.SetKeyStatesUp();
    }

    private void Tap(DtoWrapper wrapper, IViewer viewer)
    {
        if (!DtoChunker.TryComplete<TapDto>(wrapper, out var dto))
        {
            return;
        }

        _keyboardMouseInput.SendMouseButtonAction(0, ButtonAction.Down, dto!.PercentX, dto.PercentY, viewer);
        _keyboardMouseInput.SendMouseButtonAction(0, ButtonAction.Up, dto.PercentX, dto.PercentY, viewer);
    }

    private void ToggleAudio(DtoWrapper wrapper)
    {
        if (!DtoChunker.TryComplete<ToggleAudioDto>(wrapper, out var dto))
        {
            return;
        }

        _audioCapturer.ToggleAudio(dto!.ToggleOn);
    }

    private void ToggleBlockInput(DtoWrapper wrapper)
    {
        if (!DtoChunker.TryComplete<ToggleBlockInputDto>(wrapper, out var dto))
        {
            return;
        }

        _keyboardMouseInput.ToggleBlockInput(dto!.ToggleOn);
    }
}
