La desaparición de enemigos se reprodujo y corrigió. La secuencia musical tiene una ruta específica para WebGL que evita superponer pistas programadas; su verificación audible en navegador queda pendiente.

Inspección realizada con Unity 6000.3.5f2 y URP 17.3.0:

- `EnemyController` gira los visuales mediante `Quaternion.LookRotation` y `Slerp`. No hay una condición que oculte enemigos por orientación.
- Los 12 prefabs de enemigos usan MeshRenderer, materiales URP/Lit opacos y escalas positivas. No son sprites ni SkinnedMeshRenderer. Sus objetos no están marcados como estáticos.
- WebGL usa por defecto la calidad Mobile (`lodBias = 1`); el Editor/Windows usa PC (`lodBias = 2`). Mobile utiliza Forward y PC utiliza Deferred. Las máscaras del renderer incluyen la capa de enemigos. No se encontró una exclusión direccional en los shaders, las máscaras o `WebGLOptimizer`.
- Los cuatro prefabs base tenían un LODGroup con un único nivel seguido de descarte completo, tamaño 1 y umbrales aproximados del 4,5–5,2 % de pantalla. Unity midió 3,33 unidades para Basic Enemy y 2,43 para Fast Enemy al recalcular sus bounds. Varias variantes conservaban referencias LOD del modelo sustituido.

Con la cámara inclinada, los enemigos que se aproximan desde arriba quedan más lejos de ella. El descarte demasiado cercano, junto con el distinto `lodBias`, explica que pareciera depender de hacia dónde miraban. En renders con la calidad Mobile, Basic Enemy produjo 0 píxeles desde arriba y en diagonales superiores; desactivando únicamente su LOD produjo 152, 212 y 208 píxeles en esas mismas posiciones. El comportamiento de los umbrales y su dependencia del tamaño en pantalla están documentados por [Unity: LOD Group](https://docs.unity3d.com/6000.3/Documentation/Manual/class-LODGroup.html).

Se eliminaron los LODGroup de Basic Enemy, Fast Enemy, L2BaseEnnemy y L2FastEnemy, y los overrides obsoletos de BEnemy2, BEnemy3, FEnemy2 y FEnemy3. Las 12 variantes heredan la corrección. El descarte normal fuera del campo de visión continúa disponible. Ahora se dibujan los enemigos que antes desaparecían prematuramente; esto puede aumentar el trabajo gráfico visible y debe observarse con las oleadas grandes.

En audio, `MusicManager` utiliza dos fuentes y programación DSP en plataformas nativas. La ruta anterior también se ejecutaba en WebGL y evaluaba `isPlaying`/`time` inmediatamente después de solicitar la reproducción. Los clips de Ciudad tienen la precarga desactivada. La carga/decodificación asíncrona y las diferencias de programación del navegador son una causa plausible del solapamiento reportado, aunque aquí no se reprodujo el fallo audible original. [Unity documenta que WebGL utiliza Web Audio, sus restricciones de autoplay y el posible remuestreo](https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-audio.html). También se consultaron [reportes de programación musical en WebGL](https://exceed7.com/introloop/advanced/q-and-a.html); son antecedentes, no una confirmación de un bug en esta versión concreta.

La nueva ruta web:

- Solicita los datos y espera `AudioDataLoadState.Loaded` antes de iniciar cada clip.
- Usa una sola fuente para intro, loops y puentes; cada sustitución detiene explícitamente el clip anterior. Mantiene el loop del navegador para las repeticiones.
- Cuenta duración con DSP y `AudioClip.length`, sin detectar vueltas mediante saltos de `AudioSource.time` ni programar pistas futuras con `PlayScheduled`.
- Precarga el siguiente loop y puente, respeta repeticiones y tiempos configurados y ajusta el reloj con `PauseMusic`/`ResumeMusic`.
- Conserva el fundido breve de overtime; su puente y loop comparten una sola fuente. Windows conserva la programación DSP anterior.

Las transiciones web se ejecutan al llegar el siguiente frame y no garantizan empalmes exactos a nivel de muestra. Una carga lenta o una pestaña suspendida pueden introducir una pausa entre pistas. Falta comprobar este comportamiento y escuchar los empalmes en los navegadores objetivo.

Validación completada:

- Siete pruebas EditMode aprobadas: intro → loops → puente, repeticiones, ausencia de intro/puente, pausa explícita, transición por tiempo de partida, overtime y prefabs heredados. Resultados en `Logs/web-regression-tests.xml`; pruebas en `Assets/Scripts/Editor/WebBuildRegressionTests.cs`.
- 384 renders aprobados: 12 variantes × 2 calidades × 8 direcciones × 2 casos (girar en el centro y aproximarse desde alrededor). Cero enemigos invisibles. Se giraron los mismos visuales configurados en EnemyController. Resultados en `Logs/web-rendering-checks.txt`.
- Los renders usan siluetas blancas para medir geometría y descarte independientemente de luces/texturas, ejecutadas en Direct3D 11 del Editor. No constituyen una prueba del driver WebGL del navegador.

Para repetir las pruebas, usar Unity Test Runner en EditMode filtrando `WebBuildRegressionTests`. La comprobación gráfica se ejecuta en un Editor separado con `-batchmode -executeMethod WebBuildRenderingChecks.Run -quit`, habilitando un dispositivo gráfico y omitiendo `-nographics`.

Comprobación final pendiente: instalar WebGL Build Support para Unity 6000.3.5f2 (esta máquina solo tiene Windows Standalone Support), generar una build nueva y abrirla en navegador. En nivel 1, comprobar aproximaciones desde las ocho direcciones, entrada desde el menú, reinicio del nivel, intro → loop1 → puente1 → loop2, cambios de pestaña y overtime. Verificar que solo se solapen pistas durante el fundido breve de overtime configurado.
