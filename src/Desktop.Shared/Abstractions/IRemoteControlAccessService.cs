namespace Immense.RemoteControl.Desktop.Shared.Abstractions;

public interface IRemoteControlAccessService
{
    Task<bool> PromptForAccessAsync(string requesterName, string organizationName);
}
