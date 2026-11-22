<!-- atlassian-mirror
source: confluence
id: 132448258
kind: page
title: Systems Index: manners.exe
url: https://blackdoorstudios.atlassian.net/wiki/spaces/~701213bd1c0c92aa748079c0e5c6d387aa64b/pages/132448258
remote_version: v:1
pushed_at: 2026-11-22T01:41:35Z
body_sha1: d6d5d21acaf09206aae31f28f5e910d43cfdd38c
-->
# Systems Index: manners.exe

> **Status**: Draft
> **Created**: 2026-11-21
> **Last Updated**: 2026-11-21
> **Source Concept**: design/gdd/game-concept.md

---

## Overview

manners.exe es un bullet heaven (Survivors-like) en el que un robot gigante poseído por una
I.A. gubernamental destruye una ciudad mientras dispara solo a las hordas que lo rodean. El
bucle central es: moverse, derrotar enemigos, recoger experiencia y monedas, subir de nivel y
elegir mejoras; encima de él están la tienda, los cofres y las sobrecargas (overrides), que
dan variedad a cada partida. Pilares tal como los recogen las páginas de Confluence: la
violencia es consecuencia de la falta de amabilidad, inmunidad ante el caos y más destrucción
significa más poder.

**Cómo leer este índice.** El juego ya está en Production: todos los sistemas MVP y Vertical
Slice están implementados en el código, así que los GDD que salgan de aquí describen lo
construido (retrofit) y no un diseño por hacer. La columna Status habla del **documento de
diseño**, no del código: `In Progress` es una página de Confluence que ya existe y falta
llevarla al formato del framework; `Not Started` es un sistema sin página propia. Las
dependencias de "Depends On" son de diseño ("no puede funcionar sin"); las referencias
circulares que sí existen entre clases del código están en *Circular Dependencies*.

**Fuera del índice a propósito:** las herramientas de desarrollo (Sandbox, menús
`Tools > Manners`, captura de rendimiento), que no necesitan GDD, y el Mapa 2 (`MilitaryBase`),
que está en pausa.

### Los sistemas, en una línea cada uno

**Core**
1. **Bucle de actualización**: `UpdateManager` registra los objetos que implementan
   `IUpdateable`, `IFixedUpdateable` o `ILateUpdateable`; solo siete clases que no crecen con
   la horda conservan su `Update()` propio. Fuente: README. Documentado en ADR-0001.
2. **Pooling y fábrica de spawn**: `PoolManager` y `SpawnFactory`; enemigos, proyectiles y
   coleccionables salen de pools con precarga escalonada (los números de daño, del pool propio de
   `FloatingTextManager`). Fuente: README. Documentado en ADR-0003.
3. **Bus de eventos**: `GameEvents`, eventos estáticos entre sistemas. Fuente: README. Se
   documenta con un ADR.
4. **Configuración de balance**: ScriptableObjects de `Assets/Configurations/Production/` y sus
   copias `_Sandbox`: `GameBalanceConfig`, enemigos, oleadas, mejoras y tienda. Página
   "Sistema de Balance" (sus rutas están desactualizadas).
5. **Control del jugador**: movimiento constante en el plano horizontal. Fuente: "Mecánicas".
6. **Cámara**: isométrica, sigue al jugador, 16:9; temblor de cámara y edificios translúcidos.
   Fuente: "Sistemas".

**Gameplay**
7. **Ataque automático y proyectiles**: el jugador no apunta; dispara al enemigo más cercano.
   Fuente: "Sistemas".
8. **Daño, vida y muerte**: vida del jugador y de los enemigos y números flotantes; "Combate"
   (en "Sistemas") explica cómo daño, vida enemiga y ritmo de experiencia se equilibran.
9. **Enemigos**: tanques (normales) y camionetas (rápidos); persiguen en línea recta y hacen daño
   por contacto; se separan entre sí en la horda (`EnemySeparationManager`, `EnemyProximityGrid`).
10. **Oleadas, aparición y escalado de dificultad**: `EnemySpawnManager`, `WaveData`, puntos de
    aparición con aviso; escalan velocidad, vida y daño con una curva exponencial controlada.
11. **Reloj de partida y fin de partida**: `GameTimeManager`, límite de tiempo, victoria,
    derrota y tiempo extra. El final sigue sin decidir en la página (jefe o fin por tiempo,
    "20-25 min TBD") y la configuración de producción dura 5 min.
