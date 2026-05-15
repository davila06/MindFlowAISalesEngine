"use client";

import Link from "next/link";
import { useI18n } from "@/i18n/I18nProvider";

export default function HomePage() {
  const { t } = useI18n();

  return (
    <section className="panel grid">
      <h1>{t("home.title")}</h1>
      <p className="muted">{t("home.subtitle")}</p>
      <div className="row">
        <Link href="/dashboard">{t("home.goDashboard")}</Link>
      </div>
    </section>
  );
}