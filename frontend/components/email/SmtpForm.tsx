"use client";

import { useEffect, useState } from "react";
import { Button } from "@/components/ui/Button";
import { ErrorState } from "@/components/ui/ErrorState";
import { Field } from "@/components/ui/Field";
import { PageHeader } from "@/components/ui/PageHeader";
import { SkeletonRows } from "@/components/ui/Skeleton";
import { useI18n } from "@/i18n/I18nProvider";
import { emailService } from "@/services/email.service";
import { trackUxEvent } from "@/services/uxTelemetry";
import type { SmtpSettings } from "@/types/email";

const defaultState: SmtpSettings = {
  providerType: "smtp",
  providerBaseUrl: "",
  apiKey: "",
  host: "",
  port: 587,
  username: "",
  password: "",
  fromEmail: "",
  fromName: "",
  enableSsl: true
};

export function SmtpForm() {
  const [state, setState] = useState<SmtpSettings>(defaultState);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const { t } = useI18n();

  useEffect(() => {
    setLoading(true);
    (async () => {
      try {
        const loaded = await emailService.getSmtp();
        setState({ ...loaded, password: "" });
        trackUxEvent({ event: "view_loaded", screen: "email_smtp" });
      } catch {
        // Not configured yet.
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  async function save() {
    setMessage("");
    setError("");
    try {
      await emailService.saveSmtp(state);
      setMessage(t("email.smtp.saved"));
      setState((current) => ({ ...current, password: "" }));
      trackUxEvent({ event: "user_action", screen: "email_smtp", detail: "save_smtp" });
    } catch (err) {
      const detail = err instanceof Error ? err.message : t("common.error");
      setError(detail);
      trackUxEvent({ event: "request_error", screen: "email_smtp", detail });
    }
  }

  return (
    <section className="panel grid">
      <PageHeader title={t("email.smtp.title")} subtitle={t("email.smtp.subtitle")} />

      {loading ? <SkeletonRows rows={6} /> : null}

      {!loading ? (
        <div className="grid" role="form" aria-label={t("email.smtp.formLabel")}>
        <Field label="Provider" htmlFor="smtp-provider-type">
          <select
            id="smtp-provider-type"
            aria-label="Provider"
            value={state.providerType ?? "smtp"}
            onChange={(event) =>
              setState({
                ...state,
                providerType: event.target.value as SmtpSettings["providerType"]
              })
            }
          >
            <option value="smtp">SMTP</option>
            <option value="webhook">Webhook</option>
          </select>
        </Field>
        {state.providerType === "webhook" ? (
          <>
            <Field label="Provider Base URL" htmlFor="smtp-provider-base-url">
              <input
                id="smtp-provider-base-url"
                aria-label="Provider Base URL"
                value={state.providerBaseUrl ?? ""}
                onChange={(event) => setState({ ...state, providerBaseUrl: event.target.value })}
                placeholder="https://mail.example.test/hooks/send"
              />
            </Field>
            <Field label="API Key" htmlFor="smtp-api-key" hint="Leave empty to keep current API key.">
              <input
                id="smtp-api-key"
                type="password"
                aria-label="API Key"
                value={state.apiKey ?? ""}
                onChange={(event) => setState({ ...state, apiKey: event.target.value })}
              />
            </Field>
          </>
        ) : null}
        <Field label={t("email.smtp.host")} htmlFor="smtp-host">
          <input
            id="smtp-host"
            aria-label={t("email.smtp.host")}
            value={state.host}
            onChange={(event) => setState({ ...state, host: event.target.value })}
          />
        </Field>
        <Field label={t("email.smtp.port")} htmlFor="smtp-port">
          <input
            id="smtp-port"
            type="number"
            aria-label={t("email.smtp.port")}
            value={state.port}
            onChange={(event) =>
              setState({ ...state, port: Number(event.target.value || 0) })
            }
          />
        </Field>
        <Field label={t("email.smtp.username")} htmlFor="smtp-username">
          <input
            id="smtp-username"
            aria-label={t("email.smtp.username")}
            value={state.username}
            onChange={(event) => setState({ ...state, username: event.target.value })}
          />
        </Field>
        <Field
          label={t("email.smtp.password")}
          htmlFor="smtp-password"
          hint={t("email.smtp.passwordHint")}
        >
          <input
            id="smtp-password"
            type="password"
            aria-label={t("email.smtp.password")}
            placeholder={t("email.smtp.passwordHint")}
            value={state.password ?? ""}
            onChange={(event) => setState({ ...state, password: event.target.value })}
          />
        </Field>
        <Field label={t("email.smtp.fromEmail")} htmlFor="smtp-from-email">
          <input
            id="smtp-from-email"
            aria-label={t("email.smtp.fromEmail")}
            value={state.fromEmail}
            onChange={(event) => setState({ ...state, fromEmail: event.target.value })}
          />
        </Field>
        <Field label={t("email.smtp.fromName")} htmlFor="smtp-from-name">
          <input
            id="smtp-from-name"
            aria-label={t("email.smtp.fromName")}
            value={state.fromName ?? ""}
            onChange={(event) => setState({ ...state, fromName: event.target.value })}
          />
        </Field>
        <label htmlFor="smtp-enable-ssl">
          <input
            id="smtp-enable-ssl"
            type="checkbox"
            checked={state.enableSsl}
            onChange={(event) => setState({ ...state, enableSsl: event.target.checked })}
          />
          {t("email.smtp.enableSsl")}
        </label>
        </div>
      ) : null}

      <div className="row">
        <Button onClick={() => void save()} disabled={loading}>
          {t("email.smtp.save")}
        </Button>
      </div>

      {message ? (
        <p className="success-text" role="status" aria-live="polite">
          {message}
        </p>
      ) : null}
      {error ? <ErrorState message={error} /> : null}
    </section>
  );
}