12. **Entorno del nivel**: edificios destruibles que sueltan monedas (`BuildingsScript`) y muros
    invisibles con aviso animado en el borde del mapa (`InvisibleWalls`, no está en Confluence).
13. **Efectos visuales y feedback**: explosiones, campo criogénico, rayo láser, pulso
    electromagnético, squash & stretch y estilo toon. Sale del código.

**Economy**
14. **Drops, recolección y divisas**: monedas (tienda), experiencia (niveles) y gemas (tienda de
    aspectos, todavía sin implementar); el imán fija el radio de recolección. Fuente:
    "Sistemas" y "Mecánicas".
15. **Tienda en partida**: edificio con orbe de luz (verde, luego roja) donde se compran mejoras
    premium con monedas; cambia de lugar tras cada compra y se ve en el minimapa.
16. **Cofres**: aparecen al derrotar al enemigo fuerte de cada oleada; 80 % normales (mejora más
    monedas) y 20 % especiales (tesoro). La página tiene marcas "WIP", "TBD" y "TBA".

**Progression**
17. **Experiencia y subida de nivel**: la experiencia es el "ancla" del balance; al subir de
    nivel aparecen 3 opciones de estadísticas base, y por ahora es la única forma de curarse.
18. **Estadísticas y mejoras**: base (movimiento, velocidad de ataque, daño, imán, rango),
    premium a niveles múltiplos de 5 comprables en la tienda (la página lista Multi Shot, Balas
    Explosivas y Knockback; el proyecto también habla de Cadena de impacto: conciliar en el GDD)
    y estadísticas iniciales por skin; la vida máxima es constante.
19. **Sobrecargas (overrides)**: se desbloquean al subir dos mejoras distintas a un nivel
    concreto; hoy son tres (área criogénica, rayo láser y pulso electromagnético) con nivel
    requerido 2 en producción; el descubrimiento se guarda entre partidas. Las combinaciones
    con las mejoras especiales son secretas para el jugador.
20. **Metaprogresión**: skins con estadísticas iniciales propias, tienda de aspectos con gemas y
    árbol de habilidades comprado con monedas. Solo existe como diseño en "Sistemas"; el panel
    de skins del código solo copia sprites.

**UI**
21. **HUD**: barra de vida con la cara de la I.A., barra de experiencia, cronómetro, monedas,
    números flotantes; el panel de sobrecargas lo aporta el sistema de Sobrecargas. No hay
    página de UX.
22. **Minimapa**: jugador, enemigos y tienda; se actualiza constantemente. Fuente: "Sistemas".
23. **Menús, opciones, pausa y pantalla de fin**: menú principal, opciones compartidas con la
    pausa (idioma, sobrecargas, gráficos) y fin de partida con ranking. "Propuestas de UI" solo
    tiene un título.

**Audio**
24. **Música y efectos de sonido**: canciones por nivel (página "Música") y sonidos de
    recolección y de sobrecargas (MAN-142 a MAN-152 en Jira).

**Meta**
25. **Tutorial**: tutorial por gameplay en una escena aparte (MAN-144, cerrada); la página
    describe zonas, interactuables y flujo de eventos.
26. **Localización EN/ES**: inglés por defecto y español (`GameLocalization`, `LocalizedText`,
    `LocalizedString`); el idioma se guarda.
27. **Gráficos y rendimiento**: presets Eco, Balanceado y Alto; 60 FPS en partida, 30 en
    menú/pausa y 15 sin foco; salida a 1920×1080; `PerformanceMonitor`. Sus opciones las
    muestran los menús (`GraphicsSettingsMenu`) pero las posee este sistema. Fuente:
    `Docs/Performance/`.

**Persistence**
28. **Ajustes y progreso guardado**: PlayerPrefs sin una clase central: volumen, gráficos,
    idioma, descubrimiento de sobrecargas, tutorial visto y caché del ranking.
29. **Ranking global**: `GlobalLeaderboardService` consulta una base de datos por URL; entrada
    de iniciales. MAN-31 pide "una lista real del top de jugadores".

**Narrative**
30. **Narrativa y personaje de la I.A.**: premisa "la violencia es consecuencia de la falta de
    amabilidad"; la I.A. gubernamental del Proyecto M.E.C.H.A. controla el robot. Páginas
    "Preguntas necesarias para la narrativa" e "Información del mundo"; en el juego, la cara de
    la I.A. en el HUD y el personaje Charlie (MAN-137).

---

## Systems Enumeration

