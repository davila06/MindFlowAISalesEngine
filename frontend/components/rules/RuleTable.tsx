"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { Button } from "@/components/ui/Button";
import { ConfirmDialog } from "@/components/ui/ConfirmDialog";
import { EmptyState } from "@/components/ui/EmptyState";
import { ErrorState } from "@/components/ui/ErrorState";
import { Field } from "@/components/ui/Field";
import { PageHeader } from "@/components/ui/PageHeader";
import { SkeletonRows } from "@/components/ui/Skeleton";
import { useRulesQuery, useToggleRuleMutation } from "@/hooks/queries/useRulesQueries";
import { TableContainer } from "@/components/ui/TableContainer";
import { useDebouncedValue } from "@/hooks/useDebouncedValue";
import { useI18n } from "@/i18n/I18nProvider";
import { usePersistedState } from "@/hooks/usePersistedState";
import { trackUxEvent } from "@/services/uxTelemetry";
import type { Rule } from "@/types/rule";

export function RuleTable() {
  const [query, setQuery] = usePersistedState<string>("mindflow.rules.query", "");
  const debouncedQuery = useDebouncedValue(query, 300);
  const [pendingUndoRule, setPendingUndoRule] = useState<Rule | null>(null);
  const [confirmingRule, setConfirmingRule] = useState<Rule | null>(null);
  const undoTimerRef = useRef<number | null>(null);
  const { t } = useI18n();

  useEffect(() => {
    return () => {
      if (undoTimerRef.current) {
        window.clearTimeout(undoTimerRef.current);
      }
    };
  }, []);

  const { data: rules = [], error, isLoading, isFetching, refetch } = useRulesQuery();
  const toggleMutation = useToggleRuleMutation();

  async function toggle(rule: Rule) {
    try {
      if (rule.isActive) {
        await toggleMutation.mutateAsync({ ruleId: rule.id, nextActive: false });
        setPendingUndoRule(rule);

        if (undoTimerRef.current) {
          window.clearTimeout(undoTimerRef.current);
        }

        undoTimerRef.current = window.setTimeout(() => {
          setPendingUndoRule(null);
          undoTimerRef.current = null;
        }, 7000);
      } else {
        await toggleMutation.mutateAsync({ ruleId: rule.id, nextActive: true });
      }

      trackUxEvent({
        event: "user_action",
        screen: "rules",
        detail: rule.isActive ? "deactivate_rule" : "activate_rule"
      });
    } catch (err) {
      const message = err instanceof Error ? err.message : t("common.error");
      trackUxEvent({
        event: "request_error",
        screen: "rules",
        detail: message
      });
    }
  }

  async function undoDeactivate() {
    if (!pendingUndoRule) {
      return;
    }

    try {
      await toggleMutation.mutateAsync({ ruleId: pendingUndoRule.id, nextActive: true });
      setPendingUndoRule(null);
      if (undoTimerRef.current) {
        window.clearTimeout(undoTimerRef.current);
        undoTimerRef.current = null;
      }
      trackUxEvent({
        event: "user_action",
        screen: "rules",
        detail: "undo_deactivate"
      });
    } catch (err) {
      const message = err instanceof Error ? err.message : t("common.error");
      trackUxEvent({
        event: "request_error",
        screen: "rules",
        detail: message
      });
    }
  }

  const filteredRules = useMemo(
    () =>
      rules.filter(
        (rule) =>
          rule.name.toLowerCase().includes(debouncedQuery.toLowerCase()) ||
          rule.trigger.toLowerCase().includes(debouncedQuery.toLowerCase())
      ),
    [rules, debouncedQuery]
  );

  const errorMessage = error instanceof Error ? error.message : "";

  return (
    <section className="panel grid">
      <PageHeader title={t("rules.title")} subtitle={t("rules.subtitle")} />

      <div className="row row-between">
        <Field label={t("rules.filter")} htmlFor="rules-filter">
          <input
            id="rules-filter"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder={t("rules.filterPlaceholder")}
          />
        </Field>
        <Button variant="ghost" onClick={() => void refetch()}>
          {t("common.refresh")}
        </Button>
      </div>

      {pendingUndoRule ? (
        <div className="banner banner-warning" role="status" aria-live="polite">
          {t("rules.deactivatedBannerPrefix")} <strong>{pendingUndoRule.name}</strong>{" "}
          {t("rules.deactivatedBannerSuffix")}
          <Button variant="ghost" onClick={() => void undoDeactivate()}>
            {t("common.undo")}
          </Button>
        </div>
      ) : null}

      {errorMessage ? <ErrorState message={errorMessage} /> : null}
      {isLoading || isFetching ? <SkeletonRows rows={5} /> : null}

      {!isLoading && filteredRules.length === 0 ? (
        <EmptyState title={t("common.empty")} detail={t("rules.noMatches")} />
      ) : null}

      <TableContainer>
        <table className="table" data-responsive="true">
          <thead>
            <tr>
              <th scope="col">{t("rules.name")}</th>
              <th scope="col">{t("rules.trigger")}</th>
              <th scope="col">{t("common.status")}</th>
              <th scope="col">{t("common.actions")}</th>
            </tr>
          </thead>
          <tbody>
            {filteredRules.map((rule) => (
              <tr key={rule.id}>
                <td data-label={t("rules.name")}>{rule.name}</td>
                <td data-label={t("rules.trigger")}>{rule.trigger}</td>
                <td data-label={t("common.status")}>
                  <span className="pill">
                    {rule.isActive ? t("common.active") : t("common.inactive")}
                  </span>
                </td>
                <td data-label={t("common.actions")}>
                  <Button
                    variant={rule.isActive ? "danger" : "primary"}
                    onClick={() => {
                      if (rule.isActive) {
                        setConfirmingRule(rule);
                        return;
                      }

                      void toggle(rule);
                    }}
                  >
                    {rule.isActive ? t("rules.deactivate") : t("rules.activate")}
                  </Button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </TableContainer>

      <ConfirmDialog
        open={Boolean(confirmingRule)}
        title={confirmingRule ? `${t("rules.confirmDeactivatePrefix")} "${confirmingRule.name}"?` : ""}
        description={t("rules.confirmDeactivateSuffix")}
        cancelLabel={t("common.cancel")}
        confirmLabel={t("rules.deactivate")}
        onCancel={() => setConfirmingRule(null)}
        onConfirm={() => {
          if (!confirmingRule) {
            return;
          }

          const target = confirmingRule;
          setConfirmingRule(null);
          void toggle(target);
        }}
      />
    </section>
  );
}
