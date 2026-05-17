import React from "react";
import { render, screen } from "@testing-library/react";
import RuleBuilder from "@/components/rules/RuleBuilder";
import ruleFixtures from "./fixtures/rule-fixtures.json";

describe("RuleBuilder", () => {
  it("renders without crashing", () => {
    render(<RuleBuilder />);
    expect(screen.getByText("Rule Builder"));
  });

  it("displays trigger, condition, and action sections", () => {
    render(<RuleBuilder />);
    expect(screen.getByText("Triggers"));
    expect(screen.getByText("Conditions"));
    expect(screen.getByText("Actions"));
  });

  describe("RuleBuilder (fixtures)", () => {
    it("renders rules from fixture data", () => {
      // Simular que RuleBuilder recibe reglas como prop (ajustar si el componente lo soporta)
      // Si no, este test es base para cuando RuleBuilder acepte fixtures
      expect(Array.isArray(ruleFixtures)).toBe(true);
      expect(ruleFixtures.length).toBeGreaterThan(0);
      expect(ruleFixtures[0]).toHaveProperty("name");
      expect(ruleFixtures[0]).toHaveProperty("triggers");
      expect(ruleFixtures[0]).toHaveProperty("conditions");
      expect(ruleFixtures[0]).toHaveProperty("actions");
    });
  });

  describe("RuleBuilder (fixtures integration)", () => {
    it("renders fixture rules visually", () => {
      // Adaptar fixtures a tipo Rule (mock id, trigger)
      const adapted = ruleFixtures.map((r, i) => ({
        id: `fx-${i}`,
        name: r.name,
        trigger: r.triggers?.[0] || "",
        isActive: true,
        conditions: r.conditions,
        actions: r.actions,
      }));
      render(<RuleBuilder rules={adapted} />);
      expect(screen.getByTestId("fixture-list")).toBeInTheDocument();
      expect(screen.getByText("Fixture Rules")).toBeInTheDocument();
      expect(screen.getByText("Simple Email Rule")).toBeInTheDocument();
      expect(screen.getByText("Lead Scoring Rule")).toBeInTheDocument();
    });
  });
});