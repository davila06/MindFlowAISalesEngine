"use client";

import { useAuth } from "@/hooks/useAuth";

export function useTenant() {
  const { user } = useAuth();

  return {
    tenantId: user.tenantId ?? "default",
    tenantSlug: user.tenantId ?? "default",
    plan: user.plan ?? "free"
  };
}