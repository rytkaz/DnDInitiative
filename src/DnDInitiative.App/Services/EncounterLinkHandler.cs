using CommunityToolkit.Mvvm.Messaging;

using DnDInitiative.App.Messages;
using DnDInitiative.Core.Encounters;
using DnDInitiative.Core.Parties;
using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.App.Services;

/// <summary>
/// Turns an encounter link into a request to start that encounter.
/// </summary>
public sealed class EncounterLinkHandler
{
    private readonly IEncounterRepository _encounterRepository;
    private readonly IStatBlockRepository _statBlockRepository;
    private readonly IPartyRepository _partyRepository;
    private readonly IMessenger _messenger;

    public EncounterLinkHandler(
        IEncounterRepository encounterRepository,
        IStatBlockRepository statBlockRepository,
        IPartyRepository partyRepository,
        IMessenger messenger)
    {
        _encounterRepository = encounterRepository;
        _statBlockRepository = statBlockRepository;
        _partyRepository = partyRepository;
        _messenger = messenger;
    }

    /// <summary>Returns false if the text is not a link to an encounter that exists.</summary>
    public async Task<bool> HandleAsync(string? text)
    {
        if (!EncounterLink.TryParse(text, out EncounterLink? link))
        {
            return false;
        }

        Encounter? encounter = await _encounterRepository.GetAsync(link.EncounterId);
        if (encounter is null)
        {
            return false;
        }

        IReadOnlyList<StatBlock> statBlocks = await _statBlockRepository.GetAllAsync();

        Party? party = null;
        if (link.PartyId is { } partyId)
        {
            IReadOnlyList<Party> parties = await _partyRepository.GetAllAsync();
            party = parties.FirstOrDefault(candidate => candidate.Id == partyId);
        }

        _messenger.Send(new StartCombatMessage(
            encounter.Name,
            encounter.ToBlueprints(statBlocks.ToDictionary(statBlock => statBlock.Id)),
            party));
        return true;
    }
}