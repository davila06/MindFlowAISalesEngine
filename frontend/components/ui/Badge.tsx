import React from "react";

export function Badge({ children, color = "default", ...props }: { children: React.ReactNode; color?: "default" | "success" | "warning" | "error" } & React.HTMLAttributes<HTMLSpanElement>) {
  return (
    <span className={`badge badge-${color}`} {...props}>
      {children}
    </span>
  );
}
