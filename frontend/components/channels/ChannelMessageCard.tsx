import { ChannelType, ChannelMessage } from '@/types/channels';

interface Props {
  message: ChannelMessage;
}

export function ChannelMessageCard({ message }: Props) {
  return (
    <article className="border rounded p-4 bg-white shadow-sm" aria-label={`Mensaje de ${ChannelType[message.type]}`} tabIndex={0}>
      <div className="flex items-center gap-2 mb-1">
        <span className="font-semibold" aria-label="Tipo de canal">{ChannelType[message.type]}</span>
        <span className="text-xs text-gray-500" aria-label="Fecha de envío">{new Date(message.sentAtUtc).toLocaleString()}</span>
      </div>
      <div className="text-gray-800 mb-1" aria-label="Contenido del mensaje">{message.content}</div>
      <div className="text-xs text-gray-500" aria-label="Remitente y destinatario">
        De: {message.sender} &rarr; Para: {message.recipient}
      </div>
    </article>
  );
}
