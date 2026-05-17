# Storybook — Node.js Upgrade Requerido

Para ejecutar Storybook 10+ en este proyecto, es necesario actualizar Node.js a la versión 20.19+ o 22.12+.

## Pasos sugeridos

1. Descarga Node.js LTS 20.x o superior desde https://nodejs.org/en/download
2. Verifica la versión con:
   ```sh
   node -v
   ```
3. Elimina `node_modules` y `package-lock.json` para evitar conflictos:
   ```sh
   rm -rf node_modules package-lock.json
   ```
4. Reinstala dependencias:
   ```sh
   npm install
   ```
5. Inicializa Storybook:
   ```sh
   npx storybook init
   ```
6. Ejecuta Storybook:
   ```sh
   npm run storybook
   ```

> **Nota:** El resto de la UI y tests funcionan con Node 18+, pero Storybook requiere Node 20+ por dependencias internas.