| # | System Name | Category | Priority | Status | Design Doc | Depends On |
|---|-------------|----------|----------|--------|------------|------------|
| 1 | Bucle de actualización (inferred) | Core | MVP | Not Started | — | — |
| 2 | Pooling y fábrica de spawn (inferred) | Core | MVP | Not Started | — | Configuración de balance |
| 3 | Bus de eventos (inferred) | Core | MVP | Not Started | — | — |
| 4 | Configuración de balance | Core | MVP | In Progress | design/gdd/balance-system.md | — |
| 5 | Control del jugador | Core | MVP | Not Started | — | Bucle de actualización, Configuración de balance |
| 6 | Cámara | Core | MVP | Not Started | — | Bucle de actualización, Control del jugador |
| 7 | Ataque automático y proyectiles | Gameplay | MVP | Not Started | — | Pooling y fábrica de spawn, Daño vida y muerte, Enemigos, Estadísticas y mejoras |
| 8 | Daño, vida y muerte | Gameplay | MVP | Not Started | — | Bucle de actualización, Pooling y fábrica de spawn, Bus de eventos, Configuración de balance |
| 9 | Enemigos | Gameplay | MVP | Not Started | — | Bucle de actualización, Pooling y fábrica de spawn, Configuración de balance, Daño vida y muerte |
| 10 | Oleadas, aparición y escalado de dificultad | Gameplay | MVP | Not Started | — | Pooling y fábrica de spawn, Bus de eventos, Enemigos, Reloj de partida y fin de partida |
| 11 | Reloj de partida y fin de partida | Gameplay | MVP | Not Started | — | Bucle de actualización, Bus de eventos, Configuración de balance |
| 12 | Entorno del nivel | Gameplay | MVP | Not Started | — | Pooling y fábrica de spawn, Configuración de balance, Cámara, Daño vida y muerte |
| 13 | Efectos visuales y feedback (inferred) | Gameplay | Vertical Slice | Not Started | — | Bucle de actualización |
| 14 | Drops, recolección y divisas | Economy | MVP | Not Started | — | Pooling y fábrica de spawn, Configuración de balance, Daño vida y muerte, Estadísticas y mejoras |
| 15 | Tienda en partida | Economy | Vertical Slice | Not Started | — | Bus de eventos, Drops recolección y divisas, Estadísticas y mejoras |
| 16 | Cofres | Economy | Vertical Slice | In Progress | design/gdd/chests.md | Daño vida y muerte, Reloj de partida y fin de partida, Drops recolección y divisas, Estadísticas y mejoras |
| 17 | Experiencia y subida de nivel | Progression | MVP | Not Started | — | Bus de eventos, Daño vida y muerte, Drops recolección y divisas, Estadísticas y mejoras, Localización EN/ES |
| 18 | Estadísticas y mejoras | Progression | MVP | In Progress | design/gdd/progression.md | Configuración de balance, Control del jugador, Daño vida y muerte, Localización EN/ES |
| 19 | Sobrecargas (overrides) | Progression | Vertical Slice | Designed | design/gdd/overrides.md | Daño vida y muerte, Enemigos, Entorno del nivel, Estadísticas y mejoras |
| 20 | Metaprogresión: skins, tienda de gemas y árbol de habilidades | Progression | Full Vision | Not Started | — | Drops recolección y divisas, Estadísticas y mejoras, Menús opciones pausa y pantalla de fin, Ajustes y progreso guardado |
| 21 | HUD (inferred) | UI | MVP | Not Started | — | Bus de eventos, Daño vida y muerte, Reloj de partida y fin de partida, Drops recolección y divisas, Experiencia y subida de nivel, Estadísticas y mejoras, Localización EN/ES |
| 22 | Minimapa | UI | Vertical Slice | Not Started | — | Bus de eventos, Enemigos, Tienda en partida, Cofres |
| 23 | Menús, opciones, pausa y pantalla de fin (inferred) | UI | Vertical Slice | Not Started | — | Reloj de partida y fin de partida, Música y efectos de sonido, Localización EN/ES, Ajustes y progreso guardado |
| 24 | Música y efectos de sonido | Audio | Vertical Slice | In Progress | design/audio/sound-bible.md | Bus de eventos, Localización EN/ES, Ajustes y progreso guardado |
| 25 | Tutorial | Meta | Vertical Slice | In Progress | design/gdd/tutorial.md | Bus de eventos, Oleadas aparición y escalado de dificultad, Drops recolección y divisas, Música y efectos de sonido, Localización EN/ES, Ajustes y progreso guardado |
| 26 | Localización EN/ES (inferred) | Meta | MVP | Not Started | — | Ajustes y progreso guardado |
| 27 | Gráficos y rendimiento (inferred) | Meta | Vertical Slice | Not Started | — | Pooling y fábrica de spawn, Localización EN/ES, Ajustes y progreso guardado |
| 28 | Ajustes y progreso guardado (inferred) | Persistence | MVP | Not Started | — | — |
| 29 | Ranking global (inferred) | Persistence | Alpha | Not Started | — | Reloj de partida y fin de partida, Localización EN/ES, Ajustes y progreso guardado |
| 30 | Narrativa y personaje de la I.A. | Narrative | Alpha | In Progress | design/narrative/lore/world.md | HUD, Tutorial |

