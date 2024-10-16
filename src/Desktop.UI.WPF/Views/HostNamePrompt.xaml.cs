﻿using System.Windows;
using Immense.RemoteControl.Desktop.UI.WPF.ViewModels;

namespace Immense.RemoteControl.Desktop.UI.WPF.Views;

/// <summary>
/// Interaction logic for HostNamePrompt.xaml
/// </summary>
public partial class HostNamePrompt : Window
{
    public HostNamePrompt()
    {
        InitializeComponent();
    }

    public HostNamePromptViewModel? ViewModel => DataContext as HostNamePromptViewModel;

    private void OKButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
