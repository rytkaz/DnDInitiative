using System.IO;

using DnDInitiative.App.Services;

namespace DnDInitiative.App.Tests.Services;

public class SingleInstanceTests
{
    // A different name per test, so tests cannot see each other or a running copy of the app.
    private readonly string _applicationId = "DnDInitiativeTests." + Path.GetRandomFileName();

    [Fact]
    public void IsFirstInstance_OnlyTheFirstWithAGivenNameIsFirst()
    {
        using var first = new SingleInstance(_applicationId);
        using var second = new SingleInstance(_applicationId);

        Assert.True(first.IsFirstInstance);
        Assert.False(second.IsFirstInstance);
    }

    [Fact]
    public void IsFirstInstance_AfterTheFirstIsDisposed_TheNameIsFreeAgain()
    {
        new SingleInstance(_applicationId).Dispose();

        using var next = new SingleInstance(_applicationId);

        Assert.True(next.IsFirstInstance);
    }

    [Fact]
    public async Task SendToFirstInstanceAsync_DeliversTheMessageToTheListener()
    {
        using var first = new SingleInstance(_applicationId);
        using var second = new SingleInstance(_applicationId);
        var received = new TaskCompletionSource<string>();
        _ = first.ListenAsync(message =>
        {
            received.TrySetResult(message);
            return Task.CompletedTask;
        });

        bool sent = await second.SendToFirstInstanceAsync("dndinitiative://encounter/example");

        Assert.True(sent);
        Assert.Equal("dndinitiative://encounter/example", await received.Task.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task ListenAsync_KeepsListeningAfterTheFirstMessage()
    {
        using var first = new SingleInstance(_applicationId);
        using var second = new SingleInstance(_applicationId);
        var messages = new List<string>();
        var gotBoth = new TaskCompletionSource();
        _ = first.ListenAsync(message =>
        {
            messages.Add(message);
            if (messages.Count == 2)
            {
                gotBoth.TrySetResult();
            }

            return Task.CompletedTask;
        });

        await second.SendToFirstInstanceAsync("one");
        await second.SendToFirstInstanceAsync("two");
        await gotBoth.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(["one", "two"], messages);
    }
}