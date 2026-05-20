"use client";
import { createContext, useContext, useState, useCallback } from "react";
import { Toast, ToastProps } from "@/components/ui/Toast";

interface ToastContextType {
  showToast: (opts: Omit<ToastProps, "onClose">) => void;
}

const ToastContext = createContext<ToastContextType>({ showToast: () => {} });

export function useToast() {
  return useContext(ToastContext);
}

export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [toast, setToast] = useState<ToastProps & { key: number } | null>(null);
  const showToast = useCallback((opts: Omit<ToastProps, "onClose">) => {
    setToast({ ...opts, key: Date.now() });
  }, []);
  const handleClose = useCallback(() => setToast(null), []);
  return (
    <ToastContext.Provider value={{ showToast }}>
      {children}
      {toast && (
        <Toast
          key={toast.key}
          message={toast.message}
          type={toast.type}
          duration={toast.duration}
          onClose={handleClose}
        />
      )}
    </ToastContext.Provider>
  );
}
