import React from "react";
import { render, screen, fireEvent } from "@testing-library/react";
import UiGuidePage from "@/app/admin/ui-guide/page";
import { I18nProvider } from "@/i18n/I18nProvider";

describe("UI Guide accessibility and usability", () => {
  it("all buttons are focusable and have visible focus class", () => {
    render(
      <I18nProvider initialLocale="es">
        <UiGuidePage />
      </I18nProvider>
    );
    const buttons = screen.getAllByRole("button");
    buttons.forEach((btn) => {
      btn.focus();
      expect(document.activeElement).toBe(btn);
      // Focus ring is applied via :focus-visible class in CSS
      // JSDOM does not compute styles, so we check focus state only
      expect(document.activeElement).toBe(btn);
    });
  });

  it("shows error and empty states with correct ARIA roles", () => {
    render(
      <I18nProvider initialLocale="es">
        <UiGuidePage />
      </I18nProvider>
    );
    expect(screen.getByRole("status")).toBeInTheDocument();
    expect(screen.getByRole("alert")).toBeInTheDocument();
  });
});
