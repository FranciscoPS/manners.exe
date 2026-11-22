<!-- atlassian-mirror
source: confluence
id: 5734402
kind: page
title: Overrides
url: https://blackdoorstudios.atlassian.net/wiki/spaces/~701213bd1c0c92aa748079c0e5c6d387aa64b/pages/5734402
remote_version: v:4
pulled_at: 2026-11-21T23:15:17Z
body_sha1: c3399b94082432d3334367ce8fc7165189aa7041
-->
# Overrides

Las evoluciones son un concepto visto de manera prominente en juegos del género Survivors-like. La forma en la que funcionan es que por medio de las mejoras por nivel de las armas y algún otro objeto pasivo o de otra arma, dependiendo del juego y de sus sistemas, el jugador podrá obtener una nueva herramienta a su disposición que, de cierta manera, combina los atributos de sus partes originales y los expande en un ataque nuevo y más poderoso.


En este juego, un “Override” se define como una mejora adicional que otorga efectos únicos sin modificar las estadísticas base del personaje. Esta se obtiene cuando el jugador logra subir dos estadísticas distintas hasta un nivel específico, lo que desencadena automáticamente una mejora especial asociada a esa combinación. A nivel de gameplay, las evoluciones enriquecen la experiencia al añadir efectos que permiten al jugador adaptar y redefinir su estilo de juego en función de la evolución conseguida, al mismo tiempo que fomenta la planificación de futuras decisiones para obtener otras. Más que alterar las mecánicas existentes, este sistema introduce una nueva capa de progresión que convive con las demás sin interferir en su funcionamiento.

Asumiendo que se corta a los 20 minutos, el estimado de overrides que un jugador debería obtener en su partida sería de 3 a 4.


### **Propuesta de Evoluciones**

- **Área Criogénica**
    - Requisitos: 
        - Nivel de mejora de velocidad de movimiento: 5 (estimado)
        - Nivel de mejora de imán: 5 (estimado)
    - Efecto: 
        - Aparece un área circular de tamaño definido alrededor del jugador y que se mueve con él. El área causa que cualquier enemigo que entre en contacto con esta área sufra de una reducción a su velocidad de movimiento y sufran poco daño de manera constante.
- **Rayo láser**
    - Requisitos: 
        - Nivel de mejora de rango: 5 (estimado)
        - Nivel de mejora de velocidad de ataque: 5 (estimado)
    - Efecto:
        - Un rayo que viaja en una línea recta hacia donde el jugador esté viendo. Se lanza cada cierto tiempo y causa daño perforante a todo lo que toque. 
    - VFX Tutorial:
        - <https://www.youtube.com/watch?v=mGd3nYXj1Oc> 

- Pulso Electromagnético
    - Requisitos:
        - Nivel de mejora de rango: 5 (estimado)
        - Nivel de mejora de imán: 5 (estimado)
    - Efecto:
        - Cada cierto tiempo, el jugador de manera automática dispara un pulso que congela por cierto tiempo a los enemigos. Si un enemigo toca a otro que esté electrocutado se congelará también, pero no se acumula el efecto.

### **Posibles futuros overrides**

- Pisotón 
    - Velocidad de Movimiento y daño lvl 5
    - Las pisadas del robot generan daño de área, además de un knockback
- Rastro de pinchos ¿?
    - Velocidad de movimiento y Velocidad de Ataque nivel 6
    - Conforme el jugador se mueve, deja un “rastro” de pinchos detrás, los cuales hacen daño a los enemigos que entren en contacto con ellos. Los pinchos  desaparecen después de unos cuantos segundos.
- Bomba magnética ¿?
    - Daño e Imán nivel 6
    - Cada ciertos disparos, además de una bala normal, dispara una bomba a los enemigos, la cual los atrae y explota después de 1 segundo.


# Ideas

- Daño en área constante
- Balas que atraviesan los enemigos 
- Munición de daño persistente (Incendiaria)
- Munición congelante (ralentizar enemigos) 
- Ataques en área
- Torretas
- Munición tipo boomerang
- Munición eléctrica (balas eléctricas)

---

> **Status**: Designed
> **Last Updated**: 2026-11-21
> **Last Verified**: 2026-11-21, contra el código y los assets de producción (`Assets/Scripts/Overrides/`, `Assets/Configurations/Production/Overrides/`, `PlayerStatsManager`)
> **System**: Sobrecargas (overrides), n.º 19 de `design/gdd/systems-index.md`
> **Origen**: lo que está encima de la línea es la página original de Confluence y no se ha modificado. Las secciones de abajo se añadieron el 2026-11-21 y describen el juego tal como está construido; donde la página y el juego difieren, la diferencia está en *Open Questions*.

## Summary (Resumen)

Las sobrecargas (overrides) son mejoras especiales que se activan solas cuando el jugador sube dos estadísticas base distintas a un nivel concreto, sin cambiar las estadísticas del personaje. Hoy hay tres (área criogénica, rayo láser y pulso electromagnético) y se refuerzan entre sí y con las mejoras premium (Multi disparo, Balas explosivas y Cadena de impacto) sin que el jugador haga nada. Existen para dar sorpresa y rejugabilidad: el jugador descubre qué combinaciones las desbloquean y el juego recuerda sus descubrimientos entre partidas.

> **Quick reference** — Layer: `Feature` · Priority: `Vertical Slice` · Key deps: Estadísticas y mejoras, Enemigos, Daño vida y muerte, Entorno del nivel

## Overview (Visión general)

Una sobrecarga es un efecto automático que se suma al disparo normal del jugador. Se desbloquea al llevar dos mejoras concretas a un nivel concreto (hoy, nivel 2 en cada una), se activa en ese mismo instante, sin pausa ni confirmación, y sigue activa hasta que termina la partida. El jugador no la controla: la consigue decidiendo qué cartas de mejora elegir al subir de nivel y qué comprar en la tienda, y después la ve actuar. Sin ellas, las mejoras serían solo números que suben; la sobrecarga es el momento en que lo elegido cambia lo que se ve en pantalla, y la pregunta «¿qué combinación abre la siguiente?» es lo que debe hacer que el jugador quiera empezar otra partida (la página «Sistemas» lo plantea como la fuente de novedad y de *replay value*).

Las tres sobrecargas actuales, con su nombre en español y en inglés:

