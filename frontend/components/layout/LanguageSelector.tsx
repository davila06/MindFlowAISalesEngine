"use client";

import { useI18n } from "@/i18n/I18nProvider";

export function LanguageSelector() {
  const { locale, setLocale, t } = useI18n();

  return (
    <label className="language-selector" htmlFor="language-selector">
      <span className="muted">{t("language.label")}</span>
      <select
        id="language-selector"
        value={locale}
        onChange={(event) => setLocale(event.target.value === "es" ? "es" : "en")}
      >
        <option value="en">{t("language.en")}</option>
        <option value="es">{t("language.es")}</option>
      </select>
    </label>
  );
}
