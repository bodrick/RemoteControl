﻿using Immense.RemoteControl.Desktop.UI.WPF.Views;
using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.UI.WPF.Services;

namespace Immense.RemoteControl.Desktop.Windows.Services;

public class RemoteControlAccessServiceWin : IRemoteControlAccessService
{
    private readonly IWindowsUiDispatcher _dispatcher;
    private readonly IViewModelFactory _viewModelFactory;

    public RemoteControlAccessServiceWin(
        IWindowsUiDispatcher dispatcher,
        IViewModelFactory viewModelFactory)
    {
        _dispatcher = dispatcher;
        _viewModelFactory = viewModelFactory;
    }

    public Task<bool> PromptForAccessAsync(string requesterName, string organizationName)
    {
        var result = _dispatcher.InvokeWpf(() =>
        {
            var viewModel = _viewModelFactory.CreatePromptForAccessViewModel(requesterName, organizationName);
            var promptWindow = new PromptForAccessWindow
            {
                DataContext = viewModel
            };
            promptWindow.ShowDialog();

            return viewModel.PromptResult;
        });

        return Task.FromResult(result);
    }
}
