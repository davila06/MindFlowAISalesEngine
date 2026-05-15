"use client";

import { useReportWebVitals } from "next/web-vitals";
import { trackUxEvent } from "@/services/uxTelemetry";

export function WebVitalsTracker() {
  useReportWebVitals((metric) => {
    trackUxEvent({
      event: "web_vital",
      screen: "global",
      detail: metric.name,
      value: Number(metric.value.toFixed(2))
    });
  });

  return null;
}
