# manners.exe · instrucciones para Claude

Juego bullet-heaven estilo Vampire Survivors en Unity 6000.3.5f2 (URP), con WebGL y escritorio como objetivo. La arquitectura, los patrones de diseño y las reglas de código del proyecto están en el README, que se carga aquí:

@README.md

## Cómo trabajar en este proyecto

Indicaciones permanentes del usuario; aplican a cada tarea:

- Antes de implementar, entender el contexto general del juego: patrones de diseño, optimización, cómo está estructurado y cómo funcionan los sistemas que toca el cambio. Nada de "hacer por hacer": integraciones limpias que encajen con la arquitectura existente, sin agregados sueltos.
- Claude aplica los cambios él mismo y el usuario solo evalúa el resultado en Unity: no se le pide correr menús de `Tools`, scripts ni pasos de Inspector. Con el editor abierto, Claude trabaja en una copia del proyecto (worktree con la `Library` copiada), ejecuta ahí en batch lo necesario (herramientas, scripts de un solo uso, pruebas, capturas) y copia al proyecto solo las escenas, prefabs y assets resultantes, ya verificados. Un paso manual se pide solo si de verdad no hay otra forma, y se explica por qué.
- No crear una herramienta de editor (menú en `Tools`) para cada cambio: lo puntual se aplica directo a la escena, el prefab o el asset. Una herramienta nueva solo si el usuario la pide o si es un proceso que él va a repetir.
- Todo lo que el usuario pueda querer ajustar (textos, colores, tamaños, posiciones, tiempos, valores por defecto, balance) queda en campos serializados que se editan en el Inspector del objeto que lo usa: escena, prefab o ScriptableObject. Nunca fijo en código ni asignado en runtime: para cambiar un texto basta con entrar al objeto en Unity, sin abrir código. Una herramienta no vuelve a escribir encima de lo que el usuario ya ajustó a mano.
- Al terminar se explica qué cambió y dónde se ajusta en el Inspector.
- No escribir comentarios en el código C#. Los `[Tooltip]` y `[Header]` en español sí se usan: el balance se ajusta desde el Inspector.
- La documentación del proyecto (`README.md`, `Docs/UI_AUTHORING.md`, `Docs/OVERRIDE_COMBINATIONS.md`, `Docs/Performance/`, el manual publicado como Artifact y las páginas de Confluence) solo se actualiza con `/actualizar-docs`, que lanza el usuario: Claude no puede lanzarlo. Nunca se actualiza en automático: ni al terminar otra tarea, ni desde otro comando, hook o agente, aunque parezca útil o el usuario diga «continúa». Si un cambio la deja desactualizada, se avisa en una línea al final y se sugiere `/actualizar-docs`. Los comandos del framework (`/design-system`, `/architecture-decision`, `/adopt`, `/map-systems`…) siguen escribiendo sus propios documentos (GDD, ADR, índice de sistemas, plan de adopción) cuando el usuario los lanza, y los devlogs se escriben cuando el usuario los pide.
- Antes de construir un sistema desde cero, revisar las ramas locales (`git branch -a`): suele haber trabajo previo relacionado.

## Escenas y pruebas

- Escenas de producción: `MainMenu` y `Assets/Scenes/Final Levels/LEVEL 1/LEVEL 1.unity` (Mapa 1). `CityTest` es la versión anterior del Mapa 1.
- El Mapa 2 (`MilitaryBase`) está en pausa desde el 2 de octubre de 2026: todo el trabajo actual es solo sobre el Mapa 1. No tocar su escena, sus enemigos (prefabs `L2*`) ni sus assets, y no incluirlo en herramientas, validaciones, capturas ni pruebas hasta que el usuario lo reactive. Si un asset compartido con el Mapa 1 cambia, no hace falta ajustar el Mapa 2.
- Los sistemas nuevos y los experimentos de balance se prueban en `Assets/Scenes/Sandbox.unity`, con sus propias copias de assets en `Assets/Configurations/Sandbox/` (sufijo `_Sandbox`) y los ajustes en el componente `SandboxTuning`. Producción usa `Assets/Configurations/Production/` (sufijo `_Production`). No se experimenta editando los niveles que ya funcionan. El sandbox es un nivel duplicado con objetos reales de escena, no algo generado en runtime.
- Los valores del sandbox pasan a producción solo con `Tools > Manners > Sandbox > 4` (comparar y sincronizar). Nunca borrar y pegar assets a mano: cambian los GUID y se rompen referencias. Si la herramienta no lleva un cambio, se arregla la herramienta.
- Pruebas EditMode: `OverrideCombinationTests` (Window > General > Test Runner).

