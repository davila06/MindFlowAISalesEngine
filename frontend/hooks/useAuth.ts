"use client";

import { useMemo } from "react";
import type { User } from "@/types/user";

export function useAuth() {
  const user = useMemo<User>(
    () => ({
      id: "local-user",
      fullName: "Local Admin",
      email: "admin@mindflow.local",
      tenantId: process.env.NEXT_PUBLIC_TENANT_ID ?? "default",
      roles: ["Admin"],
      plan: "pro"
    }),
    []
  );

  return {
    user,
    isAuthenticated: true
  };
}