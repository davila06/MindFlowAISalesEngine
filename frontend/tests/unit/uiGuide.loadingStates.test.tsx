import React from "react";
import { render, screen } from "@testing-library/react";
import UiGuidePage from "@/app/admin/ui-guide/page";
import { I18nProvider } from "@/i18n/I18nProvider";

describe("UiGuidePage loading states", () => {
  it("shows loading guidance text, announced loading region, and spinner", () => {
    render(
      <I18nProvider initialLocale="es">
        <UiGuidePage />
      </I18nProvider>
    );

    expect(
      screen.getByText(
        "Los bloques placeholder representan contenido pendiente mientras se consulta la data."
      )
    ).toBeInTheDocument();

    const statusRegion = screen.getByLabelText("Cargando contenido");
    expect(statusRegion).toBeInTheDocument();

    // Spinner is an SVG with role="status" inside the region
    const spinner = statusRegion.querySelector('svg[role="status"]');
    expect(spinner).toBeInTheDocument();
    expect(spinner).toHaveClass("animate-spin");
  });
});
