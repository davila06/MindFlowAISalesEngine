import React from "react";
import type { Rule } from "@/types/rule";

interface RuleBuilderProps {
  rules?: Rule[];
}

const RuleBuilder: React.FC<RuleBuilderProps> = ({ rules = [] }) => {
  return (
    <div>
      <h1>Rule Builder</h1>
      <div>
        <h2>Triggers</h2>
        <h2>Conditions</h2>
        <h2>Actions</h2>
      </div>
      {rules.length > 0 && (
        <div data-testid="fixture-list">
          <h3>Fixture Rules</h3>
          <ul>
            {rules.map((rule) => (
              <li key={rule.id || rule.name}>
                <strong>{rule.name}</strong> — Trigger: {rule.trigger}<br />
                <span>Conditions: {rule.conditions?.length ?? 0}</span> | <span>Actions: {rule.actions?.length ?? 0}</span>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
};

export default RuleBuilder;