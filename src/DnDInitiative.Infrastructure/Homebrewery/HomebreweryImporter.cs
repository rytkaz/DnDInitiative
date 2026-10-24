using System.Collections.Concurrent;
using System.Globalization;

using DnDInitiative.Core.Conditions;
using DnDInitiative.Core.Encounters;
using DnDInitiative.Core.Homebrewery;
using DnDInitiative.Core.StatBlocks;
using DnDInitiative.Infrastructure.Import;

namespace DnDInitiative.Infrastructure.Homebrewery;

/// <summary>
/// Imports stat blocks and conditions from Homebrewery documents. The documents are found
/// through the links in a PDF, such as a creature catalog, and through links typed in by hand.
/// </summary>
internal sealed class HomebreweryImporter : IHomebreweryImporter
{
    // A catalog links to hundreds of documents. A few downloads at a time takes minutes
    // instead of a quarter of an hour, and is still gentle on a site run for free.
    private const int ParallelDownloads = 4;

    // How often to say how far the downloads have got.
    private const int ProgressInterval = 50;

    private readonly HttpClient _http;
    private readonly IStatBlockLibrary _library;
    private readonly IEncounterRepository _encounters;
    private readonly IConditionRepository _conditions;

    public HomebreweryImporter(
        HttpClient http,
        IStatBlockLibrary library,
        IEncounterRepository encounters,
        IConditionRepository conditions)
    {
        _http = http;
        _library = library;
        _encounters = encounters;
        _conditions = conditions;
    }

    public async Task<HomebreweryImportResult> ImportAsync(
        HomebreweryImportRequest request,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Source);

        string source = request.Source.Trim();
        List<string> shareIds = CollectShareIds(request, progress);
        if (shareIds.Count == 0)
        {
            throw new ConversionException(
                "There are no Homebrewery links to import. Choose a PDF that links to Homebrewery documents, or add links by hand.");
        }

        (List<KeyValuePair<string, string>> documents, List<string> failedIds) =
            await DownloadAllAsync(shareIds, progress, cancellationToken).ConfigureAwait(false);

        // Conditions first: the stat blocks link to them, and those links need the names.
        List<string> conditionDocumentIds = documents
            .Where(document => HomebreweryConditionParser.IsConditionsDocument(document.Value))
            .Select(document => document.Key)
            .ToList();
        List<Condition> conditions = documents
            .Where(document => conditionDocumentIds.Contains(document.Key, StringComparer.Ordinal))
            .SelectMany(document => HomebreweryConditionParser.Parse(document.Value, source))
            .ToList();

        // Names are linked to any condition the library will have, not only the new ones:
        // a stat block's "Prone" should open the Player's Handbook text when that is what is there.
        IReadOnlyList<string> storedNames = await _conditions.GetNamesAsync(cancellationToken).ConfigureAwait(false);
        var links = new ConditionLinkRewriter(conditionDocumentIds, conditions.Select(condition => condition.Name).Concat(storedNames));

        // Conditions mention each other ("is Incapacitated (see the condition)"); those become links too.
        conditions = conditions
            .Select(condition => condition with { Description = links.LinkNames(condition.Description, except: condition.Name) })
            .ToList();
        (int conditionsImported, int conditionsKept) = await SaveConditionsAsync(conditions, cancellationToken).ConfigureAwait(false);

        var statBlocks = new List<StatBlock>();
        foreach (KeyValuePair<string, string> document in documents)
        {
            statBlocks.AddRange(HomebreweryStatBlockParser.Parse(document.Value, source, links));
        }

        (int added, int updated) = await ImportedStatBlocks
            .SaveAsync(_library, _encounters, statBlocks, cancellationToken)
            .ConfigureAwait(false);

