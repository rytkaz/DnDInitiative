using System.Net;
using System.Text;

using DnDInitiative.Core.Conditions;
using DnDInitiative.Core.Homebrewery;
using DnDInitiative.Core.StatBlocks;
using DnDInitiative.Infrastructure.Conditions;
using DnDInitiative.Infrastructure.Encounters;
using DnDInitiative.Infrastructure.Homebrewery;
using DnDInitiative.Infrastructure.StatBlocks;
using DnDInitiative.Infrastructure.Tests.Persistence;

namespace DnDInitiative.Infrastructure.Tests.Homebrewery;

public sealed class HomebreweryImporterTests : IDisposable
{
    private const string Source = "Test Bestiary";
    private const string Site = "https://homebrewery.naturalcrit.com";

    private const string ConditionsDocument = """
    # Appendix Z: Conditions

    ### Grappled
    A homebrew take on being grappled.

    ### Mired
    A mired creature's speed is halved, and it is Grappled.
    """;

    private const string LurkersDocument = """
    # Marsh Lurkers

    ___
    > ## Marsh Lurker
    >*Medium monstrosity, neutral evil*
    > ___
    > - **Armor Class** 14
    > - **Hit Points** 45 (6d8 + 18)
    > - **Speed** 30 ft.
    > - **Challenge** 2 (450 XP)
    > ___
    > ***Bite.*** The target is [Mired](https://homebrewery.naturalcrit.com/share/conditions01).
    """;

    private const string DrakesDocument = """
    {{monster,frame
    ## Cinder Drake {{bonus **cr 7**}}
    **AC** :: 17
    **HP** :: 110 (13d10 + 39)
    }}
    """;

    private readonly TestDatabase _database = new();
    private readonly string _directory = Directory.CreateTempSubdirectory("dndinitiative-tests-").FullName;
    private readonly FakeHttpHandler _handler = new();
    private readonly HttpClient _http;
    private readonly SqliteStatBlockRepository _library;
    private readonly SqliteConditionRepository _conditions;
    private readonly HomebreweryImporter _importer;

    public HomebreweryImporterTests()
    {
        _http = new HttpClient(_handler);
        _library = new SqliteStatBlockRepository(_database);
        _conditions = new SqliteConditionRepository(_database);
        _importer = new HomebreweryImporter(_http, _library, new SqliteEncounterRepository(_database), _conditions);

        _handler.Respond("conditions01", ConditionsDocument);
        _handler.Respond("lurkers0001", LurkersDocument);
        _handler.Respond("drakes00001", DrakesDocument);
    }

