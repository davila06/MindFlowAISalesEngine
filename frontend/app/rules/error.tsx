"use client";

import { ErrorState } from "@/components/ui/ErrorState";

export default function RulesError({ error, reset }: { error: Error; reset: () => void }) {
  return (
    <section className="panel grid">
      <ErrorState message={error.message} />
      <button className="button button-ghost" onClick={reset} type="button">
        Retry
      </button>
    </section>
  );
}
