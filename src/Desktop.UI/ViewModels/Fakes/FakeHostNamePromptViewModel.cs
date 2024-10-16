﻿using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Immense.RemoteControl.Desktop.UI.ViewModels.Fakes;

public class FakeHostNamePromptViewModel : FakeBrandedViewModelBase, IHostNamePromptViewModel
{
    public string Host { get; set; } = "https://localhost:7024";

    public ICommand OKCommand => new RelayCommand(() => { });
}