- **Área Criogénica** (*Cryogenic field*): un campo que sigue al jugador y ralentiza y daña a los enemigos que entran.
- **Rayo Láser** (*Laser beam*): cada pocos segundos, un rayo perforante barre el suelo hacia el enemigo más cercano.
- **Pulso Electromagnético** (*Electromagnetic pulse*): una onda que congela a los enemigos cercanos; el congelamiento salta entre ellos.

Cada sobrecarga además se combina con las mejoras premium del jugador y con las otras sobrecargas. Esas combinaciones están siempre activas cuando el jugador tiene las piezas y son un secreto: el juego no las explica (ver *Detailed Design*, regla 9). Cómo se configuran y aplican los efectos (una configuración por sobrecarga con todos sus números y su visual) es una decisión de arquitectura: → se documenta en un ADR.

## Detailed Design (Diseño detallado)

### Core Rules (Reglas)

1. **Definición.** Cada sobrecarga se define con dos conjuntos de datos: su identidad y requisitos (nombre y descripción en español e inglés, icono, las dos mejoras requeridas y el nivel que se pide de cada una) y su efecto (el comportamiento más una configuración con todos sus números y su visual). El juego lee la lista de sobrecargas de una base de datos (`OverrideDatabase_Production`): una sobrecarga que no esté en esa lista no existe para el juego. El identificador de guardado de cada una (`PersistentId`) no cambia nunca, ni al renombrar el archivo ni al cambiar de idioma.
2. **Activación.** Cada vez que se aplica una mejora al jugador, el juego revisa las sobrecargas que aún no están activas. Una se activa cuando el nivel actual de su mejora A es mayor o igual que el nivel requerido A **y** el de su mejora B es mayor o igual que el requerido B (fórmula 1). Si varias cumplen a la vez, se activan todas en la misma revisión, en el orden de la base de datos.
3. **Al activarse.** Se crea el efecto con su configuración, se anuncia en pantalla (regla 10) y se registra como descubierta (regla 8).
4. **Permanencia.** Una vez activa, una sobrecarga sigue activa hasta el final de la partida: no se pierde, porque los niveles solo suben. Al cargar otra escena se descartan todas y la partida siguiente empieza sin ninguna.
5. **Las tres sobrecargas de producción** (valores de `Assets/Configurations/Production/Overrides/`):

| Sobrecarga | Requisitos | Efecto | Valores de producción |
|---|---|---|---|
| Área Criogénica | Velocidad de movimiento ≥ 2 e Imán ≥ 2 | Un campo circular centrado en el jugador, que lo sigue. Los enemigos dentro van más lentos mientras estén dentro y reciben daño cada intervalo. | Radio 10 · velocidad al 63 % · 2 de daño cada 1 s |
| Rayo Láser | Rango de ataque ≥ 2 y Daño ≥ 2 | Cada intervalo, un barrido: el punto de impacto del rayo se desliza por el suelo desde el objetivo hasta más allá de él, dañando a todo enemigo cerca del segmento y destruyendo los edificios que toca. Objetivo: el enemigo más cercano dentro del alcance; si no hay ninguno, un edificio al azar dentro del alcance; si tampoco, un punto al azar. | Cada 3 s · alcance 14 · barrido de 1,2 s · se extiende 4 más allá del objetivo · 15 de daño cada 0,09 s a ≤ 1,2 del segmento |
| Pulso Electromagnético | Rango de ataque ≥ 2 e Imán ≥ 2 | Cada intervalo, una onda se expande desde el jugador y congela (velocidad 0) a los enemigos que alcanza; al terminar la expansión, el congelamiento salta a los enemigos cercanos a los ya congelados, sin sumar tiempo. | Cada 5 s · radio 15 en 0,8 s · congela 2 s · contagio a ≤ 2,5, hasta 3 saltos |

6. **Combinaciones con las mejoras premium.** Están siempre activas si el jugador tiene la mejora; nunca dependen de una probabilidad.
   - **Multi disparo (nivel ≥ 1).** Cada sobrecarga repite su efecto en «ecos». El número total de emisiones sale de la fórmula 2 (nivel 1: 2 emisiones; nivel 2: 4; nivel 3: 5). La primera es el efecto normal; los ecos llegan con 0,15 s de retardo y no generan más ecos. *Criogénica*: ticks de daño adicionales; el intervalo siguiente empieza al terminar los ecos. *Láser*: rayos adicionales dentro del mismo barrido, cada uno con su propio objetivo y la misma prioridad que el principal (enemigo más cercano, luego un edificio al azar, luego un punto al azar), separados al menos 40° entre sí y sin repetir objetivo. *Pulso*: ondas adicionales una tras otra, cada una cuando termina de expandirse la anterior más el retardo; el intervalo normal vuelve a contar al terminar la secuencia.
   - **Balas explosivas (nivel ≥ 1).** Cada tick de ataque de una sobrecarga que golpee al menos a un enemigo produce exactamente una explosión de radio 3 centrada en el primer impacto del tick. Daña, con el daño de ese ataque, a todo enemigo vivo dentro del radio, incluidos los ya golpeados en ese tick. La explosión no genera otra explosión. Sin ningún impacto no hay explosión (fórmula 5).
   - **Cadena de impacto (nivel ≥ 1; en el código se llama *Knockback*).** Cada enemigo alcanzado por un ataque de sobrecarga sale empujado, como máximo una vez por tick. Al cerrar el tick, el empuje salta de enemigo en enemigo desde el primer impacto: cada salto empuja al enemigo vivo más cercano que aún no haya sido empujado y esté a 2,5 o menos del último empujado. La fuerza baja un 5 % en cada salto y la cadena termina cuando no queda ninguno o se alcanza el número de saltos (fórmula 6).
7. **Combinaciones entre sobrecargas.**
   - **Criogénica + Láser**: el daño del láser contra un enemigo ralentizado se multiplica por 1,25; además, los impactos del láser y las explosiones ralentizan al enemigo como lo haría el campo.
   - **Pulso + Láser**: el láser multiplica por 1,5 su daño contra enemigos congelados; y cada onda del pulso suma, al enemigo que alcanza, la mitad del daño por tick del láser (7,5 con los valores de producción) mientras el láser esté activo.
   - **Pulso + Criogénica**: la duración del congelamiento del pulso se multiplica por 1,5 (3 s en lugar de 2) mientras la criogénica esté activa.
   - Si a un mismo golpe de láser le corresponden varios multiplicadores (enemigo ralentizado y congelado), se aplica el mayor, no su producto (fórmula 3).
