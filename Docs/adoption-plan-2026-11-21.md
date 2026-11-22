# Adoption Plan

> **Generated**: 2026-11-21
> **Project phase**: Production (fijada por `/atlassian-setup` con la evidencia de Jira, no por un `/gate-check`)
> **Engine**: Unity 6000.3.5f2
> **Template version**: v1.3.0
> **Workflow**: `standard` (de `modes.rigor`). Review mode: `lean`

Sigue estos pasos en orden y marca cada punto al terminarlo. Vuelve a ejecutar `/adopt`
cuando quieras para ver qué huecos quedan.

**Resultado de la auditoría:** 0 bloqueantes · 4 altos · 6 medios · 4 bajos. Las historias y el
sprint ya los leen sin errores los scripts del framework; los GDD no se pueden trabajar con
`/design-system` hasta que exista el índice de sistemas (2.1).

## Cómo se publica lo que hagas aquí

Los GDD, las historias y los ADR de este plan son espejos de Jira y Confluence (ver
`.claude/rules/atlassian-mirror.md`). Cada retoque queda en el archivo local; para que lo vea
el equipo:

- `/atlassian-sync status` antes de empezar, para ver si Confluence cambió algo mientras tanto.
- `/actualizar-docs confluence` cuando quieras publicar lo terminado: muestra el cambio y pregunta antes de escribir (nada se publica solo).
- En páginas que escribió el equipo solo se **añaden** secciones; no se reescribe la página
  entera sin un sí explícito después de ver el diff.
- En las historias, lo que añade `/story-readiness` va debajo de la marca de la descripción de
  Jira; lo que escribió el equipo no se toca.
- Las páginas nuevas (ADR, índice de sistemas…) se crean en la carpeta de Confluence que
  indique `confluence.new_pages_parent_id`. Sugerencia: la carpeta **Programación**
  (`10747905`), donde ya está "Sistema de Balance".

---

## Step 1: Fix Blocking Gaps

**Ninguno.**

- Migración desde v1.0: no aplica (existe `project.yaml` y los archivos heredados coinciden).
- ADR sin `## Status`: no hay ADR.
- Índice de sistemas con estados entre paréntesis: no hay índice.
- Motor: configurado.

---

## Step 2: Fix High-Priority Gaps

### 2.0 Decidir la convención de encabezados (antes de tocar ningún GDD)
**Problema:** el script de estructura y los skills buscan los encabezados en inglés de la
plantilla; las páginas de Confluence están en español, así que los siete GDD salen con 0 de 8
secciones aunque parte del contenido exista (por ejemplo, "Sistema de Balance" tiene una
sección de fórmulas, "FÓRMULAS Y CÁLCULOS").

**Probado con el script de estructura:**

| Encabezado | ¿Lo reconoce? |
|---|---|
| `## Acceptance Criteria` | sí |
| `## Acceptance Criteria (Criterios de aceptación)` | sí |
| `## Acceptance Criteria — Criterios de aceptación` | sí |
| `## 8. Acceptance Criteria` | sí |
| `## Formulas (Fórmulas)` | sí |
| `## **Acceptance Criteria**` (negrita, como salen los títulos de Confluence) | no |
| `## Criterios de aceptación`, `## Fórmulas`, `## FÓRMULAS Y CÁLCULOS` | no |

Ningún script ni hook del framework ancla el final de esos encabezados, así que añadir el
español entre paréntesis no rompe nada.

**Recomendación:** cada sección que añada el retrofit lleva el nombre en inglés de la
plantilla seguido del español entre paréntesis, con el texto en español. No se renombran los
encabezados que el equipo ya tiene.
**Alternativa (no recomendada):** enseñar alias en español al framework: hay que tocar el
script del framework (se pierde al actualizarlo) y revisar cada skill que lea esos encabezados
por nombre.
**Tiempo:** 5 min
- [x] Convención elegida: inglés con el español entre paréntesis (2026-11-21)

### 2.1 Crear el índice de sistemas
**Problema:** no existe `design/gdd/systems-index.md`. `/design-system` se detiene sin él
("No systems index found. Run `/map-systems` first") y en `standard` el framework lo exige
antes de cualquier GDD, así que no puede empezar ningún retrofit de 2.2. Lo leen además
`/gate-check`, `/create-stories` y `/architecture-review`.
**Arreglo:** `/map-systems`. Parte de `game-concept.md` y de los GDD que ya están en
`design/gdd/`. La página "Sistemas" trae 14 secciones: economía, ataque automático, progresión,
enemigos, aumento de nivel, sobrecargas, escalado de dificultad, límite de tiempo, estadísticas,
victoria/derrota, cámara, minimapa, tienda y combate. Los valores de la columna Status deben ser
exactamente `Not Started`, `In Progress`, `In Review`, `Designed`, `Approved` o
`Needs Revision`, sin paréntesis: rompen los chequeos de las puertas.
**Tiempo:** 1 sesión
- [x] `design/gdd/systems-index.md` creado (2026-11-21: 30 sistemas, 18 MVP, 9 Vertical Slice, 2 Alpha, 1 Full Vision)
- [x] Publicado con `/atlassian-sync push` (2026-11-21: página 132448258 en la carpeta «Mecánicas y Sistemas», versión 1)

