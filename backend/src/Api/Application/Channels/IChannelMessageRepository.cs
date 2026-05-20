using Api.Domain.Channels;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Api.Application.Channels;

public interface IChannelMessageRepository
{
    Task<ChannelMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChannelMessage>> ListByRecipientAsync(string recipient, CancellationToken cancellationToken);
    Task AddAsync(ChannelMessage message, CancellationToken cancellationToken);
}

public interface IChannelDispatcher
{
    Task SendAsync(ChannelMessage message, CancellationToken cancellationToken);
}