8. **Descubrimiento y progreso entre partidas.** El juego guarda el nivel máximo alcanzado de cada mejora y qué sobrecargas se han desbloqueado alguna vez. Subir por primera vez de 0 a 1 una mejora que es requisito de alguna sobrecarga cuenta como una «pieza nueva» de la partida; desbloquear una sobrecarga por primera vez cuenta como una «sobrecarga nueva». Ambos contadores se reinician al empezar cada partida y los lee la pantalla de fin. El progreso guardado con el nombre anterior del sistema (claves «Synergy…») se migra una sola vez al formato actual y se borra el antiguo.
9. **Secreto.** Ningún texto que vea el jugador menciona las combinaciones con las mejoras premium ni las combinaciones entre sobrecargas: ni las descripciones de las sobrecargas ni las de las mejoras, ni los paneles ni los anuncios. Este documento es interno y sí puede describirlas.
10. **Presentación en pantalla.**
    - **Anuncio de activación**: la primera vez que una sobrecarga se activa en la partida, un aviso animado (textos que entran y salen; el icono aparece en el centro y vuela a la tira de sobrecargas activas del HUD). Si se activan varias, se anuncian de una en una; cada una se anuncia una sola vez por partida.
    - **Panel Colección** (menú principal y pantalla de fin): una fila por sobrecarga con el progreso entre partidas; los niveles muestran el máximo alcanzado y el resultado se revela y brilla en cuanto se descubre. Lo no descubierto muestra «?».
    - **Panel del HUD** (en partida): guía de la partida actual; los contadores de nivel empiezan en 0, las sobrecargas ya descubiertas en otras partidas muestran siempre su icono de resultado, atenuado hasta que se activan en esta, y lo no descubierto conserva el «?».

### States and Transitions (Estados y transiciones)

| Estado de una sobrecarga | Cuándo | Cómo se ve |
|---|---|---|
| Desconocida | Nunca se ha desbloqueado en este equipo | «?» en los paneles |
| Conocida, inactiva | Se desbloqueó en otra partida; en esta aún no cumple los requisitos | Icono de resultado atenuado en el panel del HUD |
| Activa | Cumple los requisitos en esta partida | Icono completo en la tira del HUD; su efecto actúa |

| Transición | Disparador |
|---|---|
| Desconocida → Activa | Se cumplen los requisitos por primera vez; se guarda como descubierta |
| Conocida, inactiva → Activa | Se cumplen los requisitos en esta partida |
| Activa → Conocida, inactiva | Termina la partida o se carga otra escena; las sobrecargas se desactivan por completo si se apaga la opción interna de pruebas |

### Interactions with Other Systems (Interacciones)

| Sistema | Entra en Sobrecargas | Sale de Sobrecargas | Quién es dueño de la interfaz |
|---|---|---|---|
| Estadísticas y mejoras | Nivel de cada mejora; aviso de «mejora aplicada»; datos de Multi disparo, Balas explosivas y Cadena de impacto | — | Estadísticas y mejoras: Sobrecargas solo lee |
| Enemigos | Enemigos vivos como objetivos y su estado (ralentizado, congelado) | Ralentización, congelamiento y empuje | Enemigos |
| Daño, vida y muerte | Registro de enemigos activos | Daño a los enemigos; la muerte la resuelve Daño, vida y muerte | Daño, vida y muerte |
| Entorno del nivel | Edificios activos | El láser los elige como objetivo alternativo y destruye los que toca | Entorno del nivel |
| Efectos visuales, Cámara y Música y efectos | — | VFX de cada efecto; sacudida de cámara al disparar el láser; sonido al activarse y al disparar | Cada sistema de presentación |
| Localización EN/ES | Idioma actual | Nombre y descripción de cada sobrecarga en ese idioma | Localización |
| Ajustes y progreso guardado | Progreso guardado | Nivel máximo de cada mejora y sobrecargas desbloqueadas | Sobrecargas |

## Formulas (Fórmulas)

Los valores son los de producción (`Assets/Configurations/Production/Overrides/` y `PlayerStatsManager`). El registro de entidades (`design/registry/entities.yaml`) existe y está vacío, así que ninguno choca con valores ya fijados; al terminar se proponen para registrarlos.

### Fórmula 1: activación

La fórmula de activación se define como:

`activa(o) = nivel(mejoraA_o) ≥ nivelReqA_o  Y  nivel(mejoraB_o) ≥ nivelReqB_o`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `nivel(mejora)` | int | 0–999 (solo sube) | calculated (`PlayerStatsManager.GetUpgradeLevel`) | Nivel actual de la mejora en esta partida |
| `mejoraA_o`, `mejoraB_o` | enum `UpgradeType` | cualquier mejora | data file (`OverrideData`) | Las dos mejoras que pide la sobrecarga `o` |
| `nivelReqA_o`, `nivelReqB_o` | int | 1–20 | data file (`OverrideData`) | Nivel que se pide de cada una; hoy 2 y 2 en las tres |

**Output Range:** verdadero o falso. Se evalúa tras cada mejora aplicada y, una vez verdadero, no vuelve a falso en la partida. Con nivel 0 (mejora sin comprar) es siempre falso, porque el requisito mínimo es 1.
**Example:** Rayo Láser (Rango de ataque ≥ 2 y Daño ≥ 2). Con Rango 2 y Daño 1 → falso. Al subir Daño a 2 → verdadero, y el láser se activa en esa misma revisión.

### Fórmula 2: emisiones con Multi disparo

La fórmula de emisiones se define como:

`balas = 1 + 3 × nivelMultiDisparo`   (con nivel 0: 1 bala, sin combinación)
`emisiones = clamp( ceil(balas / 2), 1, 1 + min(extraMáx, 8) )`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `nivelMultiDisparo` | int | 0–999 | calculated (`PlayerStatsManager.GetUpgradeLevel`) | Nivel de Multi disparo del jugador |
| `3` | int | fijo | constant (`PlayerStatsManager.GetMultiShotExtraBullets`) | Balas extra por nivel |
| `2` | int | fijo | constant (`OverrideCombatResolver`) | Balas que equivalen a una emisión |
| `extraMáx` | int | 0–8 | data file (`maxExtraPulses` y `maxExtraBeams` de cada configuración) | Tope de emisiones adicionales; 8 en producción; 0 desactiva la combinación en esa sobrecarga |