### 2.2 Criterios de aceptación en los 7 GDD
**Problema:** ningún GDD tiene `Acceptance Criteria`; `/create-stories` no puede derivar
historias de ellos y `/design-review` los marcará como incompletos.
**Requisito:** 2.1 hecho.
**Arreglo:** `/design-system retrofit design/gdd/<archivo>.md`. Detecta qué secciones
faltan y rellena solo los huecos; la misma pasada cubre las demás secciones requeridas (ver
4.1). Los criterios deben ser verificables por QA y salir del juego tal como está, no solo
del texto de la página.

- [x] `overrides.md` — hecho el 2026-11-21 (Status Designed, 28 criterios de aceptación, 9 preguntas abiertas; falta `/design-review` en sesión nueva y publicarlo: el push a la página «Overrides» de Confluence falló el 2026-11-21 con `internal_error` y no se aplicó nada). Era el primero: el sistema ya tiene pruebas
  (`OverrideCombinationTests`), así que sus criterios pueden ser verificables. La página está
  desactualizada (ver 4.6): escribir los criterios desde `Assets/Configurations/Production/Overrides/`
  y desde `OverrideData` / `OverrideCombatResolver`, no desde su texto. Es un documento interno:
  las combinaciones secretas pueden describirse aquí, nunca en texto que vea el jugador.
- [ ] `core-mechanics.md` — 30 min (movimiento, destrucción, tienda, mejoras, recolección)
- [ ] `chests.md` — 30 min. Tiene marcas abiertas ("WIP", "TBD", "¿¿¿¿¿"): criterios solo para
  lo confirmado (aparición tras el enemigo fuerte, reparto 80 % normales / 20 % especiales) y
  el resto como preguntas abiertas.
- [ ] `progression.md` — 30 min
- [ ] `tutorial.md` — 30 min (criterios de tipo UI: captura por pantalla en `production/qa/evidence/`)
- [ ] `balance-system.md` — 30 min. Es una referencia técnica de los assets de balance, no el
  diseño de un sistema: sus criterios son "lo documentado coincide con los assets". Corregir
  antes las rutas (4.6).
- [ ] `systems-overview.md` — 1 sesión. La página reúne unos 14 sistemas; los criterios se
  escriben por sistema, no por página, y el índice de 2.1 decide cómo se reparten.

### 2.3 Crear los ADR críticos
**Problema:** no existe ningún ADR. `/architecture-review` y `/create-control-manifest` leen
ADR, y la puerta de Technical Setup espera al menos 3. En `standard` solo hacen falta los
críticos.
**Arreglo:** `/architecture-decision <título>` por cada decisión; ya están tomadas y descritas
en el README ("Patrones de diseño principales"), así que se documentan tal como son y se
pasan a `Accepted` con tu confirmación (`/architecture-decision accept <ADR-id>`).
Candidatos:
1. Bucle de actualización centralizado (`UpdateManager`)
2. Object pooling (`PoolManager` + `SpawnFactory`)
3. Configuración por ScriptableObject con copias `_Production` / `_Sandbox`
4. Bus de eventos (`GameEvents`)
5. UI en uGUI con el kit `UIStyle_Production`, sin UI Toolkit

**Tiempo:** 1 sesión para los 3 primeros
- [x] 3 ADR escritos (2026-11-21, en `Proposed`, revisados por el especialista de Unity): ADR-0001 Bucle de actualización (Foundation), ADR-0002 Configuración por ScriptableObject (Foundation) y ADR-0003 Object pooling (Core, depende de los otros dos); 10 entradas en `docs/registry/architecture.yaml`
- [ ] Aceptados con `/architecture-decision accept ADR-0001`, luego 0002 y 0003 (`## Status: Accepted`)
- [ ] Publicados en Confluence con `/actualizar-docs confluence` (decidir antes la carpeta)
- [ ] Pendientes: ADR del bus de eventos (`GameEvents`) y de la UI (uGUI + `UIStyle_Production`)

---

## Step 3: Bootstrap Infrastructure

