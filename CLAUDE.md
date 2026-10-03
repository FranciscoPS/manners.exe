# manners.exe · instrucciones para Claude

Juego bullet-heaven estilo Vampire Survivors en Unity 6000.3.5f2 (URP), con WebGL y escritorio como objetivo. La arquitectura, los patrones de diseño y las reglas de código del proyecto están en el README, que se carga aquí:

@README.md

## Cómo trabajar en este proyecto

Indicaciones permanentes del usuario; aplican a cada tarea:

- Antes de implementar, entender el contexto general del juego: patrones de diseño, optimización, cómo está estructurado y cómo funcionan los sistemas que toca el cambio. Nada de "hacer por hacer": integraciones limpias que encajen con la arquitectura existente, sin agregados sueltos.
- Todo lo que haya que hacer a mano en Unity (Inspector, escenas, prefabs, menús de `Tools`, configuraciones) se entrega al final como instrucciones claras, paso a paso.
- No escribir comentarios en el código C#. Los `[Tooltip]` y `[Header]` en español sí se usan: el balance se ajusta desde el Inspector.
- No crear ni actualizar documentación (README, `Docs/`, el manual publicado como Artifact) salvo que el usuario lo pida explícitamente en la conversación.
- Antes de construir un sistema desde cero, revisar las ramas locales (`git branch -a`): suele haber trabajo previo relacionado.

## Escenas y pruebas

- Escenas de producción: `MainMenu` y `Assets/Scenes/Final Levels/LEVEL 1/LEVEL 1.unity` (Mapa 1). `CityTest` es la versión anterior del Mapa 1.
- El Mapa 2 (`MilitaryBase`) está en pausa desde el 2 de octubre de 2026: todo el trabajo actual es solo sobre el Mapa 1. No tocar su escena, sus enemigos (prefabs `L2*`) ni sus assets, y no incluirlo en herramientas, validaciones, capturas ni pruebas hasta que el usuario lo reactive. Si un asset compartido con el Mapa 1 cambia, no hace falta ajustar el Mapa 2.
- Los sistemas nuevos y los experimentos de balance se prueban en `Assets/Scenes/Sandbox.unity`, con sus propias copias de assets en `Assets/Configurations/SANDBOX CONFIGURATIONS FOR TESTING/` y los ajustes en el componente `SandboxTuning`. No se experimenta editando los niveles que ya funcionan. El sandbox es un nivel duplicado con objetos reales de escena, no algo generado en runtime.
- Los valores del sandbox pasan a producción solo con `Tools > Manners > Sandbox > 4` (comparar y sincronizar). Nunca borrar y pegar assets a mano: cambian los GUID y se rompen referencias. Si la herramienta no lleva un cambio, se arregla la herramienta.
- Pruebas EditMode: `OverrideCombinationTests` (Window > General > Test Runner).

## Sobrecargas (overrides)

- Antes se llamaban "sinergias". En código, carpetas, assets y menús son overrides (`OverrideManager`, `OverrideData`, `Assets/Scripts/Overrides/`, `Tools > Manners > Overrides`); en la UI del juego, en español, "sobrecargas". No volver a usar synergy/sinergia.
- Cada sobrecarga son dos assets: `OverrideData` (identidad y requisitos: dos mejoras a cierto nivel) y un config que hereda de `OverrideEffectConfig` (`CryoFieldConfig`, `LaserBeamConfig`, `EmpPulseConfig`) con todos sus números y su visual. `OverrideManager` no conoce los tipos concretos (`ApplyTo` polimórfico). Cualquier sistema ajustable nuevo sigue la misma idea: todos sus valores y su visual en un solo ScriptableObject.
- Las combinaciones con las mejoras especiales (Multi disparo, Balas explosivas, Cadena de impacto) y entre sobrecargas se resuelven en `OverrideCombatResolver`. Siempre están activas si el jugador tiene la mejora, nunca por probabilidad, y son secretas: ningún texto que vea el jugador las menciona, ni en las descripciones de las sobrecargas ni en las de las mejoras.
- Láser con Multi disparo: cada rayo elige su propio objetivo con la misma prioridad (enemigo más cercano, luego un edificio al azar, luego un punto al azar), separados al menos `minBeamSeparationAngle` grados.
- Panel grande de sobrecargas (menú principal y Game Over, filas en modo `Collection`): muestra lo descubierto entre partidas. Panel del HUD en partida (modo `Hud`): guía de la partida actual; los contadores de nivel empiezan en 0, las sobrecargas ya descubiertas muestran siempre sus iconos con el resultado atenuado hasta que se activan, y lo no descubierto conserva el "?".

## UI

- Reutilizar el lenguaje visual existente (marcos, fuente pixel, "?", animaciones que ya existen) y cuidar el equilibrio del HUD; no agregar elementos al azar.
- Los botones y cajas de texto tienen tamaño fijo: al cambiar un texto, comprobar que quepa (fuente, tamaño, auto-size).

## Assets y serialización

- Renombrar o mover scripts y assets siempre junto con su `.meta`. Al renombrar un campo serializado, añadir `[FormerlySerializedAs("nombreAnterior")]`.
- Evitar editar escenas y prefabs como texto con Unity abierto: Unity puede sobrescribir el cambio al guardar.
