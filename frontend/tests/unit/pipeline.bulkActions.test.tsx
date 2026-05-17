import React from "react";
import { render, screen, fireEvent } from "@testing-library/react";
import { KanbanBoard } from "@/components/pipeline/KanbanBoard";
import { I18nProvider } from "@/i18n/I18nProvider";

// Mock de datos de oportunidades y etapas
const mockStages = [
  { id: "stage-1", name: "Prospectos", order: 1 },
  { id: "stage-2", name: "Calificados", order: 2 },
  { id: "stage-3", name: "Cerrados", order: 3 }
];
const mockOpportunities = [
  { id: "opp-1", leadId: "lead-1", stageId: "stage-1", title: "Oportunidad 1", value: 1000 },
  { id: "opp-2", leadId: "lead-2", stageId: "stage-1", title: "Oportunidad 2", value: 2000 },
  { id: "opp-3", leadId: "lead-3", stageId: "stage-2", title: "Oportunidad 3", value: 3000 }
];

jest.mock("@/hooks/queries/usePipelineQueries", () => ({
  usePipelineBoardQuery: () => ({
    data: { stages: mockStages, opportunities: mockOpportunities },
    isLoading: false,
    isFetching: false,
    error: null,
    refetch: jest.fn()
  }),
  useCreateOpportunityMutation: () => ({ mutateAsync: jest.fn() }),
  useMoveOpportunityMutation: () => ({ mutateAsync: jest.fn(), isPending: false })
}));

describe("KanbanBoard bulk actions", () => {
  it("permite seleccionar múltiples oportunidades y activar el botón de bulk move", () => {
    render(
      <I18nProvider>
        <KanbanBoard />
      </I18nProvider>
    );
    // Seleccionar dos oportunidades (solo checkboxes)
    const checks = screen.getAllByLabelText(/Oportunidad/).filter((el) => el.tagName === "INPUT");
    fireEvent.click(checks[0]);
    fireEvent.click(checks[1]);
    // Seleccionar etapa destino para bulk move
    const stageSelect = screen.getByLabelText(/Bulk move/i);
    fireEvent.change(stageSelect, { target: { value: "stage-2" } });
    // El botón de bulk move debe estar habilitado y mostrar el conteo
    const bulkBtn = screen.getByRole("button", { name: /Bulk move/i });
    expect(bulkBtn).toBeEnabled();
    expect(bulkBtn.textContent).toMatch(/2/);
  });
});
