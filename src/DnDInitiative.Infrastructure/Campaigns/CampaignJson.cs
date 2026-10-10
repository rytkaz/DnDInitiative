using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DnDInitiative.Infrastructure.Campaigns;

/// <summary>How campaign files are written. Changing this changes every file on the next save.</summary>
internal static class CampaignJson
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,

        // The same line endings on every operating system, so files do not differ between machines.
        NewLine = "\n",

        // Leave out properties that have no value instead of writing "maxHp": null.
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

        // Write apostrophes and accented letters as themselves. The default escapes them
        // for safe embedding in web pages, which these files never are.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,

        // Refuse a file that leaves out something the type needs, or sets it to null.
        // Without these, such a file would load "successfully" and crash later.
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,

        // Enums as words ("imported"), not numbers.
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };
}