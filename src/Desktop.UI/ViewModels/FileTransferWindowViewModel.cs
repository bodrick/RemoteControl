using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Desktop.Shared.ViewModels;
using Immense.RemoteControl.Desktop.UI.Services;
using Immense.RemoteControl.Desktop.UI.Views;
using Microsoft.Extensions.Logging;

namespace Immense.RemoteControl.Desktop.UI.ViewModels;

public interface IFileTransferWindowViewModel
{
    ObservableCollection<FileUpload> FileUploads { get; }

    ICommand OpenFileUploadDialogCommand { get; }

    ICommand RemoveFileUploadCommand { get; }

    string ViewerConnectionId { get; set; }

    string ViewerName { get; set; }

    void RemoveFileUpload(FileUpload? fileUpload);

    Task UploadFileAsync(string filePath);
}

public class FileTransferWindowViewModel : BrandedViewModelBase, IFileTransferWindowViewModel
{
    private readonly IFileTransferService _fileTransferService;
    private readonly IViewer _viewer;

    public FileTransferWindowViewModel(
       IViewer viewer,
       IBrandingProvider brandingProvider,
       IAvaloniaDispatcher dispatcher,
       IFileTransferService fileTransferService,
       ILogger<FileTransferWindowViewModel> logger)
       : base(brandingProvider, dispatcher, logger)
    {
        _viewer = viewer;
        _fileTransferService = fileTransferService;
        ViewerName = viewer.Name;
        ViewerConnectionId = viewer.ViewerConnectionID;

        OpenFileUploadDialogCommand = new AsyncRelayCommand<FileTransferWindow>(OpenFileUploadDialogAsync);
        RemoveFileUploadCommand = new RelayCommand<FileUpload>(RemoveFileUpload);
    }

    public ObservableCollection<FileUpload> FileUploads { get; } = new ObservableCollection<FileUpload>();

    public ICommand OpenFileUploadDialogCommand { get; }

    public ICommand RemoveFileUploadCommand { get; }

    public string ViewerConnectionId
    {
        get => Get<string>() ?? string.Empty;
        set => Set(value);
    }

    public string ViewerName
    {
        get => Get<string>() ?? string.Empty;
        set => Set(value);
    }

    public void RemoveFileUpload(FileUpload? fileUpload)
    {
        if (fileUpload is null)
        {
            return;
        }
        FileUploads.Remove(fileUpload);
        fileUpload.CancellationTokenSource.Cancel();
    }

    public async Task UploadFileAsync(string filePath)
    {
        var fileUpload = new FileUpload()
        {
            FilePath = filePath
        };

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            FileUploads.Add(fileUpload);
        });

        await _fileTransferService.UploadFileAsync(fileUpload, _viewer, fileUpload.CancellationTokenSource.Token, async progress =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                fileUpload.PercentProgress = progress;
            });
        });
    }

    private async Task OpenFileUploadDialogAsync(FileTransferWindow? window)
    {
        var initialDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (!Directory.Exists(initialDir))
        {
            initialDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "RemoteControl")).FullName;
        }

        var ofd = new OpenFileDialog
        {
            Title = "Upload File via Remotely",
            AllowMultiple = true,
            Directory = initialDir
        };

        var result = await ofd.ShowAsync(window!);
        if (result?.Any() != true)
        {
            return;
        }
        foreach (var file in result)
        {
            if (File.Exists(file))
            {
                await UploadFileAsync(file);
            }
        }
    }
}
