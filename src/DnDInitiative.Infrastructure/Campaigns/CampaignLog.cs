using Microsoft.Extensions.Logging;

namespace DnDInitiative.Infrastructure.Campaigns;

internal static partial class CampaignLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipped campaign file {Path} because it could not be read")]
    public static partial void UnreadableFile(ILogger logger, Exception exception, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Ignored campaign file {Path} because an earlier file has the same Id")]
    public static partial void DuplicateId(ILogger logger, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The campaign used last time is no longer at {Folder}; opening the default campaign")]
    public static partial void LastCampaignMissing(ILogger logger, string folder);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The campaign at {Folder} could not be opened; opening the default campaign")]
    public static partial void LastCampaignUnusable(ILogger logger, Exception exception, string folder);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Moved {Parties} parties, {Encounters} encounters and {StatBlocks} stat blocks from the database to {Folder}")]
    public static partial void LegacyDataMoved(ILogger logger, int parties, int encounters, int statBlocks, string folder);
}