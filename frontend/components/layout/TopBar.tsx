"use client";

import { useRef, useState, useEffect } from "react";
import { useAuth } from "@/hooks/useAuth";
import { useTheme } from "@/hooks/useTheme";

export function TopBar() {
  const { user } = useAuth();
  const { dark, toggle } = useTheme();
  const [open, setOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handleOutside(e: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    }
    document.addEventListener("mousedown", handleOutside);
    return () => document.removeEventListener("mousedown", handleOutside);
  }, []);

  useEffect(() => {
    function handleKey(e: KeyboardEvent) {
      if (e.key === "Escape") setOpen(false);
    }
    document.addEventListener("keydown", handleKey);
    return () => document.removeEventListener("keydown", handleKey);
  }, []);

  const initials = (user.fullName ?? "U")
    .split(" ")
    .map((w) => w[0])
    .join("")
    .slice(0, 2)
    .toUpperCase();

  return (
    <header className="topbar" role="banner">
      <div className="topbar-spacer" />

      {/* ── Botón modo oscuro/claro siempre visible ── */}
      <button
        type="button"
        className={`topbar-theme-btn${dark ? " is-dark" : ""}`}
        onClick={toggle}
        title={dark ? "Cambiar a modo claro" : "Cambiar a modo oscuro"}
        aria-label={dark ? "Activar modo claro" : "Activar modo oscuro"}
      >
        {dark ? "☀️" : "🌙"}
      </button>

      {/* ── Menú de perfil ── */}
      <div className="topbar-right" ref={menuRef}>
        <button
          type="button"
          className="profile-btn"
          onClick={() => setOpen((o) => !o)}
          aria-haspopup="true"
          aria-expanded={open ? "true" : "false"}
          aria-label="Menú de perfil"
        >
          <span className="avatar" aria-hidden="true">
            {initials}
          </span>
          <span className="profile-name">{user.fullName}</span>
          <span className="chevron" aria-hidden="true">
            {open ? "▲" : "▼"}
          </span>
        </button>

        {open && (
          <div className="profile-dropdown" aria-label="Opciones de perfil">
            {/* ── User info ── */}
            <div className="pd-user">
              <div className="pd-avatar-lg" aria-hidden="true">
                {initials}
              </div>
              <div className="pd-user-info">
                <p className="pd-name">{user.fullName}</p>
                <p className="pd-email">{user.email}</p>
                <span className="pd-plan">{(user.plan ?? "free").toUpperCase()}</span>
              </div>
            </div>

            <div className="pd-divider" />

            {/* ── Dark mode toggle inside dropdown too ── */}
            <div className="pd-row">
              <span className="pd-label">
                {dark ? "🌙 Modo Oscuro activo" : "☀️ Modo Claro activo"}
              </span>
              <button
                type="button"
                className={`theme-toggle${dark ? " is-dark" : ""}`}
                onClick={toggle}
                aria-label={dark ? "Desactivar modo oscuro" : "Activar modo oscuro"}
                aria-pressed={dark ? "true" : "false"}
              >
                <span className="toggle-knob" />
              </button>
            </div>

            <div className="pd-divider" />

            {/* ── Meta ── */}
            <div className="pd-meta">
              <span className="pd-meta-label">Tenant</span>
              <span className="pd-meta-value">{user.tenantId}</span>
            </div>
            <div className="pd-meta">
              <span className="pd-meta-label">Rol</span>
              <span className="pd-meta-value">{user.roles?.[0] ?? "—"}</span>
            </div>
          </div>
        )}
      </div>
    </header>
  );
}
