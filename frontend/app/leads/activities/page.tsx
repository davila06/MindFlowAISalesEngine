"use client";

import { useState } from "react";
import { ActivityFeed } from "@/components/leads/ActivityFeed";
import { Field } from "@/components/ui/Field";
import { PageHeader } from "@/components/ui/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";

export default function LeadActivitiesPage() {
  const { t } = useI18n();
  const [leadId, setLeadId] = useState("");

  return (
    <section className="grid">
      <PageHeader title={t("leadActivities.title")} subtitle={t("leadActivities.subtitle")} />

      <div className="panel">
        <Field label={t("leadActivities.leadId")} htmlFor="lead-activities-id">
          <input
            id="lead-activities-id"
            value={leadId}
            onChange={(event) => setLeadId(event.target.value)}
            placeholder={t("leadActivities.leadIdPlaceholder")}
            aria-label={t("leadActivities.leadId")}
          />
        </Field>
      </div>

      <ActivityFeed leadId={leadId.trim()} />
    </section>
  );
}
