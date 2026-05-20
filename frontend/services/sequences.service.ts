import type { Sequence, SequenceEnrollment, CreateSequenceRequest, UpdateSequenceRequest } from "@/types/sequences";

const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5156";

export async function getSequences(signal?: AbortSignal): Promise<Sequence[]> {
  const res = await fetch(`${API_BASE}/api/sequences`, { signal });
  if (!res.ok) throw new Error("Failed to fetch sequences");
  return res.json();
}

export async function getSequenceById(id: string, signal?: AbortSignal): Promise<Sequence> {
  const res = await fetch(`${API_BASE}/api/sequences/${id}`, { signal });
  if (!res.ok) throw new Error("Failed to fetch sequence");
  return res.json();
}

export async function createSequence(body: CreateSequenceRequest): Promise<Sequence> {
  const res = await fetch(`${API_BASE}/api/sequences`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error("Failed to create sequence");
  return res.json();
}

export async function updateSequence(id: string, body: UpdateSequenceRequest): Promise<Sequence> {
  const res = await fetch(`${API_BASE}/api/sequences/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error("Failed to update sequence");
  return res.json();
}

export async function deleteSequence(id: string): Promise<void> {
  const res = await fetch(`${API_BASE}/api/sequences/${id}`, { method: "DELETE" });
  if (!res.ok) throw new Error("Failed to delete sequence");
}

export async function enrollLead(sequenceId: string, leadId: string): Promise<SequenceEnrollment> {
  const res = await fetch(`${API_BASE}/api/sequences/${sequenceId}/enroll`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ leadId }),
  });
  if (!res.ok) throw new Error("Failed to enroll lead");
  return res.json();
}

export async function getLeadEnrollments(leadId: string, signal?: AbortSignal): Promise<SequenceEnrollment[]> {
  const res = await fetch(`${API_BASE}/api/sequences/enrollments/lead/${leadId}`, { signal });
  if (!res.ok) throw new Error("Failed to fetch enrollments");
  return res.json();
}

export async function unenrollLead(enrollmentId: string): Promise<void> {
  const res = await fetch(`${API_BASE}/api/sequences/enrollments/${enrollmentId}`, { method: "DELETE" });
  if (!res.ok) throw new Error("Failed to unenroll");
}
