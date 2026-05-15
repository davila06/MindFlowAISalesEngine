"use client";

import { useI18n } from "@/i18n/I18nProvider";

export function ErrorState({ message }: { message: string }) {
  const { t } = useI18n();

  return (
    <div className="error-state" role="alert" aria-live="assertive">
      <strong>{t("common.errorTitle")}</strong>
      <p>{message}</p>
    </div>
  );
}
