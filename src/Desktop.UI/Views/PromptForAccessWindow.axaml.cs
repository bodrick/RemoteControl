using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Immense.RemoteControl.Desktop.UI.Views;

public partial class PromptForAccessWindow : Window
{
    public PromptForAccessWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        Opened += Window_Opened;
        this.FindControl<Border>("TitleBanner").PointerPressed += TitleBanner_PointerPressed;
    }

    private void Window_Opened(object? sender, EventArgs e)
    {
        Topmost = false;
    }

    private void TitleBanner_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.PointerUpdateKind == Avalonia.Input.PointerUpdateKind.LeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}
