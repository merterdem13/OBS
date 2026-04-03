using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace OBS.Helpers
{
    internal static class TaskExtensions
    {
        public static void Forget(this Task task, string operationName)
        {
            if (task.IsCompletedSuccessfully)
            {
                return;
            }

            _ = ForgetAwaited(task, operationName);
        }

        private static async Task ForgetAwaited(Task task, string operationName)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{operationName}] failed: {ex}");
            }
        }
    }
}
