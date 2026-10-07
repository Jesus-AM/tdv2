using System.Collections.Concurrent;
using System.Threading.Channels;
namespace Tdv2.Services;

/// <summary>Las señales no llevan datos. Cada suscriptor vuelve a autorizarse antes de recibir el estado.</summary>
public sealed class FormNotifications
{
    private readonly ConcurrentDictionary<Guid, (string Unit, Channel<bool> Channel)> subscriptions = new();
    public (Guid Id, ChannelReader<bool> Reader) Subscribe(string unit)
    {
        var channel = Channel.CreateBounded<bool>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropOldest });
        var id = Guid.NewGuid(); subscriptions[id] = (unit, channel); return (id, channel.Reader);
    }
    public void Remove(Guid id) { if (subscriptions.TryRemove(id, out var value)) value.Channel.Writer.TryComplete(); }
    public void Changed(string unit)
    {
        foreach (var subscription in subscriptions.Values)
            if (subscription.Unit == unit) subscription.Channel.Writer.TryWrite(true);
    }
    public void ParticipationChanged()
    {
        foreach (var subscription in subscriptions.Values) subscription.Channel.Writer.TryWrite(true);
    }
}
