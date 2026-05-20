
"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import type { ReactNode } from "react";
import { LanguageSelector } from "@/components/layout/LanguageSelector";
import { WebVitalsTracker } from "@/components/layout/WebVitalsTracker";
import { useTenant } from "@/hooks/useTenant";
import { useI18n } from "@/i18n/I18nProvider";

interface NavItem {
  href: string;
  label: string;
}

export function AppShell({ children }: { children: ReactNode }) {
  const tenant = useTenant();
  const pathname = usePathname();
  const currentPath = pathname ?? "";
  const { t } = useI18n();
  const [mobileNavOpen, setMobileNavOpen] = useState(false);

  useEffect(() => {
    setMobileNavOpen(false);
  }, [currentPath]);

  const navItems: NavItem[] = [
    { href: "/dashboard", label: t("nav.dashboard") },
    { href: "/leads", label: t("leads.title") },
    { href: "/pipeline", label: t("nav.pipeline") },
    { href: "/leads/activities", label: t("nav.leadActivities") },
    { href: "/sequences", label: t("nav.sequences") },
    { href: "/whatsapp", label: t("nav.whatsapp") },
    { href: "/channels", label: "Bandeja Omnicanal" },
    { href: "/rules", label: t("nav.rules") },
    { href: "/email/smtp", label: t("nav.emailSmtp") },
    { href: "/email/templates", label: t("nav.emailTemplates") },
    { href: "/email/logs", label: t("nav.emailLogs") },
    { href: "/admin", label: t("nav.admin") },
    { href: "/admin/custom-fields", label: t("nav.customFields") },
    { href: "/admin/ui-guide", label: t("nav.uiGuide") }
  ];

  return (
    <div className="shell">
      <a className="skip-link" href="#main-content">
        {t("app.skipToContent")}
      </a>
      <WebVitalsTracker />
      <aside className="sidebar">
        <div className="brand-wrap">
          <div className="brand">{t("app.title")}</div>
          <div className="tag">
            {t("app.tenant")}: <span className="pill">{tenant.tenantId}</span>
          </div>
        </div>
        <button
          type="button"
          className="nav-toggle"
          aria-controls="main-nav"
          onClick={() => setMobileNavOpen((current) => !current)}
        >
          {t("app.menu")}
        </button>
        <LanguageSelector />
        <nav
          id="main-nav"
          className={`nav ${mobileNavOpen ? "is-open" : ""}`}
          aria-label="Main navigation"
        >
          {navItems.map((item) => {
            const isActive =
              currentPath === item.href ||
              (item.href !== "/dashboard" && currentPath.startsWith(item.href));

            return (
              <Link
                key={item.href}
                href={item.href}
                prefetch={false}
                aria-current={isActive ? "page" : undefined}
              >
                {item.label}
              </Link>
            );
          })}
        </nav>
      </aside>
      <main id="main-content" className="main" tabIndex={-1}>
        {children}
      </main>
    </div>
  );
}
