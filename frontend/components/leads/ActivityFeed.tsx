"use client";

import { useMemo, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Button } from "@/components/ui/Button";
import { EmptyState } from "@/components/ui/EmptyState";
import { ErrorState } from "@/components/ui/ErrorState";
import { Field } from "@/components/ui/Field";
import { SkeletonRows } from "@/components/ui/Skeleton";
import { useLeadActivitiesQuery } from "@/hooks/queries/useLeadActivitiesQuery";
import { queryKeys } from "@/hooks/queries/queryKeys";
import { useI18n } from "@/i18n/I18nProvider";
import type { TranslationKey } from "@/i18n/messages";
import { leadsService } from "@/services/leads.service";

const GUID_REGEX =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

const activityTypes = [
  "",
  "lead_created",
  "note_added",
  "email_sent",
  "stage_changed",
  "assigned",
  "score_changed",
  "call_logged",
  "whatsapp_sent",
  "whatsapp_received",
  "sequence_step_sent"
] as const;

const activityTypeLabelMap: Record<string, TranslationKey> = {
  lead_created: "leadActivities.type.lead_created",
  note_added: "leadActivities.type.note_added",
  email_sent: "leadActivities.type.email_sent",
  stage_changed: "leadActivities.type.stage_changed",
  assigned: "leadActivities.type.assigned",
  score_changed: "leadActivities.type.score_changed",
  call_logged: "leadActivities.type.call_logged",
  whatsapp_sent: "leadActivities.type.whatsapp_sent",
  whatsapp_received: "leadActivities.type.whatsapp_received",
  sequence_step_sent: "leadActivities.type.sequence_step_sent"
};

function getTypeLabelKey(activityType: string): TranslationKey {
  return activityTypeLabelMap[activityType] ?? "leadActivities.untitled";
}

function getTypeBadgeClass(activityType: string) {
  if (activityType === "lead_created") return "badge-success";
  if (activityType === "note_added") return "badge-info";
  if (activityType === "stage_changed") return "badge-warning";
  return "badge-muted";
}

export function ActivityFeed({ leadId }: { leadId: string }) {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [typeFilter, setTypeFilter] = useState("");
  const [note, setNote] = useState("");

  const isValidLeadId = useMemo(() => GUID_REGEX.test(leadId), [leadId]);

  const { data, isLoading, isFetching, error, refetch } = useLeadActivitiesQuery(
    leadId,
    page,
    pageSize,
    typeFilter,
    isValidLeadId
  );

  const addNoteMutation = useMutation({
    mutationFn: async () => {
      await leadsService.addNote({ leadId, note: note.trim() });
    },
    onSuccess: async () => {
      setNote("");
      await queryClient.invalidateQueries({
        queryKey: queryKeys.leads.activities(leadId, page, pageSize, typeFilter)
      });
    }
  });

  const items = data?.items ?? [];
  const hasMore = data?.hasMore ?? false;

  return (
    <section className="panel grid">
      <div className="row row-between">
        <Field label={t("leadActivities.typeFilter")} htmlFor="activity-type-filter">
          <select
            id="activity-type-filter"
            aria-label={t("leadActivities.typeFilter")}
            title={t("leadActivities.typeFilter")}
            value={typeFilter}
            onChange={(event) => {
              setPage(1);
              setTypeFilter(event.target.value);
            }}
          >
            {activityTypes.map((type) => (
              <option key={type || "all"} value={type}>
                {type ? t(getTypeLabelKey(type)) : t("leadActivities.type.all")}
              </option>
            ))}
          </select>
        </Field>

        <Field label={t("leadActivities.pageSize")} htmlFor="activity-page-size">
          <select
            id="activity-page-size"
            aria-label={t("leadActivities.pageSize")}
            title={t("leadActivities.pageSize")}
            value={pageSize}
            onChange={(event) => {
              setPage(1);
              setPageSize(Number(event.target.value));
            }}
          >
            <option value={10}>10</option>
            <option value={20}>20</option>
            <option value={50}>50</option>
          </select>
        </Field>

        <Button variant="ghost" onClick={() => void refetch()}>
          {t("common.refresh")}
        </Button>
      </div>

      <div className="grid">
        <Field label={t("leadActivities.addNote")} htmlFor="activity-note">
          <textarea
            id="activity-note"
            value={note}
            maxLength={2000}
            placeholder={t("leadActivities.notePlaceholder")}
            onChange={(event) => setNote(event.target.value)}
            rows={4}
          />
        </Field>
        <div className="row">
          <Button
            onClick={() => addNoteMutation.mutate()}
            disabled={!isValidLeadId || !note.trim() || addNoteMutation.isPending}
          >
            {t("leadActivities.addNoteAction")}
          </Button>
        </div>
      </div>

      {!isValidLeadId ? <ErrorState message={t("leadActivities.invalidLeadId")} /> : null}
      {error instanceof Error ? <ErrorState message={error.message} /> : null}
      {isLoading || isFetching ? <SkeletonRows rows={6} /> : null}

      {!isLoading && isValidLeadId && items.length === 0 ? (
        <EmptyState title={t("common.empty")} detail={t("leadActivities.empty")} />
      ) : null}

      <div className="activity-feed">
        {items.map((item) => (
          <article key={item.id} className="activity-item">
            <div className="row row-between">
              <span className={`badge ${getTypeBadgeClass(item.activityType)}`}>
                {t(getTypeLabelKey(item.activityType))}
              </span>
              <time className="muted">{new Date(item.occurredAtUtc).toLocaleString()}</time>
            </div>
            <p className="activity-title">{item.title ?? t("leadActivities.untitled")}</p>
            {item.description ? <p className="muted">{item.description}</p> : null}
            <p className="muted">
              {t("leadActivities.actor")}: {item.actor}
            </p>
          </article>
        ))}
      </div>

      <div className="row row-between">
        <p className="muted">
          {t("leadActivities.page")} {page}
        </p>
        <div className="row">
          <Button variant="ghost" disabled={page <= 1} onClick={() => setPage((current) => current - 1)}>
            {t("email.logs.previous")}
          </Button>
          <Button variant="ghost" disabled={!hasMore} onClick={() => setPage((current) => current + 1)}>
            {t("email.logs.next")}
          </Button>
        </div>
      </div>
    </section>
  );
}