### 3a. Registrar los requisitos existentes (rellena tr-registry.yaml)
`docs/architecture/tr-registry.yaml` existe pero está vacío (`requirements: []`). Ejecutar
`/architecture-review` **después de 2.2 y 2.3**: rellena el registro desde los GDD y los ADR y
genera `requirements-traceability.md`.
**Tiempo:** 1 sesión
- [ ] `tr-registry.yaml` con entradas
- [ ] `docs/architecture/requirements-traceability.md` creado

### 3b. Crear el control manifest (hueco ALTO)
Ejecutar `/create-control-manifest` (necesita ADR aceptados: después de 2.3).
**Tiempo:** 30 min
- [ ] `docs/architecture/control-manifest.md` creado

### 3c. Archivo de seguimiento del sprint
**Ya hecho.** `production/sprint-status.yaml` lo generó `/atlassian-setup` desde el Sprint 7 de
Jira. No hace falta `/sprint-plan update`: los sprints se planifican en Jira y
`/atlassian-sync pull` regenera el archivo.
- [x] `production/sprint-status.yaml` creado

### 3d. Confirmar la etapa del proyecto
Ejecutar `/gate-check production` **al final de este paso, no antes**. La etapa `Production`
salió de la evidencia (6 sprints cerrados, 216 archivos de código), no de pasar una puerta. La
tabla de puertas del framework pide para pasar a Production: épicas (hecho: 11), primer plan de
sprint (hecho: Sprint 7), especificaciones de UX (falta, ver 4.4) y control manifest (falta, 3b).
Mientras falten, espera CONCERNS o FAIL; el informe lista qué falta. Un FAIL no avanza la etapa.
**Tiempo:** 5 min
- [ ] `/gate-check production` ejecutado y `project.stage` confirmado

---

## Step 4: Medium-Priority Gaps