**Output Range:** de 1 a 9 con `extraMáx` = 8. Nivel 0 → 1; nivel 1 → 4 balas → 2; nivel 2 → 7 → 4; nivel 3 → 10 → 5; nivel 4 → 13 → 7; nivel 5 → 16 → 8; nivel 6 → 19 → 10, que el tope deja en 9; de ahí en adelante, 9.
**Example:** nivel 2 → 7 balas → ceil(3,5) = 4 emisiones: el efecto normal más 3 ecos.
**Límites:** `extraMáx` = 0 deja siempre 1 emisión. Con nivel 999 el resultado sigue siendo 9: el tope impide un resultado sin límite. Desde el nivel 6, más niveles de Multi disparo no dan emisiones extra a las sobrecargas (siguen mejorando el disparo normal).

### Fórmula 3: daño de un impacto de sobrecarga

La fórmula de daño se define como:

`dañoFinal = dañoBase × multiplicador`

`dañoBase` según la fuente: Criogénica = `damagePerTick`; Láser = `damage`; Pulso = `dañoLáser × empDamageMultiplier` si el Láser está activo y 0 si no.
`multiplicador`, solo para impactos del Láser (para las demás fuentes vale 1): `max(1, multCryo si el enemigo está ralentizado, multEmp si el enemigo está congelado)`.

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `damagePerTick` | float | ≥ 0 | data file (`CryoFieldConfig`) | Daño de la Criogénica por intervalo; 2 |
| `damage` | float | ≥ 0 | data file (`LaserBeamConfig`) | Daño del Láser por tick (cada 0,09 s); 15 |
| `empDamageMultiplier` | float | ≥ 0 | data file (`LaserBeamConfig`) | Fracción del daño del Láser que suma cada onda del Pulso; 0,5 |
| `multCryo` | float | ≥ 1 | data file (`CryoFieldConfig.laserDamageMultiplier`) | Multiplicador del Láser contra enemigos ralentizados; 1,25 |
| `multEmp` | float | ≥ 1 | data file (`EmpPulseConfig.laserDamageMultiplier`) | Multiplicador del Láser contra enemigos congelados; 1,5 |
| ralentizado / congelado | bool | — | calculated (estado del enemigo) | Un enemigo congelado (velocidad 0) cuenta también como ralentizado |

**Output Range:** Láser contra un enemigo normal, 15; ralentizado, 18,75; congelado (o congelado y ralentizado), 22,5. Criogénica, 2 por enemigo e intervalo. Pulso, 0 sin Láser y 7,5 por enemigo alcanzado con Láser.
**Example:** Láser (15) contra un enemigo congelado y ralentizado: max(1; 1,25; 1,5) = 1,5 → 22,5. No se multiplican entre sí (15 × 1,25 × 1,5 = 28,1 no ocurre).
**Límites:** con daño 0 el resultado es 0 pero se conservan la ralentización, el congelamiento y el empuje. Los multiplicadores tienen mínimo 1 en los datos, así que nunca reducen el daño; no tienen tope superior (ver *Open Questions*).

### Fórmula 4: duración del congelamiento del Pulso

La fórmula de duración se define como:

`duración = freezeDuration × (Criogénica activa ? max(1, cryoFreezeDurationMultiplier) : 1)`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `freezeDuration` | float | ≥ 0 | data file (`EmpPulseConfig`) | Segundos de congelamiento de un enemigo alcanzado; 2 |
| `cryoFreezeDurationMultiplier` | float | ≥ 1 | data file (`EmpPulseConfig`) | Multiplicador con la Criogénica activa; 1,5 |
| Criogénica activa | bool | — | calculated | Si el Área Criogénica está activa en la partida |

**Output Range:** 2 s sin Criogénica y 3 s con ella. Se calcula al empezar cada pulso. El contagio a vecinos aplica la misma duración; no la suma.
**Example:** pulso con la Criogénica activa → 2 × 1,5 = 3 s de congelamiento.
**Límites:** `freezeDuration` = 0 deja el pulso sin congelar (la onda se expande igual). Sin tope superior en los datos.

### Fórmula 5: explosión (Balas explosivas)

La fórmula de explosión se define como:

`explosión = si nivelBalasExplosivas ≥ 1 y el tick golpeó al menos a un enemigo: una explosión de radio 3 en el primer impacto, con daño = dañoFinal del ataque`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `nivelBalasExplosivas` | int | 0–999 | calculated (`PlayerStatsManager.GetUpgradeLevel`) | Nivel de Balas explosivas del jugador |
| radio | float | 3 | constant (`PlayerStatsManager.GetExplosionRadius`) | Radio de la explosión; no depende del nivel |
| `dañoFinal` | float | ≥ 0 | calculated (fórmula 3) | Daño del ataque que la provoca; el Pulso, que no tiene daño propio, usa el daño del jugador (`GetModifiedDamage`) |

**Output Range:** 0 o 1 explosión por tick; cada enemigo vivo a ≤ 3 del primer impacto recibe `dañoFinal`. Sin impactos, sin explosión.
**Example:** tick del Láser con Balas explosivas que golpea a 3 enemigos: una explosión de radio 3 que daña con 15 (o con el multiplicador que toque) a todos los vivos en ese radio.
**Límites:** con nivel 0 no hay explosión; el daño 0 de una fuente distinta del Pulso da una explosión de daño 0. Desde el nivel 1, subir el nivel no cambia el radio ni el daño en las sobrecargas.

### Fórmula 6: Cadena de impacto

La fórmula de la cadena se define como:

`fuerza(L) = 7 + 0,8 × (L − 1)`
`saltos(L) = min(32, 7 + 6 × (L − 1))`
`fuerzaSalto(k) = fuerza(L) × 0,95^k`   (k = 0 para el primer salto)

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `L` | int | 0–999 | calculated (`PlayerStatsManager.GetUpgradeLevel`) | Nivel de Cadena de impacto; con 0 no hay empuje ni cadena |
| `0,8`, `6`, `7` | float | fijo | constant (`PlayerStatsManager`) | Pendientes y valores base de fuerza y saltos |
| `32` | int | fijo | constant (`OverrideCombatResolver.MaxChainJumps`) | Tope de saltos |
| `0,95` | float | fijo | constant (`OverrideCombatResolver`) | Factor de fuerza por salto |
| radio de salto | float | 2,5 | constant (`OverrideCombatResolver.ChainRadius`) | Distancia máxima entre un empujado y el siguiente |
| duración del empuje | float | 0,3 s | constant (`OverrideCombatResolver`) | Tiempo que dura el empuje de cada enemigo |

