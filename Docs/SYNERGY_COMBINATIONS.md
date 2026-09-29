# Combinaciones de mejoras y sinergias

Las combinaciones se activan automáticamente al poseer sus mejoras y sinergias. No requieren nuevas recetas ni compras. Los proyectiles conservan su comportamiento; láser, EMP y campo criogénico consultan las probabilidades actuales de mejoras por separado, en su propio ciclo.

| Mejora | Láser | EMP | Campo criogénico |
| --- | --- | --- | --- |
| Multi disparo | Una tirada por barrido: rayo central y dos laterales a ±16° | Una tirada por ciclo: hasta tres ondas completas, separadas por 0.15 s después de cada expansión | Una tirada por tick principal: hasta tres ticks, con 0.15 s entre ellos |
| Explosivos | Una tirada por tick de cada rayo | Una tirada por onda, incluyendo sus víctimas del contagio | Una tirada por tick, incluidos los ecos |
| Cadena de impacto | Empuja a los alcanzados; puede encadenar a vecinos | Empuja a los alcanzados por onda/contagio | Empuja a los enemigos alcanzados por el tick |

Explosión y empuje se sortean por separado. Cada tick/onda puede producir **una explosión**, centrada en el primer enemigo alcanzado. No es una tirada por enemigo: una horda no convierte una probabilidad pequeña en explosiones garantizadas. La explosión añade el daño del tick en el radio de la mejora; un EMP sin daño propio usa el daño actual del proyectil. Las explosiones pueden transmitir empuje y, con el campo activo, frío.

Los impactos adicionales de una explosión o cadena no generan más explosiones, cadenas, láseres ni nuevas tiradas de multishot. El empuje no golpea dos veces al mismo enemigo durante el mismo tick/onda y tiene un máximo de 32 saltos. Las repeticiones de multishot no generan nuevas repeticiones. Cada rayo y cada eco sí conserva sus propias tiradas de explosión y empuje.

| Sinergias activas | Resultado |
| --- | --- |
| Láser + campo criogénico | El láser transmite ralentización. Contra enemigos que ya estaban ralentizados inflige ×1.25 de daño. |
| Láser + EMP | El láser inflige ×1.5 contra enemigos completamente congelados. Cada enemigo alcanzado por una onda EMP recibe además el 50% del daño base de un tick láser. |
| EMP + campo criogénico | La congelación del EMP dura ×1.5; la ralentización del campo no sustituye un congelamiento más fuerte. |
| Las tres | Se combinan los comportamientos anteriores; el láser usa el mayor multiplicador aplicable, sin multiplicarlos entre sí. |

La resistencia al control del enemigo sigue determinando si realmente queda congelado. Los modificadores de daño consultan el estado vigente antes del impacto; el primer golpe que aplica frío no recibe retroactivamente su bonificación.

## Ajustes

Los assets normales y sus copias de sandbox incluyen los valores iniciales. En `LaserBeamConfig`: `maxExtraBeams`, `multiShotSpreadAngle`, `empDamageMultiplier`. En `EmpPulseConfig`: `maxExtraPulses`, `repeatDelay`, `cryoFreezeDurationMultiplier`, `laserDamageMultiplier`. En `CryoFieldConfig`: `maxExtraPulses`, `repeatDelay`, `laserDamageMultiplier`.

El máximo predeterminado es dos emisiones extra (tres en total). Se puede subir por sinergia hasta ocho extras; no cambia el número de proyectiles del arma. El nivel de multishot sigue aumentando su probabilidad. EMP y campo reinician su intervalo normal al terminar la secuencia de ecos. Los anillos naranjas indican explosiones y los anillos fríos indican ticks criogénicos extra; el EMP muestra una onda visual por onda con efecto real.

## Verificación

Ejecutar `SynergyCombinationTests` en el Test Runner de Unity (EditMode). La suite entra en Play Mode con una escena vacía y fixtures aislados para comprobar el ciclo de vida real de los enemigos. Para revisar sensación y balance, activar las tres sinergias en sandbox y probar cada mejora especial: primero por separado, luego juntas. Comprobar el abanico, los intervalos entre ondas, las explosiones por tick y el empuje con enemigos agrupados; desactivar sinergias durante una secuencia debe retirar efectos y visuales pendientes.
