import React from "react";
import { render, screen, fireEvent, cleanup } from "@testing-library/react";
import { I18nProvider } from "@/i18n/I18nProvider";
import { KanbanBoard } from "@/components/pipeline/KanbanBoard";

const mockStages = [
  { id: "stage-1", name: "Prospectos", order: 1 },
  { id: "stage-2", name: "Calificados", order: 2 }
];
const mockOpportunities = [
  { id: "opp-1", leadId: "lead-1", stageId: "stage-1", title: "Oportunidad 1", value: 1000 }
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

describe("KanbanBoard saved views", () => {
  it("guarda y restaura la vista seleccionada", () => {
    render(
      <I18nProvider>
        <KanbanBoard />
      </I18nProvider>
    );
    // Cambiar filtro de vista
    const viewSelect = screen.getByLabelText(/Saved view/i);
    fireEvent.change(viewSelect, { target: { value: "stage-2" } });
    cleanup();
    // Simular recarga de componente (persistencia)
    render(
      <I18nProvider>
        <KanbanBoard />
      </I18nProvider>
    );
    // El filtro debe seguir en "stage-2"
    expect(screen.getByLabelText(/Saved view/i)).toHaveValue("stage-2");
  });
});
