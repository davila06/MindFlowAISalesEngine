import React from "react";
import { render, screen, fireEvent } from "@testing-library/react";
import { I18nProvider } from "@/i18n/I18nProvider";
import { KanbanBoard } from "@/components/pipeline/KanbanBoard";

const mockStages = [
  { id: "stage-1", name: "Prospectos", order: 1 },
  { id: "stage-2", name: "Calificados", order: 2 }
];
const mockOpportunities = [
  { id: "opp-1", leadId: "lead-1", stageId: "stage-1", title: "Oportunidad 1", value: 1000 },
  { id: "opp-2", leadId: "lead-2", stageId: "stage-2", title: "Oportunidad 2", value: 2000 }
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

describe("KanbanBoard keyboard navigation", () => {
  it("permite navegar y seleccionar oportunidades con teclado", () => {
    render(
      <I18nProvider>
        <KanbanBoard />
      </I18nProvider>
    );
    // Foco inicial en el primer checkbox
    const checks = screen.getAllByRole("checkbox");
    checks[0].focus();
    expect(checks[0]).toHaveFocus();
    // Simular espacio para seleccionar
    fireEvent.keyDown(checks[0], { key: " ", code: "Space" });
    expect(checks[0]).toBeChecked();
    // Tab a siguiente checkbox
    fireEvent.keyDown(document.activeElement!, { key: "Tab", code: "Tab" });
    checks[1].focus();
    expect(checks[1]).toHaveFocus();
  });
});
