import React from "react";

export function Icon({ name, label }: { name: "check" | "warning" | "error" | "info"; label?: string }) {
  const icons = {
    check: "✔",
    warning: "⚠",
    error: "✖",
    info: "ℹ"
  };
  return (
    <span aria-label={label || name} role="img" className={`icon icon-${name}`}>{icons[name]}</span>
  );
}
