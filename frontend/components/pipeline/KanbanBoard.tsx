"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { Button } from "@/components/ui/Button";
import { EmptyState } from "@/components/ui/EmptyState";
import { ErrorState } from "@/components/ui/ErrorState";
import { Field } from "@/components/ui/Field";
import { PageHeader } from "@/components/ui/PageHeader";
import { SkeletonRows } from "@/components/ui/Skeleton";
import {
  useCreateOpportunityMutation,
  useMoveOpportunityMutation,
  usePipelineBoardQuery
} from "@/hooks/queries/usePipelineQueries";
import { usePersistedState } from "@/hooks/usePersistedState";
import { useI18n } from "@/i18n/I18nProvider";
import { trackUxEvent } from "@/services/uxTelemetry";
import type { Opportunity } from "@/types/lead";

export function KanbanBoard() {
  const { t } = useI18n();
  const leadIdInputRef = useRef<HTMLInputElement | null>(null);
  const [leadId, setLeadId] = useState("");
  const [title, setTitle] = useState(t("pipeline.defaultTitle"));
  const [value, setValue] = useState("1000");
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [bulkStageId, setBulkStageId] = useState("");
  const [viewFilter, setViewFilter] = usePersistedState<string>("mindflow.pipeline.viewFilter", "all");

  const { data: boardData, isLoading, isFetching, error, refetch } = usePipelineBoardQuery();
  const createOpportunityMutation = useCreateOpportunityMutation();
  const moveOpportunityMutation = useMoveOpportunityMutation();

  const board = boardData ?? { stages: [], opportunities: [] };

  const firstStage = useMemo(() => board.stages[0], [board.stages]);
  const availableForBulk = useMemo(
    () => board.stages.filter((stage) => stage.id !== viewFilter),
    [board.stages, viewFilter]
  );

  const opportunitiesByStage = useMemo(() => {
    const source =
      viewFilter === "all"
        ? board.opportunities
        : board.opportunities.filter((opportunity) => opportunity.stageId === viewFilter);

    return board.stages.map((stage) => ({
      stage,
      opportunities: source.filter((opportunity) => opportunity.stageId === stage.id)
    }));
  }, [board.opportunities, board.stages, viewFilter]);

  useEffect(() => {
    if (!isLoading && !isFetching) {
      trackUxEvent({ event: "view_loaded", screen: "pipeline" });
    }
  }, [isFetching, isLoading]);

  async function createOpportunity() {
    if (!firstStage) {
      return;
    }

    try {
      await createOpportunityMutation.mutateAsync({
        leadId,
        title,
        value: Number(value || 0),
        stageId: firstStage.id
      });
      setLeadId("");
      setTitle(t("pipeline.defaultTitle"));
      setValue("1000");
      trackUxEvent({
        event: "user_action",
        screen: "pipeline",
        detail: "create_opportunity"
      });
    } catch (err) {
      const message = err instanceof Error ? err.message : t("common.error");
      trackUxEvent({
        event: "request_error",
        screen: "pipeline",
        detail: message
      });
    }
  }

  async function moveOpportunity(opportunityId: string, targetStageId: string) {
    try {
      await moveOpportunityMutation.mutateAsync({ opportunityId, targetStageId });
      trackUxEvent({
        event: "user_action",
        screen: "pipeline",
        detail: "move_opportunity"
      });
    } catch (err) {
      const message = err instanceof Error ? err.message : t("common.error");
      trackUxEvent({
        event: "request_error",
        screen: "pipeline",
        detail: message
      });
    }
  }

  async function moveBulkSelection() {
    if (!bulkStageId || selectedIds.length === 0) {
      return;
    }

    await Promise.all(
      selectedIds.map((opportunityId) =>
        moveOpportunityMutation.mutateAsync({ opportunityId, targetStageId: bulkStageId })
      )
    );

    setSelectedIds([]);
    trackUxEvent({
      event: "user_action",
      screen: "pipeline",
      detail: `bulk_move:${selectedIds.length}`
    });
  }

  function toggleSelection(opportunityId: string) {
    setSelectedIds((current) =>
      current.includes(opportunityId)
        ? current.filter((id) => id !== opportunityId)
        : [...current, opportunityId]
    );
  }

  function applySavedView() {
    setViewFilter(bulkStageId || "all");
  }

  function focusQuickActions() {
    leadIdInputRef.current?.focus();
  }

  const errorMessage = error instanceof Error ? error.message : "";

  return (
    <section className="panel grid">
      <PageHeader
        title={t("pipeline.title")}
        subtitle={t("pipeline.subtitle")}
        actions={
          <div className="row">
            <Button variant="ghost" onClick={focusQuickActions}>
              {t("pipeline.focusQuickActions")}
            </Button>
            <Button variant="ghost" onClick={() => void refetch()}>
              {t("common.refresh")}
            </Button>
          </div>
        }
      />

      <div className="grid" data-testid="pipeline-visual-shell">
        <div className="row" role="group" aria-label={t("pipeline.quickActions")}>
          <Field label={t("pipeline.leadId")} htmlFor="opportunity-lead-id">
            <input
              ref={leadIdInputRef}
              id="opportunity-lead-id"
              placeholder={t("pipeline.leadIdPlaceholder")}
              aria-label={t("pipeline.leadId")}
              value={leadId}
              onChange={(event) => setLeadId(event.target.value)}
            />
          </Field>
          <Field label={t("pipeline.opportunityTitle")} htmlFor="opportunity-title">
            <input
              id="opportunity-title"
              aria-label={t("pipeline.opportunityTitle")}
              value={title}
              onChange={(event) => setTitle(event.target.value)}
            />
          </Field>
          <Field label={t("pipeline.opportunityValue")} htmlFor="opportunity-value">
            <input
              id="opportunity-value"
              type="number"
              aria-label={t("pipeline.opportunityValue")}
              value={value}
              onChange={(event) => setValue(event.target.value)}
            />
          </Field>
          <Button onClick={createOpportunity} disabled={!leadId.trim()}>
            {t("pipeline.createOpportunity")}
          </Button>
        </div>

        <div className="row" role="group" aria-label={t("pipeline.bulkActions")}>
          <Field label={t("pipeline.savedView")} htmlFor="pipeline-view-filter">
            <select
              id="pipeline-view-filter"
              aria-label={t("pipeline.savedView")}
              title={t("pipeline.savedView")}
              value={viewFilter}
              onChange={(event) => setViewFilter(event.target.value)}
            >
              <option value="all">{t("pipeline.allStages")}</option>
              {board.stages.map((stage) => (
                <option key={stage.id} value={stage.id}>
                  {stage.name}
                </option>
              ))}
            </select>
          </Field>
          <Field label={t("pipeline.bulkMove")} htmlFor="pipeline-bulk-stage">
            <select
              id="pipeline-bulk-stage"
              aria-label={t("pipeline.bulkMove")}
              title={t("pipeline.bulkMove")}
              value={bulkStageId}
              onChange={(event) => setBulkStageId(event.target.value)}
            >
              <option value="">{t("pipeline.selectTargetStage")}</option>
              {availableForBulk.map((stage) => (
                <option key={stage.id} value={stage.id}>
                  {stage.name}
                </option>
              ))}
            </select>
          </Field>
          <Button variant="ghost" onClick={applySavedView}>
            {t("pipeline.applyView")}
          </Button>
          <Button
            onClick={() => void moveBulkSelection()}
            disabled={selectedIds.length === 0 || !bulkStageId || moveOpportunityMutation.isPending}
          >
            {t("pipeline.bulkMove")}: {selectedIds.length}
          </Button>
        </div>
      </div>

      {isLoading || isFetching ? <SkeletonRows rows={4} /> : null}
      {errorMessage ? <ErrorState message={errorMessage} /> : null}

      {!isLoading && board.stages.length === 0 ? (
        <EmptyState title={t("common.empty")} detail={t("pipeline.noStages")} />
      ) : null}

      <div className="board">
        {opportunitiesByStage.map(({ stage, opportunities: stageItems }) => {

          return (
            <section className="column" key={stage.id}>
              <h3>{stage.name}</h3>
              <div className="items">
                {stageItems.length === 0 ? (
                  <EmptyState
                    title={t("pipeline.noOpportunities")}
                    detail={t("pipeline.stageEmpty")}
                  />
                ) : null}
                {stageItems.map((opportunity) => (
                  <article className="card" key={opportunity.id}>
                    <label className="row" htmlFor={`select-${opportunity.id}`}>
                      <input
                        id={`select-${opportunity.id}`}
                        type="checkbox"
                        aria-label={`${t("pipeline.selectOpportunity")} ${opportunity.title}`}
                        checked={selectedIds.includes(opportunity.id)}
                        onChange={() => toggleSelection(opportunity.id)}
                      />
                      <span>{t("pipeline.selectOpportunity")}</span>
                    </label>
                    <strong>{opportunity.title}</strong>
                    <span className="muted">
                      {t("pipeline.lead")}: {opportunity.leadId}
                    </span>
                    <span className="muted">
                      {t("pipeline.value")}: {opportunity.value}
                    </span>
                    <div className="row">
                      <select
                        aria-label={`${t("pipeline.moveToStage")} ${opportunity.title}`}
                        title={`${t("pipeline.moveToStage")} ${opportunity.title}`}
                        value={opportunity.stageId}
                        onChange={(event) =>
                          void moveOpportunity(opportunity.id, event.currentTarget.value)
                        }
                      >
                        {board.stages.map((target) => (
                          <option key={target.id} value={target.id}>
                            {target.name}
                          </option>
                        ))}
                      </select>
                    </div>
                  </article>
                ))}
              </div>
            </section>
          );
        })}
      </div>
    </section>
  );
}