### 4.1 Resto de secciones requeridas en los 7 GDD
Faltan Overview, Detailed Rules, Edge Cases y Dependencies en los siete, y `## Summary`, que el
skill exige en todo GDD. `Formulas` además donde el documento define reglas numéricas:
`overrides` (umbrales de nivel), `chests` (reparto 80/20), `progression` (costes),
`systems-overview` (economía y dificultad) y `balance-system` (ya existe como "§11 FÓRMULAS Y
CÁLCULOS": solo falta el encabezado reconocible). `tutorial` y `core-mechanics` no definen reglas
numéricas. Player Fantasy y Tuning Knobs son opcionales en `standard`.
**Arreglo:** es la misma pasada de `/design-system retrofit` de 2.2, sin comando aparte. El
retrofit propone contenido para cada sección que falta: cuando el texto ya está en la página
con otro título, ubicarlo bajo la sección de la plantilla en vez de duplicarlo.
**Tiempo:** incluido en 2.2 (unos 30 min más por documento)
- [ ] Cada GDD tiene Summary, Overview, Detailed Rules, Edge Cases y Dependencies (y Formulas donde aplica)

### 4.2 Documento de arquitectura
**Problema:** no existe `docs/architecture/architecture.md`; la puerta de Technical Setup lo
espera.
**Arreglo:** `/create-architecture full`. Alternativa desde el código:
`/reverse-document architecture Assets/Scripts/Core`.
**Tiempo:** 1 sesión
- [ ] `docs/architecture/architecture.md` creado

### 4.3 Historias: TR-ID y criterios de aceptación
**Problema:** las 46 historias no tienen TR-ID ni lista de criterios de aceptación; sin
criterios, `/story-readiness` no las da por listas para implementar.
**Arreglo:** no en bloque. Antes de empezar cada historia de código:
`/story-readiness <ruta de la historia>`; añade Layer, Type y criterios debajo de la marca de la
descripción de Jira y `/atlassian-sync push` los publica. Los TR-ID aparecen cuando exista el
registro (3a). Para el Sprint 7: MAN-136, MAN-138, MAN-149, MAN-150, MAN-151 y MAN-152. De las
46 historias abiertas, unas 14 tocan código; las ~32 restantes son arte, animación, la página
de itch, música y documentación: se quedan como tareas de Jira, sin Layer ni Type.
**Tiempo:** 5-10 min por historia
- [ ] Historias del Sprint 7 listas

### 4.4 Documentos de diseño que esperan las puertas y no existen
No son un problema de formato sino de existencia: `design/gdd/game-pillars.md`,
`design/art/art-bible.md`, `design/ux/hud.md`, `design/ux/interaction-patterns.md` y
`design/accessibility-requirements.md`. `/gate-check production` dirá cuáles exige de verdad en
`standard`; decide cuáles hacer ahora. Mucho del contenido ya existe repartido:
- Pilares: la premisa y la emoción central están en la página "Guía de conceptualización"; no
  hay skill dedicado salvo `/brainstorm`, así que lo corto es escribirlos con la plantilla
  `.claude/docs/templates/game-pillars.md`.
- Art bible: `/art-bible` (la paleta y el estilo viven en `UIStyle_Production`).
- UX: `/ux-design hud`, `/ux-design patterns` y `/ux-design accessibility`. La página
  "Propuestas de UI" solo tiene un título y tres imágenes: no sirve de base.

**Tiempo:** 1 sesión por documento
- [ ] Decidido cuáles se hacen: ______________________

### 4.5 Presupuestos de rendimiento
**Problema:** `performance.draw_call_limit` y `performance.memory_ceiling_mb` están sin fijar
(60 FPS y 16,67 ms sí). El tope de memoria importa sobre todo para WebGL.
**Arreglo:** `/project-settings performance.draw_call_limit=<n>` y
`/project-settings performance.memory_ceiling_mb=<n>`, con valores sacados de una build medida
(`Docs/Performance/` tiene el inventario), no estimados.
**Tiempo:** 5 min
- [ ] Presupuestos fijados

### 4.6 Documentos de Confluence desactualizados respecto al juego
No es un hueco de formato, pero hay que resolverlo antes de escribir criterios sobre ellos:
- **"Overrides"**: pide nivel 5 "(estimado)" para los requisitos y asume un corte a 20 minutos;
  los assets de producción (`Override_*_Production`) piden nivel 2 y `GameBalanceConfig_Production`
  tiene `matchDurationMinutes: 5`. Además dice "evoluciones".
- **"Sistema de Balance"**: cita `Assets/Resources/GameBalanceConfig.asset` y
  `Assets/ScriptableObjects/{Upgrades,Enemies,Waves,Orbs}/`; esas rutas ya no existen. Hoy:
  `Assets/Configurations/Production/Resources/` (`GameBalanceConfig_Production`,
  `ShopUpgradeDatabase_Production`) y `Assets/Configurations/Production/{Upgrades,Enemies,Waves}/`.

Son páginas del equipo: se proponen los cambios y quien las escribió los confirma; se publican
con `/atlassian-sync push` después de ver el diff. Hacerlo junto con 2.2 para esos dos
documentos.
**Tiempo:** 30 min por página
- [ ] "Overrides": valores al día
- [ ] "Sistema de Balance": rutas al día

---

## Step 5: Optional Improvements

### 5.1 Manifest Version en las historias
Las 46 historias no tienen el campo. Los chequeos lo dan por bueno cuando falta; no hay nada
que hacer hasta que exista el control manifest (`/story-readiness` lo estampa al prepararlas).

### 5.2 Cabecera `**Status**:` en los 7 GDD
Ninguno la tiene. La añade el retrofit de 2.2 (`> **Status**: Draft`); no requiere un paso aparte.

### 5.3 Preferencias técnicas sin responder
`platform.gamepad_support`, `platform.touch_support` y la cobertura mínima de pruebas están
sin fijar: el archivo de acciones de entrada trae bindings de mando y táctil de la plantilla
de Unity, pero ningún documento dice si el juego los soporta. Pregúntalo al equipo y fíjalos con
`/project-settings platform.gamepad_support=<None|Partial|Full>` (y `touch_support`).
**Tiempo:** 5 min
- [ ] Gamepad y táctil decididos

### 5.4 "Sinergia" en lugar de override / sobrecarga
Las páginas "Sistemas" y "Progresión" (6 menciones) y las historias MAN-149 a MAN-152 siguen
diciendo "sinergia"; el proyecto lo llama override (código) y sobrecarga (interfaz). El código
solo conserva el nombre antiguo en `[FormerlySerializedAs]` y en claves de PlayerPrefs
`legacy…`, que parecen intencionales (compatibilidad con el progreso guardado): no tocarlos.
Cambiar los títulos de Jira y las páginas es decisión del equipo.

---

## What to Expect from Existing Stories

Existing stories continue to work with all template skills. New format checks
(TR-ID validation, manifest version staleness) auto-pass when the fields are
absent — so nothing breaks. They won't benefit from staleness tracking until
regenerated. Do not regenerate stories that are in progress or done.

En este proyecto las historias vienen de Jira: no se regeneran ni se reescriben, solo se
completan con `/story-readiness` justo antes de trabajar en ellas.

---

## Re-run

Run `/adopt` again after completing Step 3 to verify all blocking and high gaps
are resolved. The new run will reflect the current state of the project.
Antes, `/atlassian-sync status` para comprobar que los espejos están al día.