**Output Range:** nivel 1: fuerza 7 y 7 saltos; nivel 2: 7,8 y 13; nivel 3: 8,6 y 19; nivel 4: 9,4 y 25; nivel 5: 10,2 y 31; nivel 6: 11 y 32 (tope); de ahí en adelante los saltos se quedan en 32 y la fuerza sigue subiendo.
**Example:** nivel 3 → fuerza 8,6 y hasta 19 saltos; el cuarto salto empuja con 8,6 × 0,95³ ≈ 7,4.
**Límites:** la fuerza no tiene tope (con nivel 999 sería ≈ 805); ver *Open Questions*. A partir del nivel 6 más niveles no añaden saltos.

### Bucles de retroalimentación

- **Positivo e intencionado:** más Multi disparo → más ecos → más daño y más control → enemigos derrotados antes → más experiencia → más niveles. Lo amortiguan el tope de 9 emisiones (nivel 6), el tope de 32 saltos, que los ecos no generen ecos y el escalado de la dificultad de los enemigos.

## Edge Cases (Casos límite)

Cada caso nombra la condición y el resultado exacto. Los marcados con una prueba existen ya en `OverrideCombinationTests`; los dudosos están en *Open Questions*.

- **Si una mejora aplicada cumple los requisitos de dos sobrecargas a la vez**: se activan las dos en la misma revisión, en el orden de la base de datos, y sus anuncios salen uno tras otro.
- **Si la mejora que completa los requisitos se aplica cuando el jugador no existe en la escena** (cambio de escena): no se activa nada en esa revisión y no se reintenta hasta la siguiente mejora aplicada. No ocurre en una partida normal.
- **Si una sobrecarga no tiene prefab de efecto, o su prefab no implementa el comportamiento esperado**: no se activa y se avisa en la consola. Se registra como descubierta antes de intentarlo y el intento se repite con cada mejora posterior.
- **Si una sobrecarga no está en la base de datos**: no existe para el juego; nunca se activa ni se descubre.
- **Si una sobrecarga no tiene configuración**: se activa con los valores por defecto del efecto, que no son los de producción (por ejemplo, radio 4 en lugar de 10 en la Criogénica), y se avisa en la consola.
- **Si se apagan las sobrecargas con la opción interna de pruebas**: se retiran todas las activas y ninguna cuenta para las combinaciones; al volver a encenderlas se activan las que cumplan los requisitos.
- **Si se carga otra escena mientras hay efectos en marcha**: se destruyen todos los efectos y sus visuales, y la partida siguiente empieza sin ninguno.
- **Si el jugador tiene Multi disparo y `extraMáx` de una sobrecarga es 0**: esa sobrecarga hace siempre 1 emisión.
- **Si un tick no golpea a ningún enemigo**: no hay explosión ni cadena, y no se reutiliza el impacto de un tick anterior (`EmptyOrCancelledTicksCannotReuseAnEarlierImpact`).
- **Si el jugador consigue o pierde Balas explosivas a mitad de un tick**: la mejora se comprueba al empezar cada tick; el que empezó sin ella no explota y el que empezó con ella explota aunque cambie antes de cerrarse (`EachTickHasExactlyOneExplosionAndNeverRecurses`).
- **Si un enemigo recibe varios impactos en el mismo tick**: solo cuenta el primero (el daño no se duplica, `EachTickHasExactlyOneExplosionAndNeverRecurses`) y se empuja como mucho una vez.
- **Si el daño de un impacto mata a otros enemigos durante el tick**: todos los objetivos ya recogidos reciben su impacto, también los de la explosión; la cadena de impacto solo empuja a enemigos vivos (`LaserHitsEveryTargetWhenDeathsRemoveEnemiesFromTheRegistry`, `LethalExplosionStillHitsAllCollectedTargets`, `EmpChainSurvivesLethalLaserDamageButStopsAtConfiguredHopCount`).
- **Si el Láser no encuentra enemigo ni edificio dentro de su alcance**: elige un punto al azar a una distancia del 30 % al 100 % del alcance. Para cada rayo prueba hasta 12 puntos, se detiene en el primero que cumple la separación de 40° y, si ninguno la cumple, usa el menos alineado con los demás, que puede quedar por debajo de 40° (`LaserBeamsWithoutEnoughTargetsSpreadToSeparatedGroundPoints`).
- **Si un enemigo está ralentizado y congelado a la vez**: el Láser usa el mayor multiplicador, no su producto (`LaserCarriesFrostAndUsesStrongestExistingControlBonus`).
- **Si el Pulso no tiene Láser activo y el jugador tiene Balas explosivas**: la explosión usa el daño del jugador; si es 0, la explosión hace 0. Una fuente con daño 0 distinta del Pulso no toma el daño del jugador (`EmpUsesPlayerDamageForExplosionsUntilLaserIsActive`, `ZeroDamageAttacksCannotGainPlayerDamageThroughExplosion`).
- **Si la cadena de impacto no encuentra ningún enemigo a 2,5 o menos del último empujado**: termina antes de agotar los saltos.
- **Si el nivel de Cadena de impacto es 6 o más**: los saltos se quedan en 32; la fuerza sigue subiendo.
- **Si el jugador cambia el idioma durante la partida**: el nombre y la descripción de las sobrecargas pasan al idioma nuevo; los efectos no se interrumpen.
- **Si el progreso guardado viene de la versión anterior del sistema** (claves «Synergy…»): se copia una sola vez al formato actual, con el mayor de los dos niveles de cada mejora, y se borran las claves antiguas.
- **Si se borra el progreso guardado** (herramienta del editor «Borrar progreso guardado de sobrecargas»): todas vuelven a «?» y los contadores de la partida se reinician; las sobrecargas ya activas en una partida en curso no se desactivan.

## Dependencies (Dependencias)

### Sistemas de los que depende (upstream)

| Sistema | Tipo | Interfaz de datos | GDD |
|---------|------|-------------------|-----|
| Estadísticas y mejoras | Hard | Lee el nivel de cada mejora y escucha el aviso de «mejora aplicada»; usa los niveles de Multi disparo, Balas explosivas y Cadena de impacto y sus datos (fórmulas 2, 5 y 6) | `design/gdd/progression.md` (formato antiguo; no recoge estos datos) |
| Enemigos | Hard | Enemigos vivos como objetivos y su estado (ralentizado, congelado); recibe ralentización, congelamiento y empuje | Sin GDD: supuesto provisional |
| Daño, vida y muerte | Hard | Registro de enemigos activos; recibe el daño de cada impacto | Sin GDD: supuesto provisional |
| Entorno del nivel | Soft | El Láser usa edificios como objetivo alternativo y destruye los que toca; sin edificios funciona igual | Sin GDD: supuesto provisional |
| Efectos visuales y feedback, Cámara, Música y efectos de sonido | Soft | VFX, sacudida de cámara y sonidos de cada efecto; sin ellos el efecto de juego es el mismo | Sin GDD |
| Localización EN/ES | Soft | Nombre y descripción de cada sobrecarga según el idioma | Sin GDD |
| Ajustes y progreso guardado | Soft | Guarda y lee el progreso de descubrimiento; sin él las sobrecargas funcionan en la partida pero no se recuerdan | Sin GDD |
| Ataque automático y proyectiles | Soft (conceptual) | Comparte con las mejoras premium la regla «siempre activo, nunca por probabilidad»; no hay enlace en el código | Sin GDD |