## Sobrecargas (overrides)

- Antes se llamaban "sinergias". En código, carpetas, assets y menús son overrides (`OverrideManager`, `OverrideData`, `Assets/Scripts/Overrides/`, `Tools > Manners > Overrides`); en la UI del juego, en español, "sobrecargas". No volver a usar synergy/sinergia.
- Cada sobrecarga son dos assets: `OverrideData` (identidad y requisitos: dos mejoras a cierto nivel) y un config que hereda de `OverrideEffectConfig` (`CryoFieldConfig`, `LaserBeamConfig`, `EmpPulseConfig`) con todos sus números y su visual. `OverrideManager` no conoce los tipos concretos (`ApplyTo` polimórfico). Cualquier sistema ajustable nuevo sigue la misma idea: todos sus valores y su visual en un solo ScriptableObject.
- Las combinaciones con las mejoras especiales (Multi disparo, Balas explosivas, Cadena de impacto) y entre sobrecargas se resuelven en `OverrideCombatResolver`. Siempre están activas si el jugador tiene la mejora, nunca por probabilidad, y son secretas: ningún texto que vea el jugador las menciona, ni en las descripciones de las sobrecargas ni en las de las mejoras.
- Láser con Multi disparo: cada rayo elige su propio objetivo con la misma prioridad (enemigo más cercano, luego un edificio al azar, luego un punto al azar), separados al menos `minBeamSeparationAngle` grados.
- Panel grande de sobrecargas (menú principal y Game Over, filas en modo `Collection`): muestra lo descubierto entre partidas. Panel del HUD en partida (modo `Hud`): guía de la partida actual; los contadores de nivel empiezan en 0, las sobrecargas ya descubiertas muestran siempre sus iconos con el resultado atenuado hasta que se activan, y lo no descubierto conserva el "?".

## UI

- Reutilizar el lenguaje visual existente (kit `UIStyle_Production`: paleta del documento de arte, placas en paralelogramo, franjas, fuente Orbitron, cara de la I.A., "?", animaciones que ya existen) y cuidar el equilibrio del HUD; no agregar elementos al azar. Lo que el estilo escribe en escenas y prefabs se cambia en `UIStyleApplier` y se aplica con `Tools > Manners > UI`, no a mano en cada escena; esas herramientas las corre Claude, no el usuario.
- Los botones y cajas de texto tienen tamaño fijo: al cambiar un texto, comprobar que quepa (fuente, tamaño, auto-size).
- La UI se diseña en escenas/prefabs con referencias serializadas; no crear su jerarquía visual en runtime ni reemplazar referencias por búsquedas de nombres. Guía: `Docs/UI_AUTHORING.md`; accesos: `Tools > Manners > UI > Editar`.
- Inglés por defecto y español mediante `LocalizedText`/`LocalizedString` editables en Inspector. Comprobar ambos idiomas con las capturas y `UIAuthoringTests`. No cambiar `OverrideData.PersistentId`: identifica el progreso guardado, independientemente del idioma o del nombre del archivo.

## Assets y serialización

- Renombrar o mover scripts y assets siempre junto con su `.meta`. Al renombrar un campo serializado, añadir `[FormerlySerializedAs("nombreAnterior")]`.
- Evitar editar escenas y prefabs como texto con Unity abierto: Unity puede sobrescribir el cambio al guardar. Los cambios se hacen con Unity en la copia de trabajo y se copian los archivos resultantes. Antes de copiarlos, `git status` debe mostrar esos archivos sin cambios del usuario; al entregar, se avisa que Unity puede pedir recargar una escena que estuviera abierta.

## Jira y Confluence

- Fuente de verdad: Jira (proyecto `MAN`, tablero 34) para épicas, historias, sprints y bugs; Confluence (carpeta `manners.exe`) para la documentación. La conexión es el servidor MCP `atlassian` de `.mcp.json`: cada persona entra con su cuenta (OAuth, `/mcp`) y trabaja con sus propios permisos. Identificadores y mapeos: `production/atlassian.yaml`.
- Lo que leen los skills del framework en `design/`, `docs/architecture/`, `production/epics/`, `production/sprints/`, `production/milestones/`, `production/qa/bugs/` y `production/sprint-status.yaml` es un espejo: cada archivo sincronizado empieza con un bloque `<!-- atlassian-mirror ... -->` que dice de dónde viene. Se trae con `/atlassian-sync pull`, y lo escrito en local se publica solo cuando lo pide el usuario: las páginas de Confluence con `/actualizar-docs` (o con `/atlassian-sync push` si lo escribe él) y las historias de Jira con `/atlassian-sync push`; la regla `.claude/rules/atlassian-mirror.md` se carga al tocar esos archivos. La primera vez, `/atlassian-setup`.
- Escribir en Jira o Confluence es publicar para todo el equipo: mostrar el cambio y pedir confirmación antes de cada tanda de escrituras; nunca borrar ni reemplazar contenido escrito por personas sin una confirmación explícita.
- Ramas, commits y PR llevan la clave de la historia de Jira (`MAN-123`).

