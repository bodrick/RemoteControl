﻿using System.Drawing;
using Immense.RemoteControl.Shared;
using SkiaSharp;

namespace Immense.RemoteControl.Desktop.Shared.Abstractions;

public interface IScreenCapturer : IDisposable
{
    event EventHandler<Rectangle> ScreenChanged;

    bool CaptureFullscreen { get; set; }

    Rectangle CurrentScreenBounds { get; }

    string SelectedScreen { get; }

    IEnumerable<string> GetDisplayNames();

    SKRect GetFrameDiffArea();

    Result<SKBitmap> GetImageDiff();

    Result<SKBitmap> GetNextFrame();

    int GetScreenCount();

    int GetSelectedScreenIndex();

    Rectangle GetVirtualScreenBounds();

    void Init();

    void SetSelectedScreen(string displayName);
}
