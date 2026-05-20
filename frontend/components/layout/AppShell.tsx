
"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import type { ReactNode } from "react";
import { LanguageSelector } from "@/components/layout/LanguageSelector";
import { WebVitalsTracker } from "@/components/layout/WebVitalsTracker";
import { TopBar } from "@/components/layout/TopBar";
import { useTenant } from "@/hooks/useTenant";
import { useI18n } from "@/i18n/I18nProvider";

interface NavItem {
  href: string;
  label: string;
}

interface NavGroup {
  key: string;
  label: string;
  items: NavItem[];
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

  const navGroups: NavGroup[] = [
    {
      key: "home",
      label: "",
      items: [
        { href: "/dashboard", label: t("nav.dashboard") },
      ],
    },
    {
      key: "crm",
      label: t("nav.group.crm"),
      items: [
        { href: "/leads", label: t("leads.title") },
        { href: "/pipeline", label: t("nav.pipeline") },
        { href: "/leads/activities", label: t("nav.leadActivities") },
      ],
    },
    {
      key: "automation",
      label: t("nav.group.automation"),
      items: [
        { href: "/sequences", label: t("nav.sequences") },
        { href: "/rules", label: t("nav.rules") },
      ],
    },
    {
      key: "comms",
      label: t("nav.group.comms"),
      items: [
        { href: "/whatsapp", label: t("nav.whatsapp") },
        { href: "/channels", label: "Bandeja Omnicanal" },
        { href: "/email/templates", label: t("nav.emailTemplates") },
        { href: "/email/smtp", label: t("nav.emailSmtp") },
        { href: "/email/logs", label: t("nav.emailLogs") },
      ],
    },
    {
      key: "admin",
      label: t("nav.group.admin"),
      items: [
        { href: "/admin/custom-fields", label: t("nav.customFields") },
        { href: "/admin", label: t("nav.admin") },
        { href: "/admin/ui-guide", label: t("nav.uiGuide") },
      ],
    },
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
          {navGroups.map((group) => (
            <div key={group.key} className="nav-group">
              {group.label && (
                <span className="nav-group-label">{group.label}</span>
              )}
              {group.items.map((item) => {
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
            </div>
          ))}
        </nav>
      </aside>
      <div className="content-area">
        <TopBar />
        <main id="main-content" className="main" tabIndex={-1}>
          {children}
        </main>
      </div>
    </div>
  );
}
