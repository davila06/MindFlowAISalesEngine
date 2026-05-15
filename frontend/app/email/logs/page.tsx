"use client";

import { useEffect, useState } from "react";
import { Button } from "@/components/ui/Button";
import { EmptyState } from "@/components/ui/EmptyState";
import { ErrorState } from "@/components/ui/ErrorState";
import { Field } from "@/components/ui/Field";
import { useEmailLogsQuery } from "@/hooks/queries/useEmailLogsQuery";
import { PageHeader } from "@/components/ui/PageHeader";
import { SkeletonRows } from "@/components/ui/Skeleton";
import { TableContainer } from "@/components/ui/TableContainer";
import { useDebouncedValue } from "@/hooks/useDebouncedValue";
import { useI18n } from "@/i18n/I18nProvider";
import { usePersistedState } from "@/hooks/usePersistedState";
import { trackUxEvent } from "@/services/uxTelemetry";

export default function EmailLogsPage() {
  const [query, setQuery] = usePersistedState<string>("mindflow.email.logs.query", "");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = usePersistedState<number>("mindflow.email.logs.pageSize", 20);
  const debouncedQuery = useDebouncedValue(query, 300);
  const { t } = useI18n();

  const { data, isLoading, isFetching, error, refetch } = useEmailLogsQuery(
    page,
    pageSize,
    debouncedQuery
  );

  const logs = data?.items ?? [];
  const hasMore = data?.hasMore ?? false;
  const errorMessage = error instanceof Error ? error.message : "";

  useEffect(() => {
    if (!isLoading && !isFetching) {
      trackUxEvent({ event: "view_loaded", screen: "email_logs" });
    }
  }, [isFetching, isLoading]);

  return (
    <section className="panel grid">
      <PageHeader
        title={t("email.logs.title")}
        subtitle={t("email.logs.subtitle")}
        actions={
          <Button variant="ghost" onClick={() => void refetch()}>
            {t("common.refresh")}
          </Button>
        }
      />

      <div className="row row-between">
        <Field label={t("email.logs.filter")} htmlFor="email-logs-filter">
          <input
            id="email-logs-filter"
            value={query}
            onChange={(event) => {
              setPage(1);
              setQuery(event.target.value);
            }}
            placeholder={t("email.logs.filterPlaceholder")}
          />
        </Field>
        <Field label={t("email.logs.pageSize")} htmlFor="email-logs-page-size">
          <select
            id="email-logs-page-size"
            aria-label={t("email.logs.pageSize")}
            title={t("email.logs.pageSize")}
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
        <Button
          variant="ghost"
          onClick={() => {
            setQuery("");
            setPage(1);
            trackUxEvent({
              event: "user_action",
              screen: "email_logs",
              detail: "clear_filter"
            });
          }}
        >
          {t("common.clearFilter")}
        </Button>
      </div>

      {errorMessage ? <ErrorState message={errorMessage} /> : null}
      {isLoading || isFetching ? <SkeletonRows rows={8} /> : null}

      {!isLoading && logs.length === 0 ? (
        <EmptyState title={t("common.empty")} detail={t("email.logs.noMatches")} />
      ) : null}

      <TableContainer>
        <table className="table" data-responsive="true">
          <thead>
            <tr>
              <th scope="col">{t("email.logs.date")}</th>
              <th scope="col">{t("email.logs.template")}</th>
              <th scope="col">{t("email.logs.recipient")}</th>
              <th scope="col">{t("common.status")}</th>
              <th scope="col">{t("email.logs.errorColumn")}</th>
            </tr>
          </thead>
          <tbody>
            {logs.map((log) => (
              <tr key={log.id}>
                <td data-label={t("email.logs.date")}>{new Date(log.sentAtUtc).toLocaleString()}</td>
                <td data-label={t("email.logs.template")}>{log.templateName}</td>
                <td data-label={t("email.logs.recipient")}>{log.toEmail ?? t("email.logs.masked")}</td>
                <td data-label={t("common.status")}>{log.status}</td>
                <td data-label={t("email.logs.errorColumn")}>{log.errorMessage ?? "-"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </TableContainer>

      <div className="row row-between">
        <p className="muted">
          {t("email.logs.page")} {page}
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
