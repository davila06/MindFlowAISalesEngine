using Api.Domain.Channels;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Api.Application.Channels;

public class EmailChannelDispatcher : IChannelDispatcher
{
    public Task SendAsync(ChannelMessage message, CancellationToken cancellationToken)
    {
        // Aquí se integraría con el módulo de Email real (SMTP, SendGrid, etc.)
        // Por ahora, simula el envío
        return Task.CompletedTask;
    }
}

public class WhatsAppChannelDispatcher : IChannelDispatcher
{
    public Task SendAsync(ChannelMessage message, CancellationToken cancellationToken)
    {
        // Aquí se integraría con la API de WhatsApp Business
        return Task.CompletedTask;
    }
}

public class ChatChannelDispatcher : IChannelDispatcher
{
    public Task SendAsync(ChannelMessage message, CancellationToken cancellationToken)
    {
        // Aquí se integraría con el proveedor de chat (Intercom, Zendesk, etc.)
        return Task.CompletedTask;
    }
}

public class SocialChannelDispatcher : IChannelDispatcher
{
    public Task SendAsync(ChannelMessage message, CancellationToken cancellationToken)
    {
        // Aquí se integraría con APIs de redes sociales
        return Task.CompletedTask;
    }
}
