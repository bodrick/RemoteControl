using System.Security.Cryptography;

namespace Immense.RemoteControl.Shared.Helpers;

public static class RandomGenerator
{
    private const string AllowableCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIGKLMNOPQRSTUVWXYZ0123456789";

    public static string GenerateAccessKey() => GenerateString(64);

    public static string GenerateString(int length)
    {
        var bytes = new byte[length];
        using (var random = RandomNumberGenerator.Create())
        {
            random.GetBytes(bytes);
        }

        return new string(bytes.Select(x => AllowableCharacters[x % AllowableCharacters.Length]).ToArray());
    }
}
