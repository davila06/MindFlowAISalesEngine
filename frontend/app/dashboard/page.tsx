"use client";

import { useEffect, useMemo, useRef } from "react";
import { Button } from "@/components/ui/Button";
import { EmptyState } from "@/components/ui/EmptyState";
import { ErrorState } from "@/components/ui/ErrorState";
import { Field } from "@/components/ui/Field";
import { KpiCard } from "@/components/ui/KpiCard";
import { PageHeader } from "@/components/ui/PageHeader";
import { SkeletonRows } from "@/components/ui/Skeleton";
import { TableContainer } from "@/components/ui/TableContainer";
import { useDashboardOverviewQuery } from "@/hooks/queries/useDashboardOverviewQuery";
import { useDebouncedValue } from "@/hooks/useDebouncedValue";
import { useI18n } from "@/i18n/I18nProvider";
import { usePersistedState } from "@/hooks/usePersistedState";
import { trackUxEvent } from "@/services/uxTelemetry";

export default function DashboardPage() {
  const [days, setDays] = usePersistedState<number>("mindflow.dashboard.days", 7);
  const debouncedDays = useDebouncedValue(days, 350);
  const { data: overview, isLoading, isFetching, error, refetch } = useDashboardOverviewQuery(
    debouncedDays
  );
  const startedAtRef = useRef<number>(0);
  const hasTrackedInsightRef = useRef(false);
  const { t } = useI18n();

  const hasRows = useMemo(
    () => (overview?.leadsPerDay?.length ?? 0) > 0,
    [overview?.leadsPerDay]
  );

  useEffect(() => {
    if (isLoading || isFetching) {
      if (!hasTrackedInsightRef.current) {
        startedAtRef.current = performance.now();
      }
      return;
    }

    trackUxEvent({
      event: "view_loaded",
      screen: "dashboard",
      detail: `days:${debouncedDays}`
    });

    if (!hasTrackedInsightRef.current) {
      const durationMs = Math.max(0, performance.now() - startedAtRef.current);
      trackUxEvent({
        event: "time_to_insight",
        screen: "dashboard",
        value: Number(durationMs.toFixed(2))
      });
      hasTrackedInsightRef.current = true;
    }
  }, [debouncedDays, isFetching, isLoading]);

  useEffect(() => {
    if (error instanceof Error) {
      trackUxEvent({
        event: "request_error",
        screen: "dashboard",
        detail: error.message
      });
    }
  }, [error]);

  const errorMessage = error instanceof Error ? error.message : "";

  return (
    <section className="grid">
      <article className="panel grid">
        <PageHeader
          title={t("dashboard.title")}
          subtitle={t("dashboard.subtitle")}
          actions={
            <>
              <Field label={t("dashboard.daysWindow")} htmlFor="dashboard-days">
                <input
                  id="dashboard-days"
                  type="number"
                  min={1}
                  max={30}
                  value={days}
                  aria-label={t("dashboard.daysWindow")}
                  onChange={(event) => setDays(Number(event.target.value || 7))}
                />
              </Field>
              <Button
                variant="ghost"
                onClick={() => {
                  trackUxEvent({
                    event: "user_action",
                    screen: "dashboard",
                    detail: "manual_refresh"
                  });
                  void refetch();
                }}
              >
                {t("common.refresh")}
              </Button>
            </>
          }
        />

        {errorMessage ? <ErrorState message={errorMessage} /> : null}

        {(isLoading || isFetching) && !overview ? (
          <SkeletonRows rows={3} />
        ) : (
          <div className="cards">
            <KpiCard
              label={t("dashboard.totalLeads")}
              value={String(overview?.totalLeads ?? 0)}
            />
            <KpiCard
              label={t("dashboard.conversionRate")}
              value={`${overview?.conversionRate ?? 0}%`}
            />
            <KpiCard
              label={t("dashboard.pipelineValue")}
              value={Number(overview?.pipelineValue ?? 0).toLocaleString()}
            />
          </div>
        )}
      </article>

      <article className="panel">
        <h2>{t("dashboard.leadsPerDay")}</h2>
        {!isLoading && !hasRows ? (
          <EmptyState title={t("common.empty")} detail={t("dashboard.noLeads")} />
        ) : (
          <TableContainer>
            <table className="table" data-responsive="true">
              <thead>
                <tr>
                  <th scope="col">{t("dashboard.date")}</th>
                  <th scope="col">{t("dashboard.count")}</th>
                </tr>
              </thead>
              <tbody>
                {(overview?.leadsPerDay ?? []).map((item) => (
                  <tr key={item.date}>
                    <td data-label={t("dashboard.date")}>{item.date}</td>
                    <td data-label={t("dashboard.count")}>{item.count}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </TableContainer>
        )}
      </article>
    </section>
  );
}
