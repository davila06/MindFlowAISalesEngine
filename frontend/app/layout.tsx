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

export default function RootLayout({
  children
}: Readonly<{ children: ReactNode }>) {
  return (
    <html lang="en">
      <body>
        <QueryProvider>
          <I18nProvider>
            <AppShell>{children}</AppShell>
          </I18nProvider>
        </QueryProvider>
      </body>
    </html>
  );
}
