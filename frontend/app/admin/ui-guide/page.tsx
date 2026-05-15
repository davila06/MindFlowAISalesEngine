"use client";

import { Button } from "@/components/ui/Button";
import { EmptyState } from "@/components/ui/EmptyState";
import { ErrorState } from "@/components/ui/ErrorState";
import { Field } from "@/components/ui/Field";
import { KpiCard } from "@/components/ui/KpiCard";
import { PageHeader } from "@/components/ui/PageHeader";
import { Skeleton } from "@/components/ui/Skeleton";
import { useI18n } from "@/i18n/I18nProvider";

export default function UiGuidePage() {
  const { t } = useI18n();

  return (
    <section className="panel grid">
      <PageHeader title={t("uiGuide.title")} subtitle={t("uiGuide.subtitle")} />

      <article className="grid">
        <h2>{t("uiGuide.buttons")}</h2>
        <div className="row">
          <Button>{t("uiGuide.primaryAction")}</Button>
          <Button variant="ghost">{t("uiGuide.secondaryAction")}</Button>
          <Button variant="danger">{t("uiGuide.destructiveAction")}</Button>
        </div>
      </article>

      <article className="grid">
        <h2>{t("uiGuide.fields")}</h2>
        <Field
          label={t("uiGuide.sampleInput")}
          htmlFor="ui-guide-input"
          hint={t("uiGuide.sampleHint")}
        >
          <input id="ui-guide-input" placeholder={t("uiGuide.samplePlaceholder")} />
        </Field>
      </article>

      <article className="grid">
        <h2>{t("uiGuide.feedbackStates")}</h2>
        <EmptyState title={t("uiGuide.noRecords")} detail={t("uiGuide.emptyDescription")} />
        <ErrorState message={t("uiGuide.errorDescription")} />
      </article>

      <article className="grid">
        <h2>{t("uiGuide.loadingStates")}</h2>
        <Skeleton large />
        <Skeleton />
      </article>

      <article className="grid">
        <h2>{t("uiGuide.kpiCards")}</h2>
        <div className="cards">
          <KpiCard label="Conversion" value="23%" />
          <KpiCard label="Pipeline Value" value="52,900" />
        </div>
      </article>

      <article className="grid">
        <h2>Component Catalog v1.1</h2>
        <p className="muted">
          Official UI reference for MindFlow operational surfaces. New patterns must extend this
          catalog or document an explicit exception in the PR.
        </p>
        <div className="table-container">
          <table className="table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Role</th>
                <th>Tokenized</th>
                <th>Adoption Rule</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td>Button</td>
                <td>Primary, secondary, destructive actions</td>
                <td>Yes</td>
                <td>Default action primitive for all screens</td>
              </tr>
              <tr>
                <td>Field</td>
                <td>Label-input grouping for AA accessibility</td>
                <td>Yes</td>
                <td>Required for labeled form controls in admin/ops flows</td>
              </tr>
              <tr>
                <td>EmptyState</td>
                <td>No-data pattern for operational modules</td>
                <td>Yes</td>
                <td>Use instead of ad-hoc no-data copy blocks</td>
              </tr>
              <tr>
                <td>ErrorState</td>
                <td>Unified error surface and ARIA alert</td>
                <td>Yes</td>
                <td>Use for recoverable route and panel errors</td>
              </tr>
              <tr>
                <td>ConfirmDialog</td>
                <td>Accessible destructive action confirmation</td>
                <td>Yes</td>
                <td>Mandatory replacement for native confirm dialogs</td>
              </tr>
            </tbody>
          </table>
        </div>
      </article>

      <article className="panel">
        <h2>UI DoD Enterprise</h2>
        <ul>
          <li>No hardcoded destructive confirms outside ConfirmDialog.</li>
          <li>Dynamic HTML goes through sanitization before render.</li>
          <li>Critical routes include loading and error boundaries.</li>
          <li>PR gate must pass smoke, a11y, contracts, and visual checks.</li>
          <li>Operational flows must be usable with explicit labels and keyboard focus.</li>
        </ul>
      </article>

      <article className="panel">
        <h2>UI Debt SLA</h2>
        <div className="table-container">
          <table className="table">
            <thead>
              <tr>
                <th>Severity</th>
                <th>Definition</th>
                <th>SLA</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td>Critical</td>
                <td>Operation blocked or accessibility/security path broken</td>
                <td>Before release or within 24h</td>
              </tr>
              <tr>
                <td>High</td>
                <td>Major workflow degradation with workaround only</td>
                <td>Within 5 business days</td>
              </tr>
              <tr>
                <td>Medium</td>
                <td>Non-blocking regression or visible inconsistency</td>
                <td>Within 2 sprints</td>
              </tr>
              <tr>
                <td>Low</td>
                <td>Localized polish debt</td>
                <td>Continuous backlog within 1 quarter</td>
              </tr>
            </tbody>
          </table>
        </div>
      </article>
    </section>
  );
}
