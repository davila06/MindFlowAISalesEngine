export type UxEventName =
  | "view_loaded"
  | "user_action"
  | "request_error"
  | "time_to_insight"
  | "web_vital";

interface UxTelemetryPayload {
  event: UxEventName;
  screen: string;
  detail?: string;
  value?: number;
  ts: string;
}

export function trackUxEvent(payload: Omit<UxTelemetryPayload, "ts">) {
  const eventPayload: UxTelemetryPayload = {
    ...payload,
    ts: new Date().toISOString()
  };

  console.info("[ux-telemetry]", eventPayload);

  if (typeof navigator !== "undefined" && navigator.sendBeacon) {
    try {
      const blob = new Blob([JSON.stringify(eventPayload)], {
        type: "application/json"
      });
      navigator.sendBeacon("/api/ux/telemetry", blob);
    } catch {
      // Keep UX telemetry non-blocking.
    }
  }
}
