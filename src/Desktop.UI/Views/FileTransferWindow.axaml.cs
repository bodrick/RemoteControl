﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Immense.RemoteControl.Desktop.UI.Views;

public partial class FileTransferWindow : Window
{
    public FileTransferWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        Opened += FileTransferWindow_Opened;
    }

    private void FileTransferWindow_Opened(object? sender, EventArgs e)
    {
        Topmost = false;

        var left = Screens.Primary.WorkingArea.Right - Width;

        var top = Screens.Primary.WorkingArea.Bottom - Height;

        Position = new PixelPoint((int)left, (int)top);
    }
}
