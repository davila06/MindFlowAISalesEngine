"use client";

import { useMemo, useRef, useState } from "react";
import { Button } from "@/components/ui/Button";
import { ErrorState } from "@/components/ui/ErrorState";
import { Field } from "@/components/ui/Field";
import { useRulesQuery } from "@/hooks/queries/useRulesQueries";
import { useI18n } from "@/i18n/I18nProvider";
import { rulesService, type RuleFixtureResponse, type RuleTemplate } from "@/services/rules.service";
import { trackUxEvent } from "@/services/uxTelemetry";
import type { RuleAction, RuleCondition } from "@/types/rule";

interface BuilderCondition extends RuleCondition {
  rowId: string;
}

interface BuilderAction extends RuleAction {
  rowId: string;
}

export function RuleBuilderPanel() {
  const { t } = useI18n();
  const { data: rules = [], refetch } = useRulesQuery();
  const nextRowIdRef = useRef(0);

  const [templates, setTemplates] = useState<RuleTemplate[]>([]);
  const [selectedTemplateKey, setSelectedTemplateKey] = useState("");
  const [ruleName, setRuleName] = useState("");
  const [trigger, setTrigger] = useState("lead.created");
  const [conditions, setConditions] = useState<BuilderCondition[]>([
    createConditionRow({ field: "source", operator: "eq", value: "website" })
  ]);
  const [actions, setActions] = useState<BuilderAction[]>([
    createActionRow({ type: "add_score", value: "5" })
  ]);
  const [selectedRuleId, setSelectedRuleId] = useState("");
  const [rollbackVersion, setRollbackVersion] = useState("1");
  const [fixtureResult, setFixtureResult] = useState<RuleFixtureResponse | null>(null);
  const [statusMessage, setStatusMessage] = useState("");
  const [error, setError] = useState("");

  const selectedTemplate = useMemo(
    () => templates.find((item) => item.key === selectedTemplateKey),
    [selectedTemplateKey, templates]
  );

  function nextRowId(prefix: string) {
    nextRowIdRef.current += 1;
    return `${prefix}-${nextRowIdRef.current}`;
  }

  function createConditionRow(condition?: Partial<RuleCondition>): BuilderCondition {
    return {
      rowId: nextRowId("condition"),
      field: condition?.field ?? "source",
      operator: condition?.operator ?? "eq",
      value: condition?.value ?? "website"
    };
  }

  function createActionRow(action?: Partial<RuleAction>): BuilderAction {
    return {
      rowId: nextRowId("action"),
      type: action?.type ?? "add_score",
      value: action?.value ?? "5"
    };
  }

  function mapConditionsToRows(items: RuleCondition[]) {
    return items.length > 0
      ? items.map((item) => createConditionRow(item))
      : [createConditionRow({ field: "source", operator: "eq", value: "website" })];
  }

  function mapActionsToRows(items: RuleAction[]) {
    return items.length > 0
      ? items.map((item) => createActionRow(item))
      : [createActionRow({ type: "add_score", value: "5" })];
  }

  function serializeConditions(items: BuilderCondition[]): RuleCondition[] {
    return items.map(({ rowId: _rowId, ...condition }) => condition);
  }

  function serializeActions(items: BuilderAction[]): RuleAction[] {
    return items.map(({ rowId: _rowId, ...action }) => action);
  }

  async function loadTemplates() {
    setError("");
    const data = await rulesService.getTemplates();
    setTemplates(data);
    trackUxEvent({ event: "view_loaded", screen: "rules_builder", detail: "templates_loaded" });
  }

  function applyTemplate() {
    if (!selectedTemplate) {
      return;
    }

    setRuleName(selectedTemplate.template.name);
    setTrigger(selectedTemplate.template.trigger);
    setConditions(mapConditionsToRows(selectedTemplate.template.conditions));
    setActions(mapActionsToRows(selectedTemplate.template.actions));
  }

  function loadRule() {
    const selectedRule = rules.find((item) => item.id === selectedRuleId);
    if (!selectedRule) {
      return;
    }

    setRuleName(selectedRule.name);
    setTrigger(selectedRule.trigger);
    setConditions(mapConditionsToRows(selectedRule.conditions));
    setActions(mapActionsToRows(selectedRule.actions));
    setStatusMessage("");
    setError("");
  }

  function updateCondition(rowId: string, nextCondition: BuilderCondition) {
    setConditions((current) => current.map((item) => (item.rowId === rowId ? nextCondition : item)));
  }

  function updateAction(rowId: string, nextAction: BuilderAction) {
    setActions((current) => current.map((item) => (item.rowId === rowId ? nextAction : item)));
  }

  function addCondition() {
    setConditions((current) => [...current, createConditionRow({ field: "priority", operator: "eq", value: "High" })]);
  }

  function removeCondition(rowId: string) {
    setConditions((current) => (current.length > 1 ? current.filter((item) => item.rowId !== rowId) : current));
  }

  function addAction() {
    setActions((current) => [...current, createActionRow({ type: "set_priority", value: "High" })]);
  }

  function removeAction(rowId: string) {
    setActions((current) => (current.length > 1 ? current.filter((item) => item.rowId !== rowId) : current));
  }

  async function createRule() {
    setError("");
    setStatusMessage("");

    try {
      const created = await rulesService.create({
        name: ruleName.trim(),
        trigger,
        isActive: true,
        priority: 100,
        conflictPolicy: "first_wins",
        cooldownMinutes: 0,
        allowDestructiveActions: false,
        conditions: serializeConditions(conditions),
        actions: serializeActions(actions)
      });

      setSelectedRuleId(created.id);
      setStatusMessage(t("rules.builderCreated"));
      await refetch();
    } catch (err) {
      setError(err instanceof Error ? err.message : t("common.error"));
    }
  }

  async function saveRuleChanges() {
    if (!selectedRuleId) {
      return;
    }

    setError("");
    setStatusMessage("");

    try {
      await rulesService.update(selectedRuleId, {
        name: ruleName.trim(),
        trigger,
        isActive: true,
        priority: 100,
        conflictPolicy: "first_wins",
        cooldownMinutes: 0,
        allowDestructiveActions: false,
        conditions: serializeConditions(conditions),
        actions: serializeActions(actions)
      });

      setStatusMessage(t("rules.builderUpdated"));
      await refetch();
    } catch (err) {
      setError(err instanceof Error ? err.message : t("common.error"));
    }
  }

  async function simulateFixture() {
    if (!selectedRuleId) {
      return;
    }

    setError("");

    try {
      const simulation = await rulesService.testFixture({
        ruleId: selectedRuleId,
        trigger,
        lead: {
          source: "website",
          priority: "High",
          score: 75,
          hasEmail: true,
          hasPhone: true,
          fromStage: "new",
          toStage: "qualified"
        }
      });

      setFixtureResult(simulation);
      trackUxEvent({ event: "user_action", screen: "rules_builder", detail: "fixture_simulated" });
    } catch (err) {
      setError(err instanceof Error ? err.message : t("common.error"));
    }
  }

  async function rollbackRule() {
    if (!selectedRuleId) {
      return;
    }

    setError("");

    try {
      await rulesService.rollback(selectedRuleId, Number(rollbackVersion));
      setStatusMessage(t("rules.builderRollbackDone"));
      await refetch();
      trackUxEvent({ event: "user_action", screen: "rules_builder", detail: "rollback_applied" });
    } catch (err) {
      setError(err instanceof Error ? err.message : t("common.error"));
    }
  }

  return (
    <section className="panel grid" data-testid="rules-builder-panel">
      <h2>{t("rules.builderTitle")}</h2>
      <p className="muted">{t("rules.builderSubtitle")}</p>

      <div className="row">
        <Button variant="ghost" onClick={() => void loadTemplates()}>
          {t("rules.builderLoadTemplates")}
        </Button>

        <Field label={t("rules.builderTemplate")} htmlFor="rules-template-select">
          <select
            id="rules-template-select"
            aria-label={t("rules.builderTemplate")}
            title={t("rules.builderTemplate")}
            value={selectedTemplateKey}
            onChange={(event) => setSelectedTemplateKey(event.target.value)}
          >
            <option value="">Select template</option>
            {templates.map((item) => (
              <option key={item.key} value={item.key}>
                {item.name}
              </option>
            ))}
          </select>
        </Field>

        <Button variant="ghost" onClick={applyTemplate} disabled={!selectedTemplateKey}>
          {t("rules.builderApplyTemplate")}
        </Button>
      </div>

      <div className="grid rule-builder-grid">
        <Field label={t("rules.builderName")} htmlFor="rules-builder-name">
          <input
            id="rules-builder-name"
            aria-label={t("rules.builderName")}
            title={t("rules.builderName")}
            placeholder={t("rules.builderName")}
            value={ruleName}
            onChange={(event) => setRuleName(event.target.value)}
          />
        </Field>
        <Field label={t("rules.builderTrigger")} htmlFor="rules-builder-trigger">
          <input
            id="rules-builder-trigger"
            aria-label={t("rules.builderTrigger")}
            title={t("rules.builderTrigger")}
            placeholder={t("rules.builderTrigger")}
            value={trigger}
            onChange={(event) => setTrigger(event.target.value)}
          />
        </Field>
        {conditions.map((condition, index) => (
          <div className="grid" key={condition.rowId}>
            <div className="field">
              <label>{`${t("rules.builderConditionField")} ${index + 1}`}</label>
              <input
                aria-label={`${t("rules.builderConditionField")} ${index + 1}`}
                title={`${t("rules.builderConditionField")} ${index + 1}`}
                placeholder={t("rules.builderConditionField")}
                value={condition.field}
                onChange={(event) =>
                  updateCondition(condition.rowId, { ...condition, field: event.target.value })
                }
              />
            </div>
            <div className="field">
              <label>{`${t("rules.builderConditionOperator")} ${index + 1}`}</label>
              <select
                aria-label={`${t("rules.builderConditionOperator")} ${index + 1}`}
                title={`${t("rules.builderConditionOperator")} ${index + 1}`}
                value={condition.operator}
                onChange={(event) =>
                  updateCondition(condition.rowId, {
                    ...condition,
                    operator: event.target.value as RuleCondition["operator"]
                  })
                }
              >
                <option value="eq">eq</option>
                <option value="contains">contains</option>
                <option value="gt">gt</option>
                <option value="lt">lt</option>
              </select>
            </div>
            <div className="field">
              <label>{`${t("rules.builderConditionValue")} ${index + 1}`}</label>
              <input
                aria-label={`${t("rules.builderConditionValue")} ${index + 1}`}
                title={`${t("rules.builderConditionValue")} ${index + 1}`}
                placeholder={t("rules.builderConditionValue")}
                value={condition.value}
                onChange={(event) =>
                  updateCondition(condition.rowId, { ...condition, value: event.target.value })
                }
              />
            </div>
            <Button variant="ghost" onClick={() => removeCondition(condition.rowId)} disabled={conditions.length === 1}>
              {t("rules.builderRemoveCondition")}
            </Button>
          </div>
        ))}
        <Button variant="ghost" onClick={addCondition}>
          {t("rules.builderAddCondition")}
        </Button>

        {actions.map((action, index) => (
          <div className="grid" key={action.rowId}>
            <div className="field">
              <label>{`${t("rules.builderActionType")} ${index + 1}`}</label>
              <input
                aria-label={`${t("rules.builderActionType")} ${index + 1}`}
                title={`${t("rules.builderActionType")} ${index + 1}`}
                placeholder={t("rules.builderActionType")}
                value={action.type}
                onChange={(event) => updateAction(action.rowId, { ...action, type: event.target.value })}
              />
            </div>
            <div className="field">
              <label>{`${t("rules.builderActionValue")} ${index + 1}`}</label>
              <input
                aria-label={`${t("rules.builderActionValue")} ${index + 1}`}
                title={`${t("rules.builderActionValue")} ${index + 1}`}
                placeholder={t("rules.builderActionValue")}
                value={action.value}
                onChange={(event) => updateAction(action.rowId, { ...action, value: event.target.value })}
              />
            </div>
            <Button variant="ghost" onClick={() => removeAction(action.rowId)} disabled={actions.length === 1}>
              {t("rules.builderRemoveAction")}
            </Button>
          </div>
        ))}
        <Button variant="ghost" onClick={addAction}>
          {t("rules.builderAddAction")}
        </Button>
      </div>

      <div className="row">
        <Field label={t("rules.builderSelectRule")} htmlFor="rules-builder-rule-id">
          <select
            id="rules-builder-rule-id"
            aria-label={t("rules.builderSelectRule")}
            title={t("rules.builderSelectRule")}
            value={selectedRuleId}
            onChange={(event) => setSelectedRuleId(event.target.value)}
          >
            <option value="">Select rule</option>
            {rules.map((rule) => (
              <option key={rule.id} value={rule.id}>
                {rule.name}
              </option>
            ))}
          </select>
        </Field>
        <Button variant="ghost" onClick={loadRule} disabled={!selectedRuleId}>
          {t("rules.builderLoadRule")}
        </Button>
        <Button onClick={() => void createRule()}>{t("rules.builderCreate")}</Button>
        <Button variant="ghost" onClick={() => void saveRuleChanges()} disabled={!selectedRuleId}>
          {t("rules.builderSaveChanges")}
        </Button>
        <Button variant="ghost" onClick={() => void simulateFixture()} disabled={!selectedRuleId}>
          {t("rules.builderSimulate")}
        </Button>
      </div>

      <div className="row">
        <Field label={t("rules.builderRollbackVersion")} htmlFor="rules-builder-rollback-version">
          <input
            id="rules-builder-rollback-version"
            aria-label={t("rules.builderRollbackVersion")}
            title={t("rules.builderRollbackVersion")}
            type="number"
            min="1"
            value={rollbackVersion}
            onChange={(event) => setRollbackVersion(event.target.value)}
          />
        </Field>
        <Button variant="ghost" onClick={() => void rollbackRule()} disabled={!selectedRuleId}>
          {t("rules.builderRollback")}
        </Button>
      </div>

      {statusMessage ? <p className="success-text">{statusMessage}</p> : null}
      {error ? <ErrorState message={error} /> : null}

      {fixtureResult ? (
        <div className="panel">
          <p>
            {t("rules.builderFixtureMatched")}: <strong>{String(fixtureResult.matched)}</strong>
          </p>
          <p>
            {t("rules.builderFixtureApplied")}: <strong>{String(fixtureResult.applied)}</strong>
          </p>
          <p className="muted">{fixtureResult.actionsApplied.join(", ") || "-"}</p>
        </div>
      ) : null}
    </section>
  );
}