### Sistemas que dependen de este (downstream)

Ninguno en el índice. El HUD y los Menús alojan los paneles de la regla 10, pero esos paneles pertenecen a este sistema; en el código el panel del HUD y el gestor de sobrecargas se referencian entre sí (ver *Circular Dependencies* en `design/gdd/systems-index.md`).

### Consistencia entre documentos

Cuando se escriban o retoquen los GDD de Estadísticas y mejoras, Enemigos, Daño vida y muerte y Entorno del nivel, cada uno debe listar a Sobrecargas como sistema que depende de él. Hoy ninguno lo hace porque no existen o están en formato antiguo.

## Cross-References (Referencias cruzadas)

| This Document References | Target GDD | Specific Element Referenced | Nature |
|--------------------------|-----------|----------------------------|--------|
| «Los niveles de cada mejora y el aviso de mejora aplicada activan las sobrecargas» | `design/gdd/progression.md` (Estadísticas y mejoras) | Nivel por mejora y evento «mejora aplicada» | State trigger |
| «Multi disparo: 3 balas extra por nivel» | `design/gdd/progression.md` | Regla de balas de Multi disparo (nivel × 3) | Data dependency |
| «Balas explosivas: radio 3» | `design/gdd/progression.md` | Radio de la explosión | Data dependency |
| «Cadena de impacto: fuerza 7 + 0,8 × (L − 1) y saltos 7 + 6 × (L − 1)» | `design/gdd/progression.md` | Fuerza y saltos de Cadena de impacto | Data dependency |
| «El Pulso usa el daño del jugador para sus explosiones» | `design/gdd/progression.md` | Daño modificado del jugador (`GetModifiedDamage`) | Data dependency |
| «Los impactos aplican daño, ralentización, congelamiento y empuje» | Enemigos (n.º 9 del índice, sin GDD) | Estados ralentizado y congelado, empuje | State trigger |
| «La muerte de un enemigo la resuelve Daño, vida y muerte» | Daño, vida y muerte (n.º 8 del índice, sin GDD) | Daño a enemigos y registro de enemigos activos | Rule dependency |
| «El Láser destruye los edificios que toca y los elige como objetivo» | Entorno del nivel (n.º 12 del índice, sin GDD) | Edificios activos y destrucción por impacto | Rule dependency |

## Acceptance Criteria (Criterios de aceptación)

Cada criterio indica la regla o fórmula que cubre, el tipo de evidencia que exige el framework (Logic, Integration, UI o Config/Data) y la prueba que ya existe en `OverrideCombinationTests`, si la hay. «Falta prueba» significa que hay que escribirla o hacer la verificación a mano.

### Reglas