        return new HomebreweryImportResult
        {
            DocumentsRead = documents.Count,
            FailedLinks = failedIds.Select(HomebreweryLinks.ShareLink).ToList(),
            StatBlocks = new StatBlockImportResult(added, updated, documents.Count),
            ConditionsImported = conditionsImported,
            ConditionsKept = conditionsKept,
        };
    }

    /// <summary>The share ids from the PDF and the typed links, each once, in the order found.</summary>
    private static List<string> CollectShareIds(HomebreweryImportRequest request, IProgress<string>? progress)
    {
        var links = new List<string>();

        if (!string.IsNullOrWhiteSpace(request.PdfPath))
        {
            IReadOnlyList<PdfLink> pdfLinks = PdfLinkReader.ReadLinks(request.PdfPath.Trim());
            int fromPdf = pdfLinks.Count(link => HomebreweryLinks.ShareId(link.Uri) is not null);
            progress?.Report(string.Create(
                CultureInfo.InvariantCulture,
                $"The PDF has {pdfLinks.Count} links, {fromPdf} of them to Homebrewery documents."));
            links.AddRange(pdfLinks.Select(link => link.Uri));
        }

        links.AddRange(request.Links);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var shareIds = new List<string>();
        foreach (string link in links)
        {
            // A catalog links to some documents more than once, from different entries.
            if (HomebreweryLinks.ShareId(link) is { } shareId && seen.Add(shareId))
            {
                shareIds.Add(shareId);
            }
        }

        return shareIds;
    }

    /// <summary>
    /// Downloads every document's source. A document that cannot be downloaded is reported and
    /// skipped, so one deleted page does not stop a catalog of hundreds.
    /// </summary>
    /// <returns>The documents that arrived, keyed by share id in the order asked for, and the ids that failed.</returns>
    private async Task<(List<KeyValuePair<string, string>> Documents, List<string> FailedIds)> DownloadAllAsync(
        List<string> shareIds,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        progress?.Report(string.Create(CultureInfo.InvariantCulture, $"Downloading {shareIds.Count} Homebrewery documents..."));

        var downloaded = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
        int finished = 0;
        var options = new ParallelOptions { MaxDegreeOfParallelism = ParallelDownloads, CancellationToken = cancellationToken };

        await Parallel.ForEachAsync(shareIds, options, async (shareId, token) =>
        {
            try
            {
                downloaded[shareId] = await DownloadAsync(shareId, token).ConfigureAwait(false);
            }
            catch (HttpRequestException exception)
            {
                progress?.Report($"Could not download {HomebreweryLinks.ShareLink(shareId)}: {exception.Message}");
            }
            catch (TaskCanceledException) when (!token.IsCancellationRequested)
            {
                // Not cancelled by the user, so the request took longer than the client allows.
                progress?.Report($"Could not download {HomebreweryLinks.ShareLink(shareId)}: it took too long to answer.");
            }

            int count = Interlocked.Increment(ref finished);
            if (count % ProgressInterval == 0 && count < shareIds.Count)
            {
                progress?.Report(string.Create(CultureInfo.InvariantCulture, $"Downloaded {count} of {shareIds.Count}..."));
            }
        }).ConfigureAwait(false);

        var documents = new List<KeyValuePair<string, string>>();
        var failedIds = new List<string>();
        foreach (string shareId in shareIds)
        {
            if (downloaded.TryGetValue(shareId, out string? markdown))
            {
                documents.Add(new KeyValuePair<string, string>(shareId, markdown));
            }
            else
            {
                failedIds.Add(shareId);
            }
        }

        return (documents, failedIds);
    }

    private async Task<string> DownloadAsync(string shareId, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _http
            .GetAsync(HomebreweryLinks.SourceUri(shareId), cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Saves the conditions, except those the library already has from the Player's Handbook
    /// or the SRD: the official text is kept, and homebrew only fills in what is missing.
    /// </summary>
    /// <returns>How many were saved, and how many were left as they were.</returns>
    private async Task<(int Imported, int Kept)> SaveConditionsAsync(List<Condition> conditions, CancellationToken cancellationToken)
    {
        var toSave = new List<Condition>();
        int kept = 0;

        foreach (Condition condition in conditions)
        {
            Condition? existing = await _conditions.FindAsync(condition.Name, cancellationToken).ConfigureAwait(false);
            if (existing is not null && ConditionSources.IsCoreRules(existing.Source))
            {
                kept++;
                continue;
            }

            toSave.Add(condition);
        }

        if (toSave.Count > 0)
        {
            await _conditions.SaveManyAsync(toSave, cancellationToken).ConfigureAwait(false);
        }

        return (toSave.Count, kept);
    }
}
