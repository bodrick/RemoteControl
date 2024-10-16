﻿using System.Drawing;
using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Shared.Models;

namespace Immense.RemoteControl.Desktop.Linux.Services;

public class CursorIconWatcherLinux : ICursorIconWatcher
{
    public event EventHandler<CursorInfo>? OnChange;

    public CursorInfo GetCurrentCursor() => new(Array.Empty<byte>(), Point.Empty, "default");
}
