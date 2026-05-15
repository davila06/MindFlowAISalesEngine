"use client";

import Link from "next/link";
import { Button } from "@/components/ui/Button";
import { PageHeader } from "@/components/ui/PageHeader";
import { usePermissions } from "@/hooks/usePermissions";
import { useI18n } from "@/i18n/I18nProvider";

export default function AdminPage() {
  const permissions = usePermissions();
  const { t } = useI18n();

  return (
    <section className="panel grid">
      <PageHeader title={t("admin.title")} subtitle={t("admin.subtitle")} />

      <div className="cards">
        <article className="kpi">
          <p className="label">{t("admin.rulesManagement")}</p>
          <p className="value">
            {permissions.canManageRules ? t("common.enabled") : t("common.locked")}
          </p>
        </article>
        <article className="kpi">
          <p className="label">{t("admin.smtpConfiguration")}</p>
          <p className="value">
            {permissions.canConfigureSmtp ? t("common.enabled") : t("common.locked")}
          </p>
        </article>
        <article className="kpi">
          <p className="label">{t("admin.emailLogs")}</p>
          <p className="value">
            {permissions.canViewLogs ? t("common.enabled") : t("common.locked")}
          </p>
        </article>
      </div>

      <div className="row">
        <Link href="/admin/ui-guide">
          <Button variant="ghost">{t("admin.openUiGuide")}</Button>
        </Link>
      </div>
    </section>
  );
}
