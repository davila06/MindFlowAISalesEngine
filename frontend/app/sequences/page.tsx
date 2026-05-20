"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useI18n } from "@/i18n/I18nProvider";
import * as sequencesService from "@/services/sequences.service";
import type { Sequence, SequenceStep } from "@/types/sequences";

type StepInput = Omit<SequenceStep, "id">;

function SequenceStepEditor({
  steps,
  onChange,
}: {
  steps: StepInput[];
  onChange: (steps: StepInput[]) => void;
}) {
  const { t } = useI18n();

  const add = () =>
    onChange([
      ...steps,
      { order: steps.length + 1, actionType: "send_email", actionValue: "", delayDays: 0 },
    ]);

  const remove = (i: number) => onChange(steps.filter((_, idx) => idx !== i));

  const update = (i: number, field: keyof StepInput, value: string | number) => {
    const next = [...steps];
    next[i] = { ...next[i], [field]: value };
    onChange(next);
  };

  return (
    <div className="space-y-2">
      {steps.map((step, i) => (
        <div key={i} className="grid grid-cols-4 gap-2 items-start border rounded p-2 bg-gray-50">
          <input
            type="number"
            min={1}
            className="input input-sm"
            placeholder={t("sequences.step.order")}
            value={step.order}
            onChange={(e) => update(i, "order", parseInt(e.target.value) || 1)}
          />
          <select
            className="input input-sm"
            value={step.actionType}
            onChange={(e) => update(i, "actionType", e.target.value)}
          >
            <option value="send_email">send_email</option>
            <option value="add_note">add_note</option>
            <option value="add_tag">add_tag</option>
          </select>
          <input
            className="input input-sm"
            placeholder={t("sequences.step.actionValue")}
            value={step.actionValue}
            onChange={(e) => update(i, "actionValue", e.target.value)}
          />
          <div className="flex gap-1">
            <input
              type="number"
              min={0}
              className="input input-sm w-20"
              placeholder={t("sequences.step.delayDays")}
              value={step.delayDays}
              onChange={(e) => update(i, "delayDays", parseInt(e.target.value) || 0)}
            />
            <button
              type="button"
              onClick={() => remove(i)}
              className="text-red-500 hover:text-red-700 px-1 text-xs"
              aria-label="Remove step"
            >
              ✕
            </button>
          </div>
        </div>
      ))}
      <button type="button" onClick={add} className="btn btn-secondary btn-sm">
        + {t("sequences.step.addStep")}
      </button>
    </div>
  );
}

function SequenceForm({
  initial,
  onSave,
  onCancel,
}: {
  initial?: Sequence;
  onSave: (data: { name: string; description: string; isActive: boolean; steps: StepInput[] }) => void;
  onCancel: () => void;
}) {
  const { t } = useI18n();
  const [name, setName] = useState(initial?.name ?? "");
  const [description, setDescription] = useState(initial?.description ?? "");
  const [isActive, setIsActive] = useState(initial?.isActive ?? true);
  const [steps, setSteps] = useState<StepInput[]>(
    initial?.steps.map((s) => ({ order: s.order, actionType: s.actionType, actionValue: s.actionValue, delayDays: s.delayDays })) ?? []
  );

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        onSave({ name, description, isActive, steps });
      }}
      className="space-y-3 p-4 border rounded bg-white"
    >
      <div>
        <label className="label-text text-sm font-medium">{t("sequences.name")}</label>
        <input
          className="input input-sm w-full"
          value={name}
          onChange={(e) => setName(e.target.value)}
          required
          maxLength={200}
        />
      </div>
      <div>
        <label className="label-text text-sm font-medium">{t("sequences.description")}</label>
        <textarea
          className="input input-sm w-full"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          maxLength={500}
          rows={2}
        />
      </div>
      <div className="flex items-center gap-2">
        <input
          type="checkbox"
          id="seq-active"
          checked={isActive}
          onChange={(e) => setIsActive(e.target.checked)}
        />
        <label htmlFor="seq-active" className="text-sm">{t("sequences.isActive")}</label>
      </div>
      <div>
        <p className="text-sm font-medium mb-1">{t("sequences.steps")}</p>
        <SequenceStepEditor steps={steps} onChange={setSteps} />
      </div>
      <div className="flex gap-2">
        <button type="submit" className="btn btn-primary btn-sm">{t("sequences.save")}</button>
        <button type="button" className="btn btn-secondary btn-sm" onClick={onCancel}>
          Cancel
        </button>
      </div>
    </form>
  );
}

