using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Immense.RemoteControl.Desktop.Shared.ViewModels;

namespace Immense.RemoteControl.Desktop.UI.ViewModels.Fakes;

public class FakeFileTransferViewModel : FakeBrandedViewModelBase, IFileTransferWindowViewModel
{
    public ObservableCollection<FileUpload> FileUploads { get; } = new();

    public string ViewerConnectionId { get; set; } = string.Empty;

    public string ViewerName { get; set; } = string.Empty;

    public ICommand OpenFileUploadDialogCommand { get; } = new RelayCommand(() => { });

    public ICommand RemoveFileUploadCommand { get; } = new RelayCommand(() => { });

    public Task OpenFileUploadDialogAsync()
    {
        return Task.CompletedTask;
    }

    public void RemoveFileUpload(FileUpload? fileUpload)
    {
    }

    public Task UploadFileAsync(string filePath)
    {
        return Task.CompletedTask;
    }
}
