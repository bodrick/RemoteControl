using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Immense.RemoteControl.Shared.Helpers;

public static class Debouncer
{
    private static readonly ConcurrentDictionary<object, System.Timers.Timer> Timers = new();

    public static void Debounce(TimeSpan wait, Action action, [CallerMemberName] string key = "")
    {
        if (Timers.TryRemove(key, out var timer))
        {
            timer.Stop();
            timer.Dispose();
        }

        timer = new System.Timers.Timer(wait.TotalMilliseconds)
        {
            AutoReset = false
        };

        timer.Elapsed += (s, e) =>
        {
            try
            {
                action();
            }
            finally
            {
                if (Timers.TryGetValue(key, out var result))
                {
                    result?.Dispose();
                }
            }
        };
        Timers.TryAdd(key, timer);
        timer.Start();
    }
}