    public void Dispose()
    {
        _http.Dispose();
        _handler.Dispose();
        _database.Dispose();
        Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public async Task ImportAsync_Pdf_ImportsTheStatBlocksOfEveryLinkedDocument()
    {
        string pdf = WriteCatalog(("Lurkers", "lurkers0001"), ("Drakes", "drakes00001"));

        HomebreweryImportResult result = await _importer.ImportAsync(new HomebreweryImportRequest { PdfPath = pdf, Source = Source });

        Assert.Equal(2, result.DocumentsRead);
        Assert.Equal(new StatBlockImportResult(Added: 2, Updated: 0, FilesScanned: 2), result.StatBlocks);
        string[] expected = ["Cinder Drake (Test Bestiary)", "Marsh Lurker (Test Bestiary)"];
        Assert.Equal(expected, (await _library.GetAllAsync()).Select(statBlock => statBlock.Name).Order(StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public async Task ImportAsync_StatBlocks_GetTheCalculatedIdUnderTheChosenSource()
    {
        await _importer.ImportAsync(new HomebreweryImportRequest { Links = [Link("drakes00001")], Source = "  " + Source + " " });

        StatBlock drake = Assert.Single(await _library.GetAllAsync());
        Assert.Equal("Cinder Drake (Test Bestiary)", drake.Name);
        Assert.Equal(Source, drake.Source);
        Assert.Equal(StatBlockIdentity.ForImported("Cinder Drake (Test Bestiary)", Source), drake.Id);
        Assert.Equal(StatBlockOrigin.Imported, drake.Origin);
    }

    [Fact]
    public async Task ImportAsync_RunTwice_UpdatesInsteadOfDuplicating()
    {
        var request = new HomebreweryImportRequest { Links = [Link("drakes00001")], Source = Source };
        await _importer.ImportAsync(request);

        HomebreweryImportResult second = await _importer.ImportAsync(request);

        Assert.Equal(new StatBlockImportResult(Added: 0, Updated: 1, FilesScanned: 1), second.StatBlocks);
        Assert.Single(await _library.GetAllAsync());
    }

    [Fact]
    public async Task ImportAsync_ConditionsDocument_ImportsItsConditionsAndLinksStatBlocksToThem()
    {
        HomebreweryImportResult result = await _importer.ImportAsync(new HomebreweryImportRequest
        {
            Links = [Link("lurkers0001"), Link("conditions01")],
            Source = Source,
        });

        Assert.Equal(2, result.ConditionsImported);
        Condition? mired = await _conditions.FindAsync("Mired");
        Assert.Equal(Source, mired?.Source);
        Assert.Equal("A mired creature's speed is halved, and it is [Grappled](condition:Grappled).", mired?.Description);
        StatBlock lurker = Assert.Single(await _library.GetAllAsync());
        Assert.Equal("The target is [Mired](condition:Mired).", Assert.Single(lurker.Traits).Description);
    }

    [Fact]
    public async Task ImportAsync_ConditionFromThePlayersHandbook_IsLeftAsItIs()
    {
        var official = new Condition { Name = "Grappled", Source = "XPHB p. 362", Description = "The official text." };
        await _conditions.SaveManyAsync([official]);

        HomebreweryImportResult result = await _importer.ImportAsync(new HomebreweryImportRequest { Links = [Link("conditions01")], Source = Source });

        Assert.Equal(1, result.ConditionsImported);
        Assert.Equal(1, result.ConditionsKept);
        Assert.Equal(official, await _conditions.FindAsync("Grappled"));
    }

    [Fact]
    public async Task ImportAsync_ConditionAlreadyInTheLibrary_IsLinkedFromStatBlockText()
    {
        await _conditions.SaveManyAsync([new Condition { Name = "Prone", Source = "XPHB p. 367", Description = "Official." }]);
        _handler.Respond("wolves00001", """
            {{monster,frame
            ## Thorn Wolf
            **AC** :: 13
            **HP** :: 22
            ***Trip.*** The target is knocked prone.
            }}
            """);

        await _importer.ImportAsync(new HomebreweryImportRequest { Links = [Link("wolves00001")], Source = Source });

        StatBlock wolf = Assert.Single(await _library.GetAllAsync());
        Assert.Equal("The target is knocked [prone](condition:Prone).", Assert.Single(wolf.Traits).Description);
    }

    [Fact]
    public async Task ImportAsync_ConditionFromAnotherHomebrew_IsReplaced()
    {
        await _conditions.SaveManyAsync([new Condition { Name = "Grappled", Source = "Older Homebrew", Description = "Old text." }]);

        HomebreweryImportResult result = await _importer.ImportAsync(new HomebreweryImportRequest { Links = [Link("conditions01")], Source = Source });

        Assert.Equal(0, result.ConditionsKept);
        Assert.Equal("A homebrew take on being grappled.", (await _conditions.FindAsync("Grappled"))?.Description);
    }

    [Fact]
    public async Task ImportAsync_DocumentCannotBeDownloaded_ReportsItAndImportsTheRest()
    {
        var progress = new RecordingProgress();

        HomebreweryImportResult result = await _importer.ImportAsync(
            new HomebreweryImportRequest { Links = [Link("deleted0001"), Link("drakes00001")], Source = Source },
            progress);

        string[] expectedFailures = [Site + "/share/deleted0001"];
        Assert.Equal(1, result.DocumentsRead);
        Assert.Equal(expectedFailures, result.FailedLinks.ToArray());
        Assert.Contains(progress.Lines, line => line.StartsWith("Could not download " + Site + "/share/deleted0001", StringComparison.Ordinal));
        Assert.Single(await _library.GetAllAsync());
    }

    [Fact]
    public async Task ImportAsync_DocumentLinkedTwice_IsDownloadedOnce()
    {
        string pdf = WriteCatalog(("Drakes", "drakes00001"), ("Drakes again", "drakes00001"));

        await _importer.ImportAsync(new HomebreweryImportRequest
        {
            PdfPath = pdf,
            Links = [Site + "/share/drakes00001#p2"],
            Source = Source,
        });

        Assert.Equal(1, _handler.RequestCount("drakes00001"));
    }

    [Fact]
    public async Task ImportAsync_NoHomebreweryLinks_ThrowsAMessageForTheUser()
    {
        string pdf = Path.Combine(_directory, "forum.pdf");
        TestPdf.Write(pdf, ("Our forum", "https://example.com/forum"));

        await Assert.ThrowsAsync<ConversionException>(
            () => _importer.ImportAsync(new HomebreweryImportRequest { PdfPath = pdf, Links = ["not a link"], Source = Source }));
    }

    private static string Link(string shareId)
    {
        return Site + "/share/" + shareId;
    }

    private string WriteCatalog(params (string Text, string ShareId)[] entries)
    {
        string path = Path.Combine(_directory, "catalog.pdf");
        TestPdf.Write(path, entries.Select(entry => (entry.Text, Link(entry.ShareId))).ToArray());
        return path;
    }

    private sealed class RecordingProgress : IProgress<string>
    {
        private readonly List<string> _lines = [];

        public IReadOnlyList<string> Lines
        {
            get
            {
                lock (_lines)
                {
                    return _lines.ToList();
                }
            }
        }

        // Downloads run in parallel, so reports can arrive from several threads at once.
        public void Report(string value)
        {
            lock (_lines)
            {
                _lines.Add(value);
            }
        }
    }

    /// <summary>Serves documents by share id instead of going to the Homebrewery; anything else is not found.</summary>
    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, string> _documents = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _requests = new(StringComparer.Ordinal);

        public void Respond(string shareId, string markdown)
        {
            _documents[shareId] = markdown;
        }

        public int RequestCount(string shareId)
        {
            lock (_requests)
            {
                return _requests.GetValueOrDefault(shareId);
            }
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string path = request.RequestUri?.AbsolutePath ?? "";
            string shareId = path.StartsWith("/download/", StringComparison.Ordinal) ? path["/download/".Length..] : "";

            lock (_requests)
            {
                _requests[shareId] = _requests.GetValueOrDefault(shareId) + 1;
            }

            return Task.FromResult(_documents.TryGetValue(shareId, out string? markdown)
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(markdown, Encoding.UTF8, "text/plain") }
                : new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
