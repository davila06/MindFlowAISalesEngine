"use client";

import { useEffect } from "react";

export interface ToastProps {
  message: string;
  type?: "success" | "error" | "info";
  onClose?: () => void;
  duration?: number;
}

export function Toast({ message, type = "info", onClose, duration = 3200 }: ToastProps) {
  useEffect(() => {
    if (!onClose) return;
    const timer = setTimeout(onClose, duration);
    return () => clearTimeout(timer);
  }, [onClose, duration]);

  return (
    <div
      className={`toast toast-${type}`}
      role={type === "error" ? "alert" : "status"}
      aria-live={type === "error" ? "assertive" : "polite"}
    >
      <span>{message}</span>
      {onClose && (
        <button className="toast-close" aria-label="Close" onClick={onClose}>
          ×
        </button>
      )}
    </div>
  );
}
