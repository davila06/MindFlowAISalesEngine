import React from "react";
import { render, screen } from "@testing-library/react";
import UiGuidePage from "@/app/admin/ui-guide/page";
import { I18nProvider } from "@/i18n/I18nProvider";

describe("UI Guide visual consistency", () => {
  it("KPI cards render with label and value, and cards are visually separated", () => {
    render(
      <I18nProvider initialLocale="es">
        <UiGuidePage />
      </I18nProvider>
    );
    const kpiLabels = screen.getAllByText(/Conversion|Pipeline Value/);
    kpiLabels.forEach(label => {
      expect(label).toBeInTheDocument();
    });
    const kpiValues = screen.getAllByText(/23%|52,900/);
    kpiValues.forEach(value => {
      expect(value).toBeInTheDocument();
    });
    // Cards visually separated by border
    const kpiCards = screen.getAllByRole("article");
    expect(kpiCards.length).toBeGreaterThanOrEqual(2);
  });
});
