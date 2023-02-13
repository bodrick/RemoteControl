using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Immense.RemoteControl.Desktop.Shared.ViewModels;

namespace Immense.RemoteControl.Desktop.UI.WPF.ViewModels.Fakes;

public class FakeFileTransferWindowViewModel : FakeBrandedViewModelBase, IFileTransferWindowViewModel
{
    public ObservableCollection<FileUpload> FileUploads { get; } = new();

    public AsyncRelayCommand OpenFileDialogCommand { get; } = new(() => Task.CompletedTask);

    public RelayCommand<FileUpload> RemoveFileUploadCommand { get; } = new RelayCommand<FileUpload>(x => { });

    public string ViewerConnectionId { get; set; } = string.Empty;

    public string ViewerName { get; set; } = string.Empty;

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