- **AC-1** (regla 1 · Config/Data): **GIVEN** los assets de producción, **WHEN** se leen las tres sobrecargas, **THEN** sus `PersistentId` son `Override_CryoField`, `Override_LaserBeam` y `Override_EmpPulse`, las tres están en `OverrideDatabase_Production` y cada una tiene prefab de efecto y configuración. *Falta prueba.*
- **AC-2** (regla 2 y fórmula 1, con el límite del nivel · Integration): **GIVEN** una partida nueva, **WHEN** el jugador sube Rango de ataque a 2 y Daño a 1, **THEN** el Rayo Láser sigue inactivo; **WHEN** sube Daño a 2, **THEN** el Láser se activa en esa misma revisión. *Falta prueba.*
- **AC-3** (regla 2 · Integration): **GIVEN** Rango de ataque 2, Velocidad de movimiento 2 e Imán 1, sin sobrecargas activas, **WHEN** el jugador sube Imán a 2, **THEN** se activan en la misma revisión el Área Criogénica y el Pulso Electromagnético, cada una una sola vez. *Falta prueba.*
- **AC-4** (reglas 3 y 10 · UI): **GIVEN** dos sobrecargas que se activan en la misma revisión, **WHEN** termina el aviso de la primera, **THEN** aparece el de la segunda, y ninguna se anuncia más de una vez en la partida. *Verificación con captura.*
- **AC-5** (regla 4 · Integration): **GIVEN** dos sobrecargas activas, **WHEN** el jugador termina la partida y empieza otra, **THEN** la nueva no tiene ninguna activa, la tira del HUD está vacía y no queda ningún efecto ni visual de la anterior. *Falta prueba.*
- **AC-6** (regla 5, Criogénica · Integration): **GIVEN** el Área Criogénica activa con los valores de producción y un enemigo a 11 del jugador, **WHEN** el enemigo entra a menos de 10, **THEN** su velocidad pasa al 63 % mientras esté dentro y recibe 2 de daño cada 1 s; **WHEN** sale del radio, **THEN** deja de recibir daño y recupera su velocidad al terminar la ralentización. *Prueba parcial: `CryoMultishotHasHalfItsBulletsAsDelayedTicksWithoutRecursiveBursts`.*
- **AC-7** (regla 5, objetivo del Láser · Logic): **GIVEN** el Láser activo y enemigos a 5 y a 9 del jugador, **WHEN** dispara, **THEN** apunta al de 5. **GIVEN** ningún enemigo a 14 o menos pero un edificio a 10, **THEN** apunta a ese edificio. **GIVEN** ni enemigos ni edificios a 14 o menos, **THEN** apunta a un punto del suelo entre 4,2 y 14 del jugador. *Prueba parcial: `LaserMultishotAimsEachBeamAtTheNearestSeparatedEnemy`, `LaserBeamsWithoutEnoughTargetsSpreadToSeparatedGroundPoints`.*
- **AC-8** (regla 5, daño del Láser · Integration): **GIVEN** el Láser activo con los valores de producción, **WHEN** un barrido de 1,2 s pasa sobre un enemigo normal, **THEN** recibe 15 de daño por cada tick de 0,09 s mientras esté a 1,2 o menos del segmento del rayo, y los edificios que toca el rayo se destruyen. *Falta prueba.*
- **AC-9** (regla 5, Pulso · Logic): **GIVEN** el Pulso activo sin Criogénica, **WHEN** se dispara, **THEN** la onda llega a radio 15 en 0,8 s, cada enemigo alcanzado queda a velocidad 0 durante 2 s y el congelamiento salta a vecinos a 2,5 o menos con un máximo de 3 saltos, sin alargar el tiempo. *Prueba parcial: `EmpChainSurvivesLethalLaserDamageButStopsAtConfiguredHopCount` (saltos).*
- **AC-10** (regla 6, Multi disparo y fórmula 2, con límites · Logic): **GIVEN** `extraMáx` = 8, **WHEN** se pide el número de emisiones con Multi disparo en nivel 0, 1, 2, 3 y 100, **THEN** son 1, 2, 4, 5 y 9; con `extraMáx` = 0 es siempre 1 y con `extraMáx` = 2 en nivel 2 son 3. *Prueba: `MultishotAlwaysEmitsHalfItsBulletsRoundedUp`.*
- **AC-11** (regla 6, ecos de la Criogénica · Logic): **GIVEN** la Criogénica con Multi disparo nivel 1, **WHEN** pasa un intervalo, **THEN** hay un tick principal y 1 eco tras el retardo configurado, el eco no inicia otra ráfaga y el siguiente intervalo empieza al terminar. *Prueba: `CryoMultishotHasHalfItsBulletsAsDelayedTicksWithoutRecursiveBursts`.*
- **AC-12** (regla 6, rayos del Láser · Logic): **GIVEN** el Láser con Multi disparo nivel 2, **WHEN** dispara un barrido, **THEN** salen 4 rayos con direcciones separadas al menos 40° y cada uno con su propio objetivo; al bajar Multi disparo a 0 sale 1. *Prueba: `LaserMultishotAlwaysFiresSeparatedBeamsAndReusesVisualsAfterUpgradeChanges`.*
- **AC-13** (regla 6, ondas del Pulso · Logic): **GIVEN** el Pulso con Multi disparo, **WHEN** se dispara la secuencia, **THEN** cada onda espera a que termine la anterior más el retardo, cada onda golpea a cada enemigo una sola vez y el intervalo normal empieza al terminar la secuencia. *Prueba: `EmpMultishotWaitsBetweenWavesAndCryoExtendsEachFreeze`.*
- **AC-14** (regla 6, Balas explosivas y fórmula 5 · Logic): **GIVEN** Balas explosivas nivel 1 y un tick de 10 de daño que golpea a 2 enemigos, **WHEN** el tick se cierra, **THEN** hay una sola explosión de radio 3 en el primer impacto que daña con 10 a cada enemigo vivo dentro, incluidos los golpeados, y sus víctimas no generan otra; con nivel 0 no hay explosión. *Prueba: `EachTickHasExactlyOneExplosionAndNeverRecurses`.*
- **AC-15** (fórmula 5, límite de daño 0 · Logic): **GIVEN** Balas explosivas y un ataque de 0 de daño del Láser o de la Criogénica, **WHEN** el tick se cierra, **THEN** la explosión hace 0; **GIVEN** el Pulso sin Láser activo, **THEN** la explosión usa el daño del jugador. *Pruebas: `ZeroDamageAttacksCannotGainPlayerDamageThroughExplosion`, `EmpUsesPlayerDamageForExplosionsUntilLaserIsActive`.*
- **AC-16** (regla 6, Cadena de impacto y fórmula 6, con el tope · Logic): **GIVEN** Cadena de impacto nivel 3 y 25 enemigos a 2,5 o menos unos de otros, **WHEN** un tick golpea a uno, **THEN** la cadena empuja a un máximo de 19 enemigos más, cada vez al más cercano aún no empujado, con la fuerza reducida un 5 % por salto desde 8,6; con nivel 6 o más el máximo es 32 saltos. *Prueba parcial: `ExplosionAndKnockbackDependOnlyOnTheirOwnUpgradeAndCarryCryoSlow` (la cadena funciona sin Balas explosivas).*
- **AC-17** (regla 7 y fórmula 3 · Logic): **GIVEN** el Láser y la Criogénica activos y tres enemigos (normal, ralentizado y congelado), **WHEN** el Láser impacta con 10 de daño, **THEN** reciben 10, 12,5 y 15, y el normal queda ralentizado; el congelado conserva su congelamiento. *Prueba: `LaserCarriesFrostAndUsesStrongestExistingControlBonus`.*
- **AC-18** (regla 7, Pulso + Láser · Logic): **GIVEN** el Pulso y el Láser activos (Láser de 15 de daño), **WHEN** la onda alcanza a un enemigo, **THEN** recibe 7,5 de daño directo; sin el Láser, 0. *Prueba parcial: `EmpUsesPlayerDamageForExplosionsUntilLaserIsActive` (con 12 de daño del Láser, 6).*
- **AC-19** (regla 7 y fórmula 4 · Logic): **GIVEN** el Pulso y la Criogénica activos, **WHEN** la onda congela a un enemigo, **THEN** el congelamiento dura 3 s; sin la Criogénica, 2 s. *Prueba: `EmpMultishotWaitsBetweenWavesAndCryoExtendsEachFreeze`.*
- **AC-20** (regla 8 · Logic y UI): **GIVEN** un equipo sin progreso guardado, **WHEN** el jugador sube por primera vez de 0 a 1 una mejora que es requisito y desbloquea el Láser, **THEN** los contadores «pieza nueva» y «sobrecarga nueva» de la partida son 1 y la pantalla de fin los muestra; al reiniciar el juego el panel Colección muestra el Láser descubierto y el nivel máximo de cada mejora; tras borrar el progreso todo vuelve a «?». *Falta prueba.*
- **AC-21** (regla 8, migración · Logic): **GIVEN** claves guardadas `SynergyDiscovery_*`, **WHEN** se lee el progreso por primera vez, **THEN** se copian al formato `OverrideDiscovery_*` con el mayor de los dos valores por mejora y se borran las claves antiguas. *Falta prueba.*
- **AC-22** (regla 9 · UI): **GIVEN** el juego en español y en inglés, **WHEN** se revisan los textos que ve el jugador (descripciones de sobrecargas y de mejoras, paneles, anuncios y tutorial), **THEN** ninguno menciona las combinaciones con mejoras premium ni entre sobrecargas. *Revisión manual con capturas en ambos idiomas.*
- **AC-23** (regla 10, panel del HUD · UI): **GIVEN** una partida nueva con el Láser descubierto en otra partida y ninguna sobrecarga activa, **WHEN** se abre el HUD, **THEN** los contadores de nivel están en 0, el icono del Láser aparece atenuado y las no descubiertas muestran «?»; **WHEN** el Láser se activa, **THEN** su icono pasa a completo. *Capturas en ambos idiomas.*
- **AC-24** (regla 10, panel Colección · UI): **GIVEN** el panel Colección del menú principal y de la pantalla de fin, **WHEN** se descubre una sobrecarga, **THEN** su resultado se revela y brilla y los niveles muestran el máximo histórico. *Capturas en ambos idiomas.*

