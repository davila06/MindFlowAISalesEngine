import type { WhatsAppMessage } from "@/types/sequences";

const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5156";

export async function sendWhatsApp(toPhone: string, body: string, leadId?: string): Promise<WhatsAppMessage> {
  const res = await fetch(`${API_BASE}/api/whatsapp/send`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ toPhone, body, leadId }),
  });
  if (!res.ok) throw new Error("Failed to send WhatsApp message");
  return res.json();
}

export async function optIn(phone: string): Promise<void> {
  const res = await fetch(`${API_BASE}/api/whatsapp/opt-in`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ phone }),
  });
  if (!res.ok) throw new Error("Failed to opt-in phone");
}

export async function optOut(phone: string): Promise<void> {
  const res = await fetch(`${API_BASE}/api/whatsapp/opt-out`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ phone }),
  });
  if (!res.ok) throw new Error("Failed to opt-out phone");
}

export async function getConversation(phone: string, page = 1, pageSize = 30, signal?: AbortSignal): Promise<WhatsAppMessage[]> {
  const res = await fetch(
    `${API_BASE}/api/whatsapp/conversations/${encodeURIComponent(phone)}?page=${page}&pageSize=${pageSize}`,
    { signal }
  );
  if (!res.ok) throw new Error("Failed to fetch conversation");
  return res.json();
}
