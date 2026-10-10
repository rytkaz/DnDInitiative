using Microsoft.Extensions.Logging;

namespace DnDInitiative.Infrastructure.Campaigns;

internal static partial class CampaignLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipped campaign file {Path} because it could not be read")]
    public static partial void UnreadableFile(ILogger logger, Exception exception, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Ignored campaign file {Path} because an earlier file has the same Id")]
    public static partial void DuplicateId(ILogger logger, string path);
}