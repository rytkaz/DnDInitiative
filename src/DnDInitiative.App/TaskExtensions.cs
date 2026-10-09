namespace DnDInitiative.App;

internal static class TaskExtensions
{
    /// <summary>
    /// Starts a task without waiting for it. If it fails, the exception is raised on the
    /// calling thread's dispatcher, where the app's global handler logs and reports it.
    /// </summary>
    public static async void Forget(this Task task)
    {
        ArgumentNullException.ThrowIfNull(task);
        await task;
    }
}