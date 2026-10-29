# Combinaciones de mejoras y sobrecargas

Las combinaciones se activan automáticamente al poseer la sobrecarga y la mejora especial. No requieren recetas ni compras, y no dependen de probabilidades: basta con tener la mejora al nivel 1 o más para que el efecto combinado ocurra **siempre**, en cada ciclo de la sobrecarga. Los proyectiles conservan su comportamiento probabilístico normal; solo las sobrecargas (láser, EMP y campo criogénico) usan la versión garantizada.

| Mejora | Láser | EMP | Campo criogénico |
| --- | --- | --- | --- |
| Multi disparo | Cada barrido dispara todos sus rayos, cada uno hacia su propio objetivo | Cada ciclo emite todas sus ondas, separadas por 0.15 s después de cada expansión | Cada tick principal va seguido de sus ticks extra, con 0.15 s entre ellos |
| Balas explosivas | Cada tick de cada rayo explota | Cada onda explota, incluidas sus víctimas del contagio | Cada tick explota, incluidos los ecos |
| Cadena de impacto | Cada tick empuja a los alcanzados y encadena a vecinos | Cada onda empuja a los alcanzados por la onda o el contagio | Cada tick empuja a los enemigos alcanzados |

El número de emisiones es `1 + balas extra de Multi disparo`, limitado por `maxExtraBeams` / `maxExtraPulses` de cada config (valor inicial 2, es decir, tres emisiones; máximo 8 extras). El intervalo de cada sobrecarga no cambia: el láser sigue disparando cada `interval` segundos, pero siempre con todos sus rayos.

Cada rayo del láser elige su objetivo por separado, con la misma prioridad que el rayo principal: el enemigo más cercano dentro de `range`; si no hay, un edificio al azar dentro de `range`; si tampoco hay, un punto al azar del suelo. Un objetivo se descarta si ya lo tomó otro rayo o si queda a menos de `minBeamSeparationAngle` grados (40 por defecto) de la dirección de un rayo ya apuntado, así que los rayos salen en direcciones distintas. Cada rayo barre desde su objetivo hasta `extendDistance` más allá, con su propio daño por tick y su propia explosión y empuje.

Cada tick u onda produce **una explosión**, centrada en el primer enemigo alcanzado, sin importar cuántos enemigos golpee. La explosión añade el daño del tick en el radio de la mejora; un EMP sin daño propio usa el daño actual del proyectil. Las explosiones pueden transmitir empuje y, con el campo activo, frío.

Los impactos adicionales de una explosión o cadena no generan más explosiones, cadenas, láseres ni repeticiones. El empuje no golpea dos veces al mismo enemigo durante el mismo tick u onda y tiene un máximo de 32 saltos. Las repeticiones de Multi disparo no generan nuevas repeticiones. Cada rayo y cada eco sí conserva su propia explosión y su propio empuje.

| Sobrecargas activas | Resultado |
| --- | --- |
| Láser + campo criogénico | El láser transmite ralentización. Contra enemigos que ya estaban ralentizados inflige ×1.25 de daño. |
| Láser + EMP | El láser inflige ×1.5 contra enemigos completamente congelados. Cada enemigo alcanzado por una onda EMP recibe además el 50% del daño base de un tick láser. |
| EMP + campo criogénico | La congelación del EMP dura ×1.5; la ralentización del campo no sustituye un congelamiento más fuerte. |
| Las tres | Se combinan los comportamientos anteriores; el láser usa el mayor multiplicador aplicable, sin multiplicarlos entre sí. |

La resistencia al control del enemigo sigue determinando si realmente queda congelado. Los modificadores de daño consultan el estado vigente antes del impacto; el primer golpe que aplica frío no recibe retroactivamente su bonificación.

## Ajustes

Los assets normales (`Assets/Configurations/Production/Overrides/`, sufijo `_Production`) y sus copias de sandbox (`Assets/Configurations/Sandbox/Overrides/`, sufijo `_Sandbox`) incluyen los valores iniciales. En `LaserBeamConfig`: `maxExtraBeams`, `minBeamSeparationAngle`, `empDamageMultiplier`. En `EmpPulseConfig`: `maxExtraPulses`, `repeatDelay`, `cryoFreezeDurationMultiplier`, `laserDamageMultiplier`. En `CryoFieldConfig`: `maxExtraPulses`, `repeatDelay`, `laserDamageMultiplier`.

Subir `maxExtraBeams` o `maxExtraPulses` por encima de 2 hace que el nivel de Multi disparo sí cuente: con 8, el nivel 1 da 4 emisiones (las mismas que balas), el nivel 2 da 7 y del nivel 3 en adelante 9. EMP y campo reinician su intervalo normal al terminar la secuencia de ecos. Los anillos naranjas indican explosiones y los anillos fríos indican ticks criogénicos extra; el EMP muestra una onda visual por onda con efecto real.

## Verificación

Ejecutar `OverrideCombinationTests` en el Test Runner de Unity (EditMode). La suite entra en Play Mode con una escena vacía y fixtures aislados para comprobar el ciclo de vida real de los enemigos; las mejoras de prueba tienen 0% de probabilidad para demostrar que las combinaciones no dependen de ella. Para revisar sensación y balance, activar las tres sobrecargas en el sandbox y probar cada mejora especial: primero por separado, luego juntas. Comprobar que salen todos los rayos en cada barrido, en direcciones distintas, los intervalos entre ondas, las explosiones por tick y el empuje con enemigos agrupados; desactivar sobrecargas durante una secuencia debe retirar efectos y visuales pendientes.
