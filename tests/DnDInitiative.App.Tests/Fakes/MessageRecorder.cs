using CommunityToolkit.Mvvm.Messaging;

using DnDInitiative.App.Messages;

namespace DnDInitiative.App.Tests.Fakes;

internal sealed class MessageRecorder : IRecipient<StartCombatMessage>
{
    public StartCombatMessage? Received { get; private set; }

    public void Receive(StartCombatMessage message)
    {
        Received = message;
    }
}