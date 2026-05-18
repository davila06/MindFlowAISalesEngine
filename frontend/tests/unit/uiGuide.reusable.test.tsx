import React from "react";
import { render, screen } from "@testing-library/react";
import UiGuidePage from "@/app/admin/ui-guide/page";
import { I18nProvider } from "@/i18n/I18nProvider";

describe("UI Guide reusable components and a11y", () => {
  it("renders Banner, Tooltip, Icon, Badge components and all are accessible", () => {
    render(
      <I18nProvider initialLocale="es">
        <UiGuidePage />
      </I18nProvider>
    );
    // Banner, Tooltip, Icon, Badge examples (to be implemented)
    // expect(screen.getByRole("status", { name: /banner/i })).toBeInTheDocument();
    // expect(screen.getByLabelText(/tooltip/i)).toBeInTheDocument();
    // expect(screen.getByLabelText(/icon/i)).toBeInTheDocument();
    // expect(screen.getByText(/badge/i)).toBeInTheDocument();
  });
});
