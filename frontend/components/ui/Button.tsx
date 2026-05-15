"use client";

import { forwardRef, type ButtonHTMLAttributes, type ReactNode } from "react";

type Variant = "primary" | "ghost" | "danger";

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant;
  children: ReactNode;
}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(function Button(
  { variant = "primary", children, ...props },
  ref
) {
  const className = ["button", `button-${variant}`, props.className ?? ""]
    .join(" ")
    .trim();

  return (
    <button {...props} ref={ref} className={className}>
      {children}
    </button>
  );
});
