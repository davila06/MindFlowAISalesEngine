"use client";

import { useAuth } from "@/hooks/useAuth";

export function usePermissions() {
  const { user } = useAuth();
  const roles = user.roles.map((x) => x.toLowerCase());

  return {
    canManageRules: roles.includes("admin"),
    canConfigureSmtp: roles.includes("admin"),
    canViewLogs: roles.includes("admin") || roles.includes("sales")
  };
}