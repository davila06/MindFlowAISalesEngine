
'use client';
import React, { useEffect, useState } from 'react';

interface WorkflowStep {
  type: string;
  label: string;
  parameters: Record<string, any>;
  children: WorkflowStep[];
}

interface WorkflowDefinition {
  id: string;
  name: string;
  description: string;
  steps: WorkflowStep[];
  createdAtUtc: string;
  updatedAtUtc?: string;
}

export default function WorkflowsPage() {
  const [workflows, setWorkflows] = useState<WorkflowDefinition[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetch('/api/workflows')
      .then(res => res.json())
      .then(setWorkflows)
      .finally(() => setLoading(false));
  }, []);

  return (
    <div style={{ padding: 32 }}>
      <h1>Workflows</h1>
      {loading ? (
        <p>Loading...</p>
      ) : (
        <ul>
          {workflows.map(wf => (
            <li key={wf.id}>
              <strong>{wf.name}</strong> <br />
              <span>{wf.description}</span>
            </li>
          ))}
        </ul>
      )}
      {/* Punto de extensión: aquí irá el editor visual drag-and-drop */}
    </div>
  );
}
