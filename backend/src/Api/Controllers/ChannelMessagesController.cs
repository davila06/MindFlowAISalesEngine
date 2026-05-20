using Api.Application.Channels;
using Api.Domain.Channels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Api.Controllers;

[ApiController]
[Route("api/channels/messages")]
public class ChannelMessagesController : ControllerBase
{
    private readonly IChannelMessageRepository _repository;
    private readonly IChannelDispatcher _dispatcher;

    public ChannelMessagesController(IChannelMessageRepository repository, IChannelDispatcher dispatcher)
    {
        _repository = repository;
        _dispatcher = dispatcher;
    }

    [HttpPost]
    public async Task<IActionResult> Send([FromBody] ChannelMessage message, CancellationToken cancellationToken)
    {
        message.Id = Guid.NewGuid();
        message.SentAtUtc = DateTime.UtcNow;
        await _dispatcher.SendAsync(message, cancellationToken);
        await _repository.AddAsync(message, cancellationToken);
        return Accepted(message);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var msg = await _repository.GetByIdAsync(id, cancellationToken);
        return msg is null ? NotFound() : Ok(msg);
    }
}
