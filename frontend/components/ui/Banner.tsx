import React from "react";

export function Banner({ type = "success", message }: { type?: "success" | "warning" | "error"; message: string }) {
  const icon = type === "success"
    ? "✔"
    : type === "warning"
    ? "⚠"
    : "✖";
  return (
    <div className={`banner banner-${type}`} role="note" aria-label={type}>
      <span aria-hidden="true" className="banner-icon">{icon}</span>
      <span>{message}</span>
    </div>
  );
}
