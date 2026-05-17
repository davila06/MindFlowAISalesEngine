import React, { useEffect, useRef } from "react";

interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  description?: string;
  children: React.ReactNode;
  ariaLabelledBy?: string;
  ariaDescribedBy?: string;
  showCloseButton?: boolean;
}

export const Modal: React.FC<ModalProps> = ({
  isOpen,
  onClose,
  title,
  description,
  children,
  ariaLabelledBy,
  ariaDescribedBy,
  showCloseButton = true,
}) => {
  const modalRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!isOpen) return;
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
      if (e.key === "Tab") {
        // Focus trap
        const focusableEls = modalRef.current?.querySelectorAll<HTMLElement>(
          'a[href], button:not([disabled]), textarea, input, select, [tabindex]:not([tabindex="-1"])'
        );
        if (!focusableEls || focusableEls.length === 0) return;
        const first = focusableEls[0];
        const last = focusableEls[focusableEls.length - 1];
        if (document.activeElement === last && !e.shiftKey) {
          e.preventDefault();
          first.focus();
        } else if (document.activeElement === first && e.shiftKey) {
          e.preventDefault();
          last.focus();
        }
      }
    };
    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [isOpen, onClose]);

  useEffect(() => {
    if (isOpen) {
      // Focus modal on open
      setTimeout(() => {
        modalRef.current?.focus();
      }, 0);
    }
  }, [isOpen]);

  if (!isOpen) return null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50"
      role="dialog"
      aria-modal="true"
      aria-labelledby={ariaLabelledBy || "modal-title"}
      aria-describedby={ariaDescribedBy || (description ? "modal-desc" : undefined)}
      tabIndex={-1}
      ref={modalRef}
    >
      <div className="bg-white dark:bg-neutral-900 rounded-lg shadow-xl max-w-lg w-full p-6 outline-none" tabIndex={0}>
        <div className="flex items-start justify-between">
          <h2 id={ariaLabelledBy || "modal-title"} className="text-lg font-semibold">
            {title}
          </h2>
          {showCloseButton && (
            <button
              onClick={onClose}
              aria-label="Cerrar modal"
              className="ml-4 text-gray-500 hover:text-gray-900 dark:hover:text-white focus:outline-none"
            >
              <span aria-hidden="true">&times;</span>
            </button>
          )}
        </div>
        {description && (
          <p id={ariaDescribedBy || "modal-desc"} className="mt-2 text-sm text-gray-600 dark:text-gray-300">
            {description}
          </p>
        )}
        <div className="mt-4">{children}</div>
      </div>
    </div>
  );
};
