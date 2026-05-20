import type { CustomFieldDefinition, CustomFieldValue } from "@/types/sequences";

const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5156";

export async function getCustomFieldDefinitions(entityType?: string, signal?: AbortSignal): Promise<CustomFieldDefinition[]> {
  const url = `${API_BASE}/api/admin/custom-fields${entityType ? `?entityType=${entityType}` : ""}`;
  const res = await fetch(url, { signal });
  if (!res.ok) throw new Error("Failed to fetch custom field definitions");
  return res.json();
}

export async function createCustomFieldDefinition(body: Omit<CustomFieldDefinition, "id" | "createdAtUtc">): Promise<CustomFieldDefinition> {
  const res = await fetch(`${API_BASE}/api/admin/custom-fields`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error("Failed to create custom field");
  return res.json();
}

export async function updateCustomFieldDefinition(
  id: string,
  body: Pick<CustomFieldDefinition, "label" | "fieldType" | "options" | "isRequired" | "order">
): Promise<CustomFieldDefinition> {
  const res = await fetch(`${API_BASE}/api/admin/custom-fields/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error("Failed to update custom field");
  return res.json();
}

export async function deleteCustomFieldDefinition(id: string): Promise<void> {
  const res = await fetch(`${API_BASE}/api/admin/custom-fields/${id}`, { method: "DELETE" });
  if (!res.ok) throw new Error("Failed to delete custom field");
}

export async function getLeadCustomFieldValues(leadId: string, signal?: AbortSignal): Promise<CustomFieldValue[]> {
  const res = await fetch(`${API_BASE}/api/admin/custom-fields/values/lead/${leadId}`, { signal });
  if (!res.ok) throw new Error("Failed to fetch custom field values");
  return res.json();
}

export async function setLeadCustomFieldValue(leadId: string, fieldKey: string, value: string | null): Promise<void> {
  const res = await fetch(`${API_BASE}/api/admin/custom-fields/values/lead/${leadId}/${fieldKey}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ value }),
  });
  if (!res.ok) throw new Error("Failed to set custom field value");
}
