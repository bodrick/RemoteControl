using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Desktop.Shared.ViewModels;

namespace Immense.RemoteControl.Desktop.Shared.Abstractions;

public interface IFileTransferService
{
    string GetBaseDirectory();

    Task ReceiveFileAsync(byte[] buffer, string fileName, string messageId, bool endOfFile, bool startOfFile);

    void OpenFileTransferWindow(IViewer viewer);

    Task UploadFileAsync(FileUpload file, IViewer viewer, CancellationToken cancelToken, Action<double> progressUpdateCallback);
}
