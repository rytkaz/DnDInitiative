using DnDInitiative.Core.Campaigns;
using DnDInitiative.Infrastructure.Campaigns;
using DnDInitiative.Infrastructure.Persistence;

using Microsoft.Extensions.Logging.Abstractions;

namespace DnDInitiative.Infrastructure.Tests.Campaigns;

public sealed class CampaignServiceTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("dndinitiative-tests-").FullName;
    private readonly CampaignLocation _location = new();

    private string DataFolder => Path.Combine(_root, "appdata");

    private string DefaultFolder => Path.Combine(_root, "documents", "My Campaign");

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public async Task OpenAsync_NewFolder_CreatesACampaignNamedAfterTheFolder()
    {
        string folder = Path.Combine(_root, "Curse of Strahd");

        Campaign campaign = await CreateService().OpenAsync(folder);

        Assert.Equal(new Campaign(folder, "Curse of Strahd"), campaign);
        Assert.True(File.Exists(Path.Combine(folder, "campaign.json")));
        Assert.Equal(folder, _location.Folder);
    }

    [Fact]
    public async Task OpenAsync_ExistingCampaign_UsesTheNameInItsFile()
    {
        string folder = Path.Combine(_root, "strahd");
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(
            Path.Combine(folder, "campaign.json"),
            """{ "formatVersion": 1, "name": "Curse of Strahd" }""");

        Campaign campaign = await CreateService().OpenAsync(folder);

        Assert.Equal("Curse of Strahd", campaign.Name);
    }

    [Fact]
    public async Task OpenAsync_CampaignFromANewerVersion_RefusesAndKeepsTheCurrentOne()
    {
        CampaignService service = CreateService();
        Campaign first = await service.OpenAsync(Path.Combine(_root, "first"));
        string newer = Path.Combine(_root, "newer");
        Directory.CreateDirectory(newer);
        await File.WriteAllTextAsync(Path.Combine(newer, "campaign.json"), """{ "formatVersion": 99, "name": "Future" }""");

        await Assert.ThrowsAsync<CampaignException>(() => service.OpenAsync(newer));

        Assert.Equal(first, service.Current);
        Assert.Equal(first.Folder, _location.Folder);
    }

    [Fact]
    public async Task OpenAsync_UnreadableCampaignFile_Refuses()
    {
        string folder = Path.Combine(_root, "broken");
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "campaign.json"), "<<<<<<< HEAD");

        await Assert.ThrowsAsync<CampaignException>(() => CreateService().OpenAsync(folder));
    }

    [Fact]
    public async Task OpenLastOrDefaultAsync_NothingRemembered_OpensTheDefault()
    {
        Campaign campaign = await CreateService().OpenLastOrDefaultAsync();

        Assert.Equal(new Campaign(DefaultFolder, "My Campaign"), campaign);
    }

    [Fact]
    public async Task OpenLastOrDefaultAsync_AfterARestart_OpensTheCampaignUsedLast()
    {
        string folder = Path.Combine(_root, "strahd");
        await CreateService().OpenAsync(folder);

        // A new service object stands in for the app being closed and started again.
        Campaign campaign = await CreateService().OpenLastOrDefaultAsync();

        Assert.Equal(folder, campaign.Folder);
    }

    [Fact]
    public async Task OpenLastOrDefaultAsync_LastCampaignIsGone_OpensTheDefault()
    {
        string folder = Path.Combine(_root, "strahd");
        await CreateService().OpenAsync(folder);
        Directory.Delete(folder, recursive: true);

        Campaign campaign = await CreateService().OpenLastOrDefaultAsync();

        Assert.Equal(DefaultFolder, campaign.Folder);
    }

    [Fact]
    public async Task Inspect_TellsCampaignsEmptyFoldersAndOtherFoldersApart()
    {
        CampaignService service = CreateService();

        string campaign = Path.Combine(_root, "campaign");
        await service.OpenAsync(campaign);

        string newRepository = Path.Combine(_root, "new-repository");
        Directory.CreateDirectory(Path.Combine(newRepository, ".git"));

        string documents = Path.Combine(_root, "documents");
        Directory.CreateDirectory(documents);
        await File.WriteAllTextAsync(Path.Combine(documents, "letter.txt"), "Dear diary");

        Assert.Equal(CampaignFolderState.Campaign, service.Inspect(campaign));
        Assert.Equal(CampaignFolderState.Empty, service.Inspect(newRepository));
        Assert.Equal(CampaignFolderState.Empty, service.Inspect(Path.Combine(_root, "does-not-exist")));
        Assert.Equal(CampaignFolderState.HasOtherFiles, service.Inspect(documents));
    }

    private CampaignService CreateService()
    {
        return new CampaignService(
            _location,
            new CampaignOptions(DefaultFolder),
            new StorageOptions(DataFolder),
            NullLogger<CampaignService>.Instance);
    }
}