import React from 'react';
import { WorkflowStepEditor } from '../../components/workflows/WorkflowStepEditor';

export default function WorkflowEditorDemo() {
  // Demo de un workflow simple
  const [step, setStep] = React.useState({
    type: 'action',
    label: 'Enviar correo',
    parameters: { to: '', subject: '' },
    children: []
  });

  return (
    <div className="min-h-[60vh] flex flex-col items-center justify-center bg-gradient-to-br from-[#f8fafb] to-[#eef2f5] p-8 rounded-2xl shadow-md">
      <h2 className="text-3xl font-display mb-6 text-brand">Editor Visual de Workflow (Demo)</h2>
      <div className="w-full max-w-xl">
        <WorkflowStepEditor step={step} onChange={setStep} />
      </div>
      {/* Aquí se integrará el drag-and-drop y edición anidada */}
    </div>
  );
}
