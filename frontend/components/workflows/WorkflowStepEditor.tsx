import React from 'react';

export interface WorkflowStepEditorProps {
  step: any;
  onChange: (step: any) => void;
}

export const WorkflowStepEditor: React.FC<WorkflowStepEditorProps> = ({ step, onChange }) => {
  // Edición básica de tipo y etiqueta
  const handleChange = (field: string, value: any) => {
    onChange({ ...step, [field]: value });
  };

  return (
    <div className="bg-surface border border-line rounded-lg shadow-md p-6 mb-4">
      <div className="flex gap-4 mb-2">
        <div className="flex-1">
          <label className="block text-xs text-muted mb-1">Tipo</label>
          <input
            className="w-full border border-line rounded px-2 py-1"
            value={step.type}
            onChange={e => handleChange('type', e.target.value)}
            title="Tipo de paso"
            placeholder="Ej: action, condition"
          />
        </div>
        <div className="flex-1">
          <label className="block text-xs text-muted mb-1">Etiqueta</label>
          <input
            className="w-full border border-line rounded px-2 py-1"
            value={step.label}
            onChange={e => handleChange('label', e.target.value)}
            title="Etiqueta del paso"
            placeholder="Descripción breve"
          />
        </div>
      </div>
      <div className="mb-2">
        <label className="block text-xs text-muted mb-1">Parámetros</label>
        <pre className="bg-surface-strong border border-line rounded p-2 text-xs overflow-x-auto">
          {JSON.stringify(step.parameters, null, 2)}
        </pre>
      </div>
      {step.children && step.children.length > 0 && (
        <div className="mt-4 pl-4 border-l-2 border-accent">
          <div className="text-xs text-accent mb-2">Sub-pasos:</div>
          {step.children.map((child: any, idx: number) => (
            <WorkflowStepEditor key={idx} step={child} onChange={updated => {
              const newChildren = [...step.children];
              newChildren[idx] = updated;
              onChange({ ...step, children: newChildren });
            }} />
          ))}
        </div>
      )}
    </div>
  );
};