export default function SequencesPage() {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState<Sequence | null | "new">(null);
  const [enrollSeqId, setEnrollSeqId] = useState<string | null>(null);
  const [enrollLeadId, setEnrollLeadId] = useState("");
  const [enrollMsg, setEnrollMsg] = useState("");

  const { data: sequences = [], isLoading } = useQuery({
    queryKey: ["sequences"],
    queryFn: ({ signal }) => sequencesService.getSequences(signal),
  });

  const createMutation = useMutation({
    mutationFn: (data: { name: string; description: string; isActive: boolean; steps: StepInput[] }) =>
      sequencesService.createSequence({ name: data.name, description: data.description || undefined, steps: data.steps }),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ["sequences"] }); setEditing(null); },
  });

  const updateMutation = useMutation({
    mutationFn: (args: { id: string; data: { name: string; description: string; isActive: boolean; steps: StepInput[] } }) =>
      sequencesService.updateSequence(args.id, { name: args.data.name, description: args.data.description || undefined, isActive: args.data.isActive, steps: args.data.steps }),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ["sequences"] }); setEditing(null); },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => sequencesService.deleteSequence(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["sequences"] }),
  });

  const enrollMutation = useMutation({
    mutationFn: ({ seqId, leadId }: { seqId: string; leadId: string }) =>
      sequencesService.enrollLead(seqId, leadId),
    onSuccess: () => {
      setEnrollMsg(t("sequences.enrollSuccess"));
      setEnrollLeadId("");
      setTimeout(() => setEnrollMsg(""), 3000);
    },
    onError: (err: Error) => setEnrollMsg(err.message),
  });

  if (isLoading) return <p className="p-4">{t("common.loading")}</p>;

  return (
    <main className="p-6 max-w-4xl mx-auto">
      <div className="flex items-center justify-between mb-4">
        <div>
          <h1 className="text-2xl font-bold">{t("sequences.title")}</h1>
          <p className="text-gray-500 text-sm">{t("sequences.subtitle")}</p>
        </div>
        <button className="btn btn-primary" onClick={() => setEditing("new")}>
          {t("sequences.create")}
        </button>
      </div>

      {editing === "new" && (
        <div className="mb-6">
          <SequenceForm
            onSave={(data) => createMutation.mutate(data)}
            onCancel={() => setEditing(null)}
          />
        </div>
      )}

      {sequences.length === 0 && !editing && (
        <p className="text-gray-400 text-sm">{t("sequences.empty")}</p>
      )}

      <div className="space-y-4">
        {sequences.map((seq) => (
          <div key={seq.id} className="border rounded-lg p-4 bg-white shadow-sm">
            {editing !== null && typeof editing === "object" && editing.id === seq.id ? (
              <SequenceForm
                initial={seq}
                onSave={(data) => updateMutation.mutate({ id: seq.id, data })}
                onCancel={() => setEditing(null)}
              />
            ) : (
              <>
                <div className="flex items-start justify-between">
                  <div>
                    <h2 className="font-semibold text-lg">{seq.name}</h2>
                    {seq.description && <p className="text-gray-500 text-sm">{seq.description}</p>}
                    <span className={`text-xs px-2 py-0.5 rounded-full mt-1 inline-block ${seq.isActive ? "bg-green-100 text-green-700" : "bg-gray-100 text-gray-500"}`}>
                      {seq.isActive ? t("sequences.activeLabel") : t("sequences.inactiveLabel")}
                    </span>
                    <p className="text-xs text-gray-400 mt-1">{seq.steps.length} steps</p>
                  </div>
                  <div className="flex gap-2">
                    <button className="btn btn-secondary btn-sm" onClick={() => setEditing(seq)}>Edit</button>
                    <button className="btn btn-sm text-red-500 border-red-200 hover:bg-red-50" onClick={() => deleteMutation.mutate(seq.id)}>
                      {t("sequences.delete")}
                    </button>
                    <button className="btn btn-primary btn-sm" onClick={() => setEnrollSeqId(seq.id)}>
                      {t("sequences.enroll")}
                    </button>
                  </div>
                </div>

                {enrollSeqId === seq.id && (
                  <div className="mt-3 flex gap-2 items-center">
                    <input
                      className="input input-sm flex-1"
                      placeholder={t("sequences.enrollLeadId")}
                      value={enrollLeadId}
                      onChange={(e) => setEnrollLeadId(e.target.value)}
                    />
                    <button
                      className="btn btn-primary btn-sm"
                      onClick={() => { enrollMutation.mutate({ seqId: seq.id, leadId: enrollLeadId }); setEnrollSeqId(null); }}
                      disabled={!enrollLeadId.trim()}
                    >
                      OK
                    </button>
                    <button className="btn btn-secondary btn-sm" onClick={() => setEnrollSeqId(null)}>✕</button>
                  </div>
                )}
                {enrollMsg && enrollSeqId === null && <p className="text-green-600 text-sm mt-1">{enrollMsg}</p>}
              </>
            )}
          </div>
        ))}
      </div>
    </main>
  );
}
