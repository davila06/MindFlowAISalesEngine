using Api.Domain.Channels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Api.Application.Channels;

public class InMemoryChannelMessageRepository : IChannelMessageRepository
{
    private readonly ConcurrentDictionary<Guid, ChannelMessage> _store = new();

    public Task<ChannelMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(_store.TryGetValue(id, out var msg) ? msg : null);

    public Task<IReadOnlyList<ChannelMessage>> ListByRecipientAsync(string recipient, CancellationToken cancellationToken)
        => Task.FromResult((IReadOnlyList<ChannelMessage>)_store.Values.Where(m => m.Recipient == recipient).ToList());

    public Task AddAsync(ChannelMessage message, CancellationToken cancellationToken)
    {
        _store[message.Id] = message;
        return Task.CompletedTask;
    }
}
