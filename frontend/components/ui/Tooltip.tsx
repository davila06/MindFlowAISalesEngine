import React, { useState } from "react";

export function Tooltip({ children, content }: { children: React.ReactNode; content: string }) {
  const [open, setOpen] = useState(false);
  return (
    <span
      className="tooltip-wrapper"
      tabIndex={0}
      aria-label={content}
      onFocus={() => setOpen(true)}
      onBlur={() => setOpen(false)}
      onMouseEnter={() => setOpen(true)}
      onMouseLeave={() => setOpen(false)}
      style={{ position: "relative", display: "inline-block" }}
    >
      {children}
      {open && (
        <span className="tooltip-bubble" role="tooltip">
          {content}
        </span>
      )}
    </span>
  );
}
