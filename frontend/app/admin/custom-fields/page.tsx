"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useI18n } from "@/i18n/I18nProvider";
import * as cfService from "@/services/customFields.service";
import type { CustomFieldDefinition } from "@/types/sequences";

const FIELD_TYPES = ["text", "number", "date", "select", "boolean"] as const;
const ENTITY_TYPES = ["Lead", "Contact"] as const;

type DraftField = Omit<CustomFieldDefinition, "id" | "createdAtUtc">;

const emptyDraft = (): DraftField => ({
  key: "",
  label: "",
  fieldType: "text",
  entityType: "Lead",
  options: "",
  isRequired: false,
  order: 0,
});

export default function CustomFieldsPage() {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const [filter, setFilter] = useState<string>("Lead");
  const [draft, setDraft] = useState<DraftField | null>(null);
  const [editingId, setEditingId] = useState<string | null>(null);

  const { data: definitions = [], isLoading } = useQuery({
    queryKey: ["custom-fields", filter],
    queryFn: ({ signal }) => cfService.getCustomFieldDefinitions(filter || undefined, signal),
  });

  const createMutation = useMutation({
    mutationFn: (d: DraftField) => cfService.createCustomFieldDefinition(d),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ["custom-fields"] }); setDraft(null); },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, d }: { id: string; d: Partial<DraftField> }) =>
      cfService.updateCustomFieldDefinition(id, {
        label: d.label ?? "",
        fieldType: d.fieldType ?? "text",
        options: d.options,
        isRequired: d.isRequired ?? false,
        order: d.order ?? 0,
      }),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ["custom-fields"] }); setEditingId(null); },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => cfService.deleteCustomFieldDefinition(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["custom-fields"] }),
  });

  if (isLoading) return <p className="p-4">{t("common.loading")}</p>;

  return (
    <main className="p-6 max-w-3xl mx-auto">
      <div className="flex items-center justify-between mb-4">
        <div>
          <h1 className="text-2xl font-bold">{t("customFields.title")}</h1>
          <p className="text-gray-500 text-sm">{t("customFields.subtitle")}</p>
        </div>
        <button className="btn btn-primary" onClick={() => { setDraft(emptyDraft()); setEditingId(null); }}>
          {t("customFields.create")}
        </button>
      </div>

      <div className="mb-4 flex gap-2">
        {ENTITY_TYPES.map((e) => (
          <button
            key={e}
            className={`btn btn-sm ${filter === e ? "btn-primary" : "btn-secondary"}`}
            onClick={() => setFilter(e)}
          >
            {e}
          </button>
        ))}
      </div>

      {/* Create form */}
      {draft && (
        <form
          className="border rounded p-4 mb-6 bg-white space-y-3"
          onSubmit={(ev) => { ev.preventDefault(); createMutation.mutate(draft); }}
        >
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="text-sm font-medium">{t("customFields.key")}</label>
              <input className="input input-sm w-full" value={draft.key} onChange={(e) => setDraft({ ...draft, key: e.target.value })} required maxLength={64} />
            </div>
            <div>
              <label className="text-sm font-medium">{t("customFields.label")}</label>
              <input className="input input-sm w-full" value={draft.label} onChange={(e) => setDraft({ ...draft, label: e.target.value })} required maxLength={120} />
            </div>
            <div>
              <label className="text-sm font-medium">{t("customFields.fieldType")}</label>
              <select className="input input-sm w-full" value={draft.fieldType} onChange={(e) => setDraft({ ...draft, fieldType: e.target.value })}>
                {FIELD_TYPES.map((ft) => <option key={ft} value={ft}>{t(`customFields.type.${ft}` as any)}</option>)}
              </select>
            </div>
            <div>
              <label className="text-sm font-medium">{t("customFields.entityType")}</label>
              <select className="input input-sm w-full" value={draft.entityType} onChange={(e) => setDraft({ ...draft, entityType: e.target.value })}>
                {ENTITY_TYPES.map((et) => <option key={et} value={et}>{et}</option>)}
              </select>
            </div>
            {draft.fieldType === "select" && (
              <div className="col-span-2">
                <label className="text-sm font-medium">{t("customFields.options")}</label>
                <input className="input input-sm w-full" value={draft.options ?? ""} onChange={(e) => setDraft({ ...draft, options: e.target.value })} maxLength={2000} />
              </div>
            )}
            <div>
              <label className="text-sm font-medium">{t("customFields.order")}</label>
              <input type="number" className="input input-sm w-full" value={draft.order} onChange={(e) => setDraft({ ...draft, order: parseInt(e.target.value) || 0 })} />
            </div>
            <div className="flex items-center gap-2 mt-4">
              <input type="checkbox" id="cf-required" checked={draft.isRequired} onChange={(e) => setDraft({ ...draft, isRequired: e.target.checked })} />
              <label htmlFor="cf-required" className="text-sm">{t("customFields.isRequired")}</label>
            </div>
          </div>
          <div className="flex gap-2">
            <button type="submit" className="btn btn-primary btn-sm">{t("customFields.save")}</button>
            <button type="button" className="btn btn-secondary btn-sm" onClick={() => setDraft(null)}>Cancel</button>
          </div>
        </form>
      )}

      {definitions.length === 0 && !draft && (
        <p className="text-gray-400 text-sm">{t("customFields.empty")}</p>
      )}

      <div className="space-y-2">
        {definitions.map((def) => (
          <div key={def.id} className="border rounded p-3 bg-white flex items-center justify-between">
            <div>
              <span className="font-mono text-sm font-semibold">{def.key}</span>
              <span className="ml-2 text-gray-600 text-sm">{def.label}</span>
              <span className="ml-2 badge badge-info text-xs">{def.fieldType}</span>
              {def.isRequired && <span className="ml-1 text-red-500 text-xs font-medium">required</span>}
              {def.options && <span className="ml-2 text-gray-400 text-xs">[{def.options}]</span>}
            </div>
            <div className="flex gap-2">
              <button className="btn btn-secondary btn-sm text-xs" onClick={() => setEditingId(editingId === def.id ? null : def.id)}>Edit</button>
              <button className="btn btn-sm text-red-500 text-xs border-red-200 hover:bg-red-50" onClick={() => deleteMutation.mutate(def.id)}>
                {t("customFields.delete")}
              </button>
            </div>
          </div>
        ))}
      </div>
    </main>
  );
}
