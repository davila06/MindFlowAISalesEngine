
import type { Metadata } from "next";
import type { ReactNode } from "react";
import "./globals.css";
import { AppShell } from "@/components/layout/AppShell";
import { QueryProvider } from "@/components/providers/QueryProvider";
import { I18nProvider } from "@/i18n/I18nProvider";

export const metadata: Metadata = {
  title: "MindFlow UI",
  description: "MindFlow operational frontend"
};

function getPreferredLocale(): "en" | "es" {
  if (typeof navigator !== "undefined" && navigator.language) {
    return navigator.language.startsWith("es") ? "es" : "en";
  }
  if (typeof window !== "undefined" && window.localStorage) {
    const stored = window.localStorage.getItem("mindflow.locale");
    if (stored === "es" || stored === "en") return stored;
  }
  return "en";
}

export default function RootLayout({ children }: Readonly<{ children: ReactNode }>) {
  // SSR fallback: always en, but client will hydrate with preferred
  const initialLocale = typeof navigator !== "undefined" ? getPreferredLocale() : "en";
  return (
    <html lang={initialLocale}>
      <body>
        <QueryProvider>
          <I18nProvider initialLocale={initialLocale}>
            <AppShell>{children}</AppShell>
          </I18nProvider>
        </QueryProvider>
      </body>
    </html>
  );
}
