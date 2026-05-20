"use client";
import { ChannelMessage } from '@/types/channels';
import { useQuery } from '@tanstack/react-query';
import { ChannelMessageCard } from '@/components/channels/ChannelMessageCard';

async function fetchMessages(): Promise<ChannelMessage[]> {
  const res = await fetch('/api/channels/messages');
  if (!res.ok) throw new Error('Error fetching messages');
  return res.json();
}

export default function ChannelsInboxPage() {
  const { data, isLoading, error } = useQuery({
    queryKey: ['channels', 'messages'],
    queryFn: fetchMessages,
  });

  return (
    <main className="p-6 max-w-3xl mx-auto" aria-label="Bandeja Omnicanal">
      <h1 className="text-2xl font-bold mb-4">Bandeja Omnicanal</h1>
      {isLoading && <div>Cargando mensajes...</div>}
      {error && <div className="text-red-600">Error: {(error as Error).message}</div>}
      <ul className="space-y-4" aria-label="Lista de mensajes">
        {data?.map((msg) => (
          <li key={msg.id}>
            <ChannelMessageCard message={msg} />
          </li>
        ))}
      </ul>
    </main>
  );
}
