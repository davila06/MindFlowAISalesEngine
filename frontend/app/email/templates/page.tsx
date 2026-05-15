"use client";

import { useState } from "react";
import { Button } from "@/components/ui/Button";
import { ErrorState } from "@/components/ui/ErrorState";
import { Field } from "@/components/ui/Field";
import { PageHeader } from "@/components/ui/PageHeader";
import { TableContainer } from "@/components/ui/TableContainer";
import { useI18n } from "@/i18n/I18nProvider";
import { emailService } from "@/services/email.service";
import { sanitizeHtml } from "@/services/htmlSanitizer";
import type { EmailTemplatePreview, EmailTemplateVersion } from "@/types/email";

const templateKey = "lead.welcome";

const sampleVariables = [
  { key: "lead.name", value: "Ada Lovelace" },
  { key: "lead.email", value: "ada@example.com" },
  { key: "company.name", value: "Analytical Engines" },
  { key: "pipeline.stage", value: "Qualified" }
];

export default function EmailTemplatesPage() {
  const { t } = useI18n();
  const [subject, setSubject] = useState("Welcome {{lead.name}}");
  const [bodyHtml, setBodyHtml] = useState("<p>Hello {{lead.name}}</p><p>Stage: {{pipeline.stage}}</p>");
  const [requiredVariables, setRequiredVariables] = useState("lead.name, pipeline.stage");
  const [rollbackVersion, setRollbackVersion] = useState("1");
  const [preview, setPreview] = useState<EmailTemplatePreview | null>(null);
  const [currentVersion, setCurrentVersion] = useState<EmailTemplateVersion | null>(null);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");

  async function publishVersion() {
    setMessage("");
    setError("");
    try {
      const created = await emailService.createTemplateVersion(templateKey, {
        subject,
        bodyHtml,
        requiredVariables: requiredVariables
          .split(",")
          .map((item) => item.trim())
          .filter(Boolean)
      });
      setCurrentVersion(created);
      setMessage(`${t("email.templates.versionPrefix")} ${created.version} ${t("email.templates.publishedSuffix")}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : t("email.templates.publishError"));
    }
  }

  async function runPreview() {
    setError("");
    try {
      const rendered = await emailService.previewTemplate(templateKey, {
        variables: Object.fromEntries(sampleVariables.map((item) => [item.key, item.value]))
      });
      setPreview(rendered);
    } catch (err) {
      setError(err instanceof Error ? err.message : t("email.templates.previewError"));
    }
  }

  async function rollback() {
    setMessage("");
    setError("");
    try {
      const rolledBack = await emailService.rollbackTemplate(templateKey, Number(rollbackVersion));
      setCurrentVersion(rolledBack);
      setMessage(`${t("email.templates.rollbackPrefix")} ${rolledBack.version}.`);
    } catch (err) {
      setError(err instanceof Error ? err.message : t("email.templates.rollbackError"));
    }
  }

  return (
    <section className="panel grid">
      <PageHeader
        title={t("email.templates.title")}
        subtitle={t("email.templates.subtitle")}
      />

      <div className="grid template-layout">
        <div className="grid">
          <Field label={t("email.templates.subjectLabel")} htmlFor="email-template-subject">
            <input
              id="email-template-subject"
              aria-label={t("email.templates.subjectLabel")}
              value={subject}
              onChange={(event) => setSubject(event.target.value)}
            />
          </Field>
          <Field label={t("email.templates.bodyLabel")} htmlFor="email-template-body">
            <textarea
              id="email-template-body"
              aria-label={t("email.templates.bodyLabel")}
              rows={10}
              value={bodyHtml}
              onChange={(event) => setBodyHtml(event.target.value)}
            />
          </Field>
          <Field label={t("email.templates.requiredVariablesLabel")} htmlFor="email-template-required">
            <input
              id="email-template-required"
              aria-label={t("email.templates.requiredVariablesLabel")}
              value={requiredVariables}
              onChange={(event) => setRequiredVariables(event.target.value)}
              placeholder={t("email.templates.requiredVariablesPlaceholder")}
            />
          </Field>
          <div className="row">
            <Button onClick={() => void publishVersion()}>{t("email.templates.publishVersion")}</Button>
            <Button variant="ghost" onClick={() => void runPreview()}>{t("email.templates.preview")}</Button>
          </div>
        </div>

        <div className="grid">
          <div className="panel template-card">
            <p className="label">{t("email.templates.templateKey")}</p>
            <p className="value">{templateKey}</p>
            <p className="muted">{t("email.templates.templateSandboxHint")}</p>
          </div>

          <TableContainer>
            <table className="table">
              <thead>
                <tr>
                  <th>{t("email.templates.variableColumn")}</th>
                  <th>{t("email.templates.sampleColumn")}</th>
                </tr>
              </thead>
              <tbody>
                {sampleVariables.map((item) => (
                  <tr key={item.key}>
                    <td>{`{{${item.key}}}`}</td>
                    <td>{item.value}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </TableContainer>

          <div className="row">
            <input
              aria-label={t("email.templates.rollbackVersion")}
              type="number"
              min="1"
              value={rollbackVersion}
              onChange={(event) => setRollbackVersion(event.target.value)}
            />
            <Button variant="ghost" onClick={() => void rollback()}>{t("email.templates.rollback")}</Button>
          </div>
        </div>
      </div>

      {message ? <p className="success-text">{message}</p> : null}
      {error ? <ErrorState message={error} /> : null}

      {currentVersion ? (
        <div className="panel template-card">
          <p className="label">{t("email.templates.currentVersion")}</p>
          <p className="value">v{currentVersion.version}</p>
          <p className="muted">
            {currentVersion.requiredVariables.join(", ") || t("email.templates.noRequiredVariables")}
          </p>
        </div>
      ) : null}

      {preview ? (
        <div className="grid template-preview-grid">
          <div className="panel template-card">
            <h2>{t("email.templates.previewSubject")}</h2>
            <p>{preview.subject}</p>
          </div>
          <div className="panel template-card">
            <h2>{t("email.templates.previewBody")}</h2>
            <div dangerouslySetInnerHTML={{ __html: sanitizeHtml(preview.bodyHtml) }} />
          </div>
        </div>
      ) : null}
    </section>
  );
}