---

## Categories

| Category | Description | Typical Systems |
|----------|-------------|-----------------|
| **Core** | Foundation systems everything depends on | Player controller, input, physics, camera, scene management, state machine |
| **Gameplay** | The systems that make the game fun | Combat, AI, stealth, movement abilities, interaction |
| **Progression** | How the player grows over time | XP/leveling, skill trees, unlocks, achievements, reputation |
| **Economy** | Resource creation and consumption | Currency, loot, crafting, shops, item database, drop tables |
| **Persistence** | Save state and continuity | Save/load, settings, cloud sync, profile management |
| **UI** | Player-facing information displays | HUD, menus, inventory screen, dialogue UI, map, notifications |
| **Audio** | Sound and music systems | Music manager, SFX bus, ambient audio, adaptive music, voice |
| **Narrative** | Story and dialogue delivery | Dialogue system, quest tracking, cutscenes, journal, lore entries |
| **Meta** | Systems outside the core game loop | Analytics, tutorials/onboarding, accessibility options, photo mode |

---

## Priority Tiers

| Tier | Definition | Target Milestone | Design Urgency |
|------|------------|------------------|----------------|
| **MVP** | Required for the core loop to function. Without these, you can't test "is this fun?" | First playable prototype | Design FIRST |
| **Vertical Slice** | Required for one complete, polished area. Demonstrates the full experience. | Vertical slice / demo | Design SECOND |
| **Alpha** | All features present in rough form. Complete mechanical scope, placeholder content OK. | Alpha milestone | Design THIRD |
| **Full Vision** | Polish, edge cases, nice-to-haves, and content-complete features. | Beta / Release | Design as needed |

En manners.exe el MVP es el bucle central (moverse, disparar solo, derrotar hordas, experiencia,
subir de nivel) más lo que ese bucle necesita debajo; Localización y Ajustes y progreso guardado
entran porque las cartas de mejora y el HUD ya los usan. Vertical Slice es lo que muestra la
demo además del bucle: tienda, cofres, sobrecargas, minimapa, menús, audio, tutorial, efectos y
presupuesto de rendimiento.

---

## Dependency Map

### Foundation Layer (no dependencies)

1. Bucle de actualización — lo usan 19 sistemas
2. Bus de eventos — los demás sistemas se comunican por él
3. Configuración de balance — todos los valores ajustables viven en sus ScriptableObjects
4. Ajustes y progreso guardado — volumen, gráficos, idioma y descubrimientos

### Core Layer (depends on foundation)

1. Pooling y fábrica de spawn — depends on: Configuración de balance
2. Control del jugador — depends on: Bucle de actualización, Configuración de balance
3. Cámara — depends on: Bucle de actualización, Control del jugador
4. Reloj de partida y fin de partida — depends on: Bucle de actualización, Bus de eventos, Configuración de balance
5. Localización EN/ES — depends on: Ajustes y progreso guardado

### Feature Layer (depends on core)

1. Daño, vida y muerte — depends on: Bucle de actualización, Pooling, Bus de eventos, Configuración de balance
2. Estadísticas y mejoras — depends on: Configuración de balance, Control del jugador, Daño vida y muerte, Localización
3. Enemigos — depends on: Bucle de actualización, Pooling, Configuración de balance, Daño vida y muerte
4. Ataque automático y proyectiles — depends on: Pooling, Daño vida y muerte, Enemigos, Estadísticas y mejoras
5. Oleadas, aparición y escalado — depends on: Pooling, Bus de eventos, Enemigos, Reloj de partida
6. Entorno del nivel — depends on: Pooling, Configuración de balance, Cámara, Daño vida y muerte
7. Drops, recolección y divisas — depends on: Pooling, Configuración de balance, Daño vida y muerte, Estadísticas y mejoras
8. Experiencia y subida de nivel — depends on: Bus de eventos, Daño vida y muerte, Drops y divisas, Estadísticas y mejoras, Localización
9. Tienda en partida — depends on: Bus de eventos, Drops y divisas, Estadísticas y mejoras
10. Cofres — depends on: Daño vida y muerte, Reloj de partida, Drops y divisas, Estadísticas y mejoras
11. Sobrecargas — depends on: Daño vida y muerte, Enemigos, Entorno del nivel, Estadísticas y mejoras

