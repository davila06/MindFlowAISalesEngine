# Anexo — Reglas de Deduplicación y Scoring

> **Última actualización:** 2026-05-14
> **Propósito:** Formalizar criterios de deduplicación y cálculo de scoring para leads en MindFlow AI Sales Engine.

## 1. Deduplicación de Leads

### 1.1. Normalización
- **Email:**
  - Convertir a minúsculas.
  - Eliminar espacios y caracteres invisibles.
  - Remover alias de Gmail (`usuario+alias@gmail.com` → `usuario@gmail.com`).
- **Teléfono:**
  - Eliminar espacios, guiones y paréntesis.
  - Normalizar a formato internacional (`+CC-...`).
  - Remover ceros a la izquierda si aplica.

### 1.2. Criterios de Duplicado
- **Exacto:**
  - Email normalizado igual.
  - Teléfono normalizado igual.
- **Fuzzy:**
  - Email similar (Levenshtein ≤ 2, mismo dominio).
  - Teléfono similar (≥ 80% coincidencia, mismo país).

### 1.3. Política de Merge
- Si se detecta duplicado exacto, actualizar registro existente.
- Si es fuzzy, marcar como posible duplicado y requerir revisión manual o lógica de merge automática (por definir).

### 1.4. Ejemplos
- `juan.perez+promo@gmail.com` y `juan.perez@gmail.com` → duplicado exacto.
- `+52 55 1234-5678` y `+525512345678` → duplicado exacto.
- `j.perez@gmail.com` y `juan.perez@gmail.com` → posible duplicado (fuzzy).

## 2. Scoring de Leads

### 2.1. Inputs
- Fuente del lead (web, referido, campaña, etc.).
- Compleción de perfil (email, teléfono, empresa).
- Interacciones (apertura/click email, respuesta, avance en pipeline).
- Tiempo de respuesta.

### 2.2. Fórmula v1 (ejemplo)
- Score inicial: 50
- +20 si fuente es "referido"
- +10 si perfil completo
- +10 por cada interacción positiva
- -10 si no responde en 48h
- Score máximo: 100, mínimo: 0

### 2.3. Thresholds
- **Hot:** score ≥ 80
- **Warm:** 50 ≤ score < 80
- **Cold:** score < 50

### 2.4. Ejemplo
- Lead de campaña, perfil completo, abrió email, no respondió en 48h:
  - 50 (base) + 0 (fuente) + 10 (perfil) + 10 (abrió email) - 10 (no respuesta) = 60 → Warm

---

> **Nota:** Estos criterios deben revisarse y ajustarse con negocio antes de implementación final. Se recomienda agregar casos límite y evidencia de pruebas en este mismo anexo.