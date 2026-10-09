using Microsoft.Extensions.Logging;

namespace DnDInitiative.App.Logging;

/// <summary>
/// The app's own log messages, declared in one place.
/// </summary>
internal static partial class AppLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Application started")]
    public static partial void Started(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception on {Origin}")]
    public static partial void UnhandledException(ILogger logger, Exception exception, string origin);

    [LoggerMessage(Level = LogLevel.Critical, Message = "Fatal exception, the process is ending")]
    public static partial void FatalException(ILogger logger, Exception exception);
}