### Presentation Layer (depends on features)

1. HUD — depends on: Bus de eventos, Daño vida y muerte, Reloj de partida, Drops y divisas, Experiencia y subida de nivel, Estadísticas y mejoras, Localización
2. Minimapa — depends on: Bus de eventos, Enemigos, Tienda en partida, Cofres
3. Efectos visuales y feedback — depends on: Bucle de actualización
4. Música y efectos de sonido — depends on: Bus de eventos, Localización, Ajustes y progreso guardado
5. Menús, opciones, pausa y pantalla de fin — depends on: Reloj de partida, Música y efectos de sonido, Localización, Ajustes y progreso guardado

### Polish Layer (depends on everything)

1. Tutorial — depends on: Bus de eventos, Oleadas, Drops y divisas, Música y efectos de sonido, Localización, Ajustes y progreso guardado
2. Gráficos y rendimiento — depends on: Pooling, Localización, Ajustes y progreso guardado
3. Ranking global — depends on: Reloj de partida, Localización, Ajustes y progreso guardado
4. Narrativa y personaje de la I.A. — depends on: HUD, Tutorial
5. Metaprogresión — depends on: Drops y divisas, Estadísticas y mejoras, Menús, Ajustes y progreso guardado

---

## Recommended Design Order

Los sistemas independientes de una misma capa pueden diseñarse en paralelo. Los tres primeros
de Core son decisiones de arquitectura: se documentan con ADR (plan de adopción, paso 2.3) y su
GDD solo recoge el contrato de diseño. Sobrecargas va en el puesto 21 por dependencias, pero
todos sus sistemas de apoyo ya están implementados: puede diseñarse antes con las interfaces de
esos sistemas marcadas como provisionales.

| Order | System | Priority | Layer | Agent(s) | Est. Effort |
|-------|--------|----------|-------|----------|-------------|
| 1 | Configuración de balance | MVP | Foundation | systems-designer | M |
| 2 | Bucle de actualización | MVP | Foundation | systems-designer | S |
| 3 | Bus de eventos | MVP | Foundation | systems-designer | S |
| 4 | Ajustes y progreso guardado | MVP | Foundation | systems-designer | S |
| 5 | Pooling y fábrica de spawn | MVP | Core | systems-designer | S |
| 6 | Control del jugador | MVP | Core | game-designer | S |
| 7 | Cámara | MVP | Core | game-designer | S |
| 8 | Reloj de partida y fin de partida | MVP | Core | game-designer | M |
| 9 | Localización EN/ES | MVP | Core | game-designer | S |
| 10 | Daño, vida y muerte | MVP | Feature | game-designer | M |
| 11 | Estadísticas y mejoras | MVP | Feature | game-designer | M |
| 12 | Enemigos | MVP | Feature | game-designer | M |
| 13 | Ataque automático y proyectiles | MVP | Feature | game-designer | S |
| 14 | Oleadas, aparición y escalado de dificultad | MVP | Feature | game-designer | M |
| 15 | Entorno del nivel | MVP | Feature | game-designer | S |
| 16 | Drops, recolección y divisas | MVP | Feature | economy-designer | S |
| 17 | Experiencia y subida de nivel | MVP | Feature | game-designer | S |
| 18 | HUD | MVP | Presentation | game-designer | M |
| 19 | Tienda en partida | Vertical Slice | Feature | economy-designer | S |
| 20 | Cofres | Vertical Slice | Feature | economy-designer | S |
| 21 | Sobrecargas | Vertical Slice | Feature | game-designer | M |
| 22 | Efectos visuales y feedback | Vertical Slice | Presentation | game-designer | S |
| 23 | Minimapa | Vertical Slice | Presentation | game-designer | S |
| 24 | Música y efectos de sonido | Vertical Slice | Presentation | game-designer | S |
| 25 | Menús, opciones, pausa y pantalla de fin | Vertical Slice | Presentation | game-designer | M |
| 26 | Tutorial | Vertical Slice | Polish | game-designer | S |
| 27 | Gráficos y rendimiento | Vertical Slice | Polish | game-designer | S |
| 28 | Ranking global | Alpha | Polish | systems-designer | S |
| 29 | Narrativa y personaje de la I.A. | Alpha | Polish | game-designer | S |
| 30 | Metaprogresión: skins, tienda de gemas y árbol de habilidades | Full Vision | Polish | game-designer | M |

