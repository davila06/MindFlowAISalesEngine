# Ejemplo de Storybook para MindFlow

Una vez inicializado Storybook (ver `storybook-node-upgrade.md`), puedes documentar y visualizar componentes UI de MindFlow.

## Ejemplo: Button

Crea el archivo `frontend/components/ui/Button.stories.tsx`:

```tsx
import type { Meta, StoryObj } from '@storybook/react';
import { Button } from './Button';

const meta: Meta<typeof Button> = {
  title: 'UI/Button',
  component: Button,
  tags: ['autodocs'],
};
export default meta;

type Story = StoryObj<typeof Button>;

export const Primary: Story = {
  args: {
    children: 'Primary Button',
    variant: 'primary',
  },
};

export const Ghost: Story = {
  args: {
    children: 'Ghost Button',
    variant: 'ghost',
  },
};
```

## Guía rápida
- Corre `npm run storybook` para abrir el catálogo interactivo.
- Usa stories para documentar props, variantes y casos de uso.
- Integra stories en PRs para revisión visual y de accesibilidad.

> **Recomendación:** Documenta todos los componentes base y de dominio crítico (pipeline, rules, email, layout, etc).