## Claude Code Game Studios

Este proyecto usa [Claude Code Game Studios](https://github.com/Donchitos/Claude-Code-Game-Studios) 1.3.0 (licencia MIT en `.claude/LICENSE`): agentes en `.claude/agents/`, skills en `.claude/skills/`, hooks en `.claude/settings.json`, reglas por ruta en `.claude/rules/` y la configuración en `project.yaml`. Las secciones anteriores de este archivo y el README prevalecen sobre las reglas genéricas del framework; lo que se adaptó a manners.exe está en `.claude/docs/manners-adaptations.md`.

> **Primera sesión con el framework:** `/atlassian-setup` trae Jira y Confluence y completa la configuración; después `/adopt` y `/next-step`.

## Technology Stack

- **Engine**: Unity 6000.3.5f2 (6.3 LTS), URP 17.3
- **Language**: C#
- **Version Control**: Git + GitHub, ramas de trabajo con PR a `develop`
- **Build System**: Unity Build Pipeline; las builds oficiales de Windows, Mac y Web salen de Unity Build Automation (workflow `Build Windows, Mac and Web`)
- **Asset Pipeline**: importación de Unity, ScriptableObjects en `Assets/Configurations/` y `Resources/` para los globales (sin Addressables)

## Project Structure

@.claude/docs/directory-structure.md

## Engine Version Reference

<!-- ENGINE-REFERENCE-IMPORT: the line under this comment is engine-specific,
     and there is none until an engine is set. /setup-engine, the grow's record
     step and /project-settings (engine.name) write
     @docs/engine-reference/<engine>/VERSION.md here for the chosen engine. Skills
     that need the pinned version read that file on demand either way. -->
@docs/engine-reference/unity/VERSION.md

## Technical Preferences

`project.yaml` at the repo root is the primary config store — engine, specialists,
naming, platform, performance, modes. Skills resolve it via `resolve_config`
(see `.claude/docs/config-resolution.md`).

`.claude/docs/technical-preferences.md` is the **legacy fallback**, read on demand
when a key is absent from `project.yaml`. It also holds this project's forbidden
patterns and allowed libraries, which `project.yaml` has no key for.

## Coordination Rules

@.claude/docs/coordination-rules.md

## Collaboration Protocol

**User-driven collaboration, not autonomous execution.**
Every task follows: **Question -> Options -> Decision -> Draft -> Approval**, at the
depth `modes.automation` sets. This project uses `guided`: major decisions are asked,
minor ones are stated and carried out (`.claude/docs/automation-modes.md`).

- Agents ask "May I write this to [filepath]?" before creating a new file; updates
  to existing files proceed after a short summary in `guided` mode
  (exempt: `active.md` appends, agent memory, a subagent's new file at the path
  its orchestrating skill named; see `.claude/rules/agent-memory.md`)
- Agents MUST show drafts or summaries before requesting approval
- Multi-file changes require explicit approval for the full changeset
- No commits without user instruction
- A skipped or dismissed question is not an answer: act on nothing, re-ask in
  plain text and wait
- Started outside this file's folder? Warn first: hooks, deny rules and the
  ask-first mode load only when Claude Code starts here

See `docs/COLLABORATIVE-DESIGN-PRINCIPLE.md` for full protocol and examples.

## Coding Standards

@.claude/docs/coding-standards.md

## Context Management

Read `.claude/docs/context-management.md` on demand — it is a reference, not
session context. Two of its conventions are load-bearing and cited by name
elsewhere in the repo, so they are restated here rather than lost:

- **`production/session-state/active.md` is the session checkpoint.** The file is
  the memory, not the conversation. Read it first after any compaction, crash, or
  `/clear`.
- **Helpers in `.claude/scripts/` emit observations, never verdicts.** A script
  that scores or judges will eventually contradict a mode or override it cannot
  see. (Cited by `artifact-check.sh` and `adr-dep-graph.sh`.)