### Casos límite y otros sistemas

- **AC-25** (caso límite del tick · Logic): **GIVEN** un jugador que consigue Balas explosivas durante un tick, **WHEN** ese tick se cierra, **THEN** no explota y el tick siguiente sí. *Prueba: `EachTickHasExactlyOneExplosionAndNeverRecurses`.*
- **AC-26** (caso límite de configuración · Logic): **GIVEN** una sobrecarga sin prefab de efecto, **WHEN** el jugador cumple sus requisitos, **THEN** no se activa, la consola avisa y el juego sigue; las demás sobrecargas no se ven afectadas. *Falta prueba.*
- **AC-27** (interacción con Daño, vida y muerte · Integration): **GIVEN** una horda, **WHEN** un impacto de sobrecarga mata a un enemigo, **THEN** suelta su experiencia y sus monedas como cualquier otra muerte. *Falta prueba.*
- **AC-28** (rendimiento · Performance): **GIVEN** las tres sobrecargas activas, Multi disparo, Balas explosivas y Cadena de impacto en nivel 6 y una horda de 450 enemigos en la build de WebGL, **WHEN** se juega un minuto, **THEN** el juego mantiene 60 FPS y las sobrecargas no producen asignaciones de memoria por frame. *Pendiente de medir: aún no hay presupuesto de draw calls ni de memoria (plan de adopción, 4.5).*

### Huecos encontrados

- Sin prueba automática, aunque son verificables: AC-1, 2, 3, 5, 8, 20, 21, 26 y 27, y los números de AC-16.
- Con prueba parcial: AC-6, 7, 9, 16 y 18.
- Solo con captura o revisión manual (tipo UI): AC-4, 22, 23 y 24.
- Sin criterio de límite: `freezeDuration` = 0 (fórmula 4) y daño 0 del Pulso sin Láser (fórmula 3).
- AC-28 no se puede cerrar hasta que existan los presupuestos de rendimiento.
- No había criterios previos que reescribir.

## Open Questions (Preguntas abiertas)

Salieron de contrastar la página original con el código y los assets el 2026-11-21. Responsable y fecha objetivo: por asignar hasta que el equipo las reparta.

1. **Requisito del Rayo Láser.** La página pide Rango de ataque y Velocidad de ataque (nivel 5, estimado); el asset de producción (`Override_LaserBeam_Production`) pide Rango de ataque y Daño (nivel 2). Este documento sigue el asset. ¿Cuál es el diseño correcto?
2. **Nivel requerido y duración de la partida.** La página estima nivel 5 en las dos mejoras de cada sobrecarga y un corte a 20 minutos, con 3 o 4 sobrecargas por partida. Los assets de producción piden nivel 2, la partida configurada dura 5 minutos (`matchDurationMinutes: 5`, igual en `develop` y en `demo`) y solo existen 3 sobrecargas, así que en una partida corta se consiguen todas. ¿Se actualiza la página a este balance o los niveles vuelven a subir para una versión larga?
3. **Nombres.** La página dice «evoluciones» y «sinergias»; el juego dice «sobrecargas» (*overrides*). La mejora que la página llama «Knockback» se llama «Cadena de impacto» en el juego. Hay que actualizar los nombres en Confluence y en las historias MAN-149 a MAN-152 de Jira.
4. **Registro antes de activar.** El juego marca una sobrecarga como descubierta antes de intentar activarla: si falta el prefab o no implementa el comportamiento, queda descubierta sin haberse activado y el intento se repite con cada mejora posterior. ¿Debe registrarse solo si la activación tiene éxito? Es un cambio de código y una decisión sobre qué cuenta como descubrir.
5. **Topes superiores.** Los multiplicadores de daño del Láser (contra ralentizados y congelados) y la fracción del Pulso (`empDamageMultiplier`) solo tienen mínimo, y el daño de las configuraciones (`damage`, `damagePerTick`) no tiene ningún límite en los datos (el código lo recorta a 0 por abajo); la fuerza de Cadena de impacto crece sin límite (≈ 805 con nivel 999). Opciones: (a) añadir máximos en las configuraciones y un tope a la fuerza; (b) limitar el nivel de las mejoras premium; (c) dejarlo, porque los niveles de una partida real son bajos.
6. **Niveles que dejan de aportar.** Para las sobrecargas, Multi disparo no da emisiones extra desde el nivel 6, Balas explosivas no cambia radio ni daño desde el nivel 1 y Cadena de impacto no añade saltos desde el nivel 6 (siguen mejorando el disparo normal). ¿Es intencionado o deben escalar?
7. **Congelamiento casi permanente.** Con Pulso, Criogénica y Multi disparo de nivel 3 o más, estimo (con los valores de producción, sin medirlo) que los enemigos dentro del radio 15 quedan congelados el 70-80 % del tiempo: 5 ondas de 0,8 s con pausas de 0,15 s (4,6 s) más un intervalo de 5 s, y 2 o 3 s de congelamiento por onda. ¿Es el efecto buscado? Hay que comprobarlo jugando.
8. **El Láser destruye edificios.** Cada tick del barrido destruye los edificios que toca, y los edificios dan monedas: el jugador puede conseguir monedas de edificios sin acercarse a ellos. La página dice que el rayo «causa daño perforante a todo lo que toque». ¿Es intencionado para la economía?
9. **Futuras sobrecargas.** La página propone Pisotón, Rastro de pinchos y Bomba magnética como «posibles futuros overrides», sin requisitos ni combinaciones definidos. ¿Entran en el alcance (Alpha o Full Vision)? Si entran, cada una necesita requisitos, efecto y combinaciones antes de implementarse.