Esfuerzo: S = 1 sesión, M = 2-3 sesiones, L = 4+. Una sesión es una conversación de diseño
que produce un GDD completo; aquí, casi siempre un retrofit sobre una página o un código que ya
existen.

---

## Circular Dependencies

Resultado de un sondeo automático de referencias entre clases (por nombre de clase, en
`Assets/Scripts`): 15 de los 28 sistemas con código se referencian entre sí en ciclos. No todas
son dependencias de diseño; las que merecen una decisión:

- **Daño, vida y muerte ↔ Enemigos, Oleadas, Drops y Experiencia**: el código de muerte llama
  directamente a cada uno y ellos vuelven a llamar a vida/daño. Propuesta: la muerte es un
  evento del bus (Bus de eventos) y es el único contrato entre estos sistemas.
- **Tienda ↔ Experiencia y subida de nivel, Cofres ↔ Experiencia y subida de nivel**: la tienda y
  los cofres reutilizan `LevelUpManager` para elegir mejoras y este conoce su estado. Propuesta:
  una "oferta de mejoras" definida por Estadísticas y mejoras que los tres consuman.
- **Sobrecargas ↔ HUD**: el panel del HUD y `OverrideManager` se referencian entre sí.
  Propuesta: comunicarlos por eventos.
- **Configuración de balance ↔ Pooling, Ataque automático, Daño y Estadísticas**: los assets de
  configuración mencionan clases de ejecución. Propuesta: que la configuración sea solo datos.
- **Reloj de partida ↔ Música y efectos de sonido**: se llaman entre sí. Propuesta: eventos.

---

## High-Risk Systems

| System | Risk Type | Risk Description | Mitigation |
|--------|-----------|-----------------|------------|
| Enemigos; Oleadas, aparición y escalado | Technical | Cientos de enemigos simultáneos en WebGL, que es el objetivo principal | Pooling y cuadrícula de proximidad ya existen; medir con la captura de rendimiento y fijar presupuestos (plan de adopción 4.5) |
| Sobrecargas | Design | Balance entre sobrecargas y combinaciones secretas con mejoras especiales sin documentar; la página de Confluence no coincide con los assets | Retrofit de `overrides.md` desde los assets y `OverrideCombatResolver` (plan 2.2 y 4.6) |
| Reloj de partida y fin de partida | Design | El final del juego sigue sin decidir (jefe o fin por tiempo) y la duración cambió de 20-25 min a 5 min en la configuración | Decidir el final y la duración objetivo antes de escribir los criterios de aceptación |
| Cofres | Design | La página tiene marcas "WIP", "TBD" y "TBA" (tesoros sin diseñar) | Criterios solo para lo confirmado y el resto como preguntas abiertas |
| Gráficos y rendimiento | Technical | Sin presupuestos de draw calls ni de memoria; WebGL es el objetivo más ajustado | Fijarlos desde una build medida (`Docs/Performance/`) |
| Ranking global | Technical | Depende de un servicio externo (consulta a una base de datos por URL); errores y abuso sin documentar | Documentar los modos de fallo en su GDD; MAN-31 pide una lista real |
| Metaprogresión | Scope | Skins, tienda de gemas y árbol de habilidades solo existen como diseño | Mantenerla en Full Vision hasta decidir si entra en el alcance |

---

## Progress Tracker

| Metric | Count |
|--------|-------|
| Total systems identified | 30 |
| Design docs started | 7 |
| Design docs reviewed | 0 |
| Design docs approved | 0 |
| MVP systems designed | 0/18 |
| Vertical Slice systems designed | 1/9 |

---

## Next Steps

- [ ] Revisar y aprobar esta enumeración
- [ ] Retrofit de los GDD con página existente y diseño de los demás: `/design-system retrofit design/gdd/<archivo>.md` o `/design-system <sistema>`
- [ ] `/design-review` en cada GDD terminado
- [ ] Seguir el plan de `Docs/adoption-plan-2026-11-21.md` (ADR, registro de TR, control manifest)
- [x] Publicar este índice con `/atlassian-sync push`
