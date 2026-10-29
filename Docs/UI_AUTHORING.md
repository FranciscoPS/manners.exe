# UI editable e idiomas

La interfaz conserva la identidad actual: placas inclinadas, marcos, franjas, fuente y colores existentes. Las escenas y prefabs contienen los objetos visuales reales. En Play se activan y animan esos objetos o se instancian sus prefabs; no se construyen paneles, textos, cámaras de cofre ni rayos de UI mediante código.

## Cambiar un aviso sin escribir código

1. Abre **Tools → Manners → UI → Editar → Aviso de cofre**.
2. Selecciona el texto del aviso en la jerarquía del prefab.
3. En **LocalizedText → Content**, cambia **English** y **Spanish**. **Inspector Preview** permite comprobar cada idioma.
4. Ajusta el **RectTransform** y **TextMeshProUGUI**: fuente, tamaño máximo/mínimo, márgenes, alineación y auto-size. La placa es una **Image** real: puedes sustituir su sprite por uno de tu spritesheet.
5. Ajusta duración y pulso en **ChestAnnouncement**. Guarda el prefab y prueba ambos idiomas.

El aviso usa una caja fija con márgenes y auto-size; el código no recalcula su tamaño a partir del texto. Los menús de **Editar** también abren cinemática, idioma, gráficos, números flotantes y estilo.

En la cinemática, el tamaño y posición del cofre se ajustan en la **RawImage**; el FOV y giro, en su **Camera** real; el margen de encuadre y calidad, en **ChestShowcase**. El color del velo y del flash está en sus **Images**, y la opacidad del velo en **ChestOpeningSequence**. Los tiempos, sonido, sacudidas y tecla de salto permanecen en el config de partida correspondiente.

## Dónde editar cada cosa

| Elemento | Objeto o prefab |
| --- | --- |
| Aviso de cofre y tiempo extra | `Assets/Prefabs/UI/Notifications/` |
| Cinemática, texto, aura y cámara del cofre | `Assets/Prefabs/UI/Chest/ChestOpeningSequence.prefab` |
| Panel de gráficos compartido | `Assets/Prefabs/UI/Settings/GraphicsSettingsPanel.prefab` |
| Selector de idioma | `Assets/Prefabs/UI/Settings/LanguageSelector.prefab` |
| Aviso e iconos de sobrecarga | `Assets/Prefabs/UI/Overrides/` |
| HUD de sobrecargas y su rótulo | `Assets/Prefabs/UI/OverrideHudPanel.prefab → Title` |
| Niveles de cada mejora en el HUD de sobrecargas | `OverrideHudPanel.prefab → EmptySquare1/2 → LevelText` (texto `Lv. {0}/{1}` / `Nv. {0}/{1}` en `OverrideHintRowUI`) |
| Barra de vida, cara de la I.A. y lectura `HP`/`VIDA` | `LEVEL 1.unity → Canvas/HealthBarContainer` (`StyleFrame`, `HealthBarBackground/HealthBarFill`, `StyleValue`, `StyleFace`); formato y latido de vida baja en `HealthBarUI`; gestos en `MannersFaceUI` |
| Relleno al mantener pulsada una tarjeta | `LEVEL 1.unity → Canvas/LevelUpPanel → … → UpgradeButton1-3 → FillOverlay`; duración y color premium en `HoldToSelectButton` |
| Tooltip | `Assets/Prefabs/UI/Overlays/HoverTooltip.prefab` |
| Daño y cantidades flotantes | `Assets/Prefabs/UI/FloatingText.prefab`; colores y pool en `FloatingTextManager` de la escena; el rojo semitransparente del daño sale de `UIStyle → Damage Number` al aplicar el estilo |
| Foil, rayos y partículas de cofre | `Assets/Prefabs/UI/Effects/`; materiales en `Assets/Materials/UI/Authored/` |
| Menú principal | `MainMenu.unity → Canvas` |
| HUD, pausa, tarjetas, tienda y Game Over | `LEVEL 1.unity → Canvas` |
| Diálogo del tutorial | `LEVEL 1.unity → TutorialCanvas`; referencias y los 41 pasos bilingües en `TutorialManager` |
| Sprites del estilo actual | `Assets/Sprites/UI/Style/` |
| Área compartida de composición | `Assets/Prefabs/UI/Layout/ContentFrame.prefab` |

Las referencias a textos, imágenes, grupos y botones están serializadas. Los controladores ya no buscan hijos por nombre para reemplazar tus referencias visuales al iniciar. Cada tarjeta conserva sus componentes de selección, relleno, foil, aura y feedback. Los números flotantes reutilizan su pool y tienen un canvas real en las escenas.

Para usar un spritesheet propio, impórtalo como **Sprite (2D and UI)**, configura **Multiple** y corta sus regiones en **Sprite Editor**. Arrastra la región al **Source Image** de la Image correspondiente. Usa **Sliced** con bordes si la placa debe estirarse. Cambia materiales, forma y tipografía directamente en los componentes del prefab. Las instancias de gráficos y de idioma son compartidas; una modificación del prefab llega al menú y a pausa. Las posiciones específicas de cada pantalla se ajustan en su instancia de escena.

**Editar → Estilo y sprites** selecciona el recurso común de la identidad. La herramienta antigua **Aplicar todo el estilo** vuelve a escribir propiedades visuales: úsala cuando quieras reaplicar la identidad global, no después de hacer ajustes manuales que quieras conservar.

## Inglés y español

El primer inicio usa inglés. El selector está en **Opciones** y en **Pausa**, junto a los controles existentes. La elección se guarda en `PlayerPrefs` con la clave `GameLanguage` y permanece al cambiar de escena o reiniciar el juego.

Los textos fijos usan **LocalizedText** junto al TMP. Los textos que incluyen números o cambian de modo tienen pares **English/Spanish** en su controlador: `LevelUpManager`, `UpgradeButton`, `GameOverUI`, `GraphicsSettingsMenu`, `AudioSettingsMenu`, `CurrencyUI`, `ExperienceUI`, `PlayerStatsHUD`, `HealthBarUI` (lectura `HP {0}/{1}` / `VIDA {0}/{1}`), `OverrideHintRowUI` (niveles `Lv.`/`Nv.`) y `ChestOpeningSequence`. Los nombres y descripciones de mejoras, objetos de cofre y sobrecargas se editan en sus assets. El tutorial tiene sus pasos y botones en el componente `TutorialManager`; ya no carga un JSON separado.

Cambiar idioma actualiza el contenido mediante eventos. No reinicia un paso del tutorial, no repite descubrimientos ni vuelve a consultar la clasificación por cambiar los textos. `OverrideData.PersistentId` mantiene la identidad de los desbloqueos separada del idioma y del nombre del archivo.

## Anchors, centrado y proporciones de pantalla

Los menús y ventanas conservan sus raíces de pantalla completa y contienen un **ContentFrame** real. Su **AspectRatioFitter**, en **Fit In Parent** con proporción **16:9**, mantiene juntos títulos, subtítulos y controles. Los **CanvasScaler** usan **Scale With Screen Size**, referencia **1920×1080** y **Expand**. La composición conserva sus proporciones en 4:3, 16:10, 16:9, ultrawide y 32:9; los fondos y velos cubren toda la pantalla.

Para ajustar una pantalla, abre su panel y entra en **ContentFrame**. Cambia los RectTransform de sus hijos desde Inspector; el fitter controla el tamaño del marco. Los encabezados y sus subtítulos comparten el eje central. Los márgenes del TMP del subtítulo son simétricos; el chevron sigue siendo una Image independiente a la izquierda. El cursor de consola sigue el borde real del último carácter al cambiar texto, idioma o tamaño, también durante la edición. Su separación y parpadeo se ajustan en **UITextCursor**.

**Tools → Manners → UI → Editar → Composición y anchors** abre el prefab compartido. En **UIScreenIntro → Blocks**, los títulos y controles reales están asignados en el orden de su animación. Puedes ajustar esa lista desde Inspector; el `ContentFrame` y los objetos con `AspectRatioFitter` deben quedar fuera de ella para que la entrada no compita con el ajuste de proporción.

El HUD se ancla al viewport: minimapa y estadísticas arriba a la izquierda, monedas arriba a la derecha y temporizador arriba al centro. Estos elementos conservan su distancia al borde en unidades de Canvas. Evita combinar un anchor porcentual cercano al borde derecho con un gran offset negativo para simular una columna izquierda: esa columna se desplaza al ensanchar la pantalla.

Al crear un panel nuevo, conserva su fondo de pantalla completa y coloca dentro una instancia de `ContentFrame.prefab` con los controles. Las referencias existentes de los controladores apuntan a los componentes reales; las herramientas de Editor resuelven los contenidos de ese marco mediante `UIEditorHierarchy`.

## Configuraciones

| Carpeta | Uso |
| --- | --- |
| `Assets/Configurations/Production/` | Datos de las escenas de producción; archivos con sufijo `_Production` |
| `Assets/Configurations/Production/Resources/` | Recursos globales cargados por el juego |
| `Assets/Configurations/Production/Resources/UI/` | Estilo y registro de prefabs compartidos de UI |
| `Assets/Configurations/Sandbox/` | Copias de pruebas; archivos con sufijo `_Sandbox` |

**Tools → Manners → Configuraciones** selecciona cada carpeta. Los ajustes de experimentación se conectan desde `SandboxTuning` y se promueven exclusivamente con **Tools → Manners → Sandbox → 4**. Las herramientas usan `GameAssetPaths` para emparejar los archivos por nombre base. Un cambio de carpeta conserva su `.meta` y GUID. Los archivos del Mapa 2 permanecen en sus ubicaciones anteriores mientras ese mapa está pausado.

La estructura compartida de UI se edita en prefabs; no hace falta duplicarla entre Production y Sandbox. Los parámetros de partida y de cinemática que ya pertenecían a configuraciones de balance mantienen sus copias independientes.

## Corregir el HUD de partida

**Tools → Manners → UI → Corregir HUD: vida, niveles de sobrecargas y daño** aplica solo la parte del estilo que toca el HUD, sin reescribir el resto de la interfaz ni deshacer ajustes manuales de otras pantallas. En `OverrideHudPanel.prefab` y en las escenas `LEVEL 1` y `Sandbox` deja:

- La barra de vida a escala 1, de 520×66 unidades, anclada arriba a la izquierda con su borde en 112 y centrada en −112; marco con filo rojo (`danger`), relleno verde→naranja→rojo según la vida (`UIStyle.HealthColor`), lectura `HP actual/máxima` centrada (`StyleValue`) y la cara de la I.A. de 104 unidades montada sobre el extremo izquierdo (`StyleFace`). Con menos del 30 % de vida el relleno late hacia el rojo de alerta (`HealthBarUI → Vida baja`).
- El panel de sobrecargas activo y, si está bajo la barra de vida, 12 unidades por debajo de la cara (se mueve su `AnchoredPosition.y`). Sus textos `LevelText` quedan en 28×16 unidades, auto-size 9–12, sin elipsis, para que `Lv. 1/3` siempre se vea bajo cada mejora requerida.
- `FloatingTextManager → Damage Color` igual a `UIStyle → Damage Number` (rojo de alerta con alfa 0.77); experiencia en cian, monedas en oro y diamantes en lila.
- El relleno de mantener pulsado de cada tarjeta con alfa 0.78 (cian normal, lila en especiales).

La herramienta pide guardar la escena abierta, escribe `Logs/ui-hud-report.txt` y vuelve a abrir la escena en la que estabas. Los valores de tamaño y posición están en `UIStyleApplierScreens` (constantes `HealthBar*`); los colores, en `UIStyle_Production`. **Aplicar todo el estilo** (paso 0 o 2) incluye estos mismos ajustes.

## Comprobar cambios

1. **Tools → Manners → UI → Validar componentes e idiomas** revisa referencias, scripts ausentes, traducciones, destinos del tutorial y recursos de las cuatro escenas soportadas.
2. **Tools → Manners → UI → Capturar inglés y español** genera 336 vistas y auditoría de texto en `Logs/UIReview/`: 1920×1080, 1280×720, 2560×1080, 1600×1200, 1920×1200, 3440×1440 y 3840×1080. Los nombres incluyen idioma, resolución completa, superficie y panel.
3. En **Window → General → Test Runner → EditMode**, ejecuta `UIAuthoringTests` para comprobar las referencias y el flujo real de idioma, gráficos, pausa, daño y cofre. `UITextCursorTests` alterna los idiomas con los cursores reales; `UIResponsiveLayoutTests` comprueba centrado, límites, márgenes del HUD, posición y solapes del selector y que el título de sobrecargas muestre todas sus letras en cinco proporciones. `UIScreenIntroAuthoringTests` verifica los bloques de animación y que el marco no reciba tweens. Conserva también las pruebas de sobrecargas y gráficos existentes.

Las capturas son vistas de edición con ejemplos de datos. Las animaciones, el relleno y la cámara del cofre se comprueban durante Play. El Mapa 2 queda excluido de escenas, validación y pruebas.

La auditoría utiliza cajas de texto y puede señalar la marca diagonal de «Próximamente» del selector de mapas como un cruce con el título de la tarjeta. También detecta el aviso de tienda del HUD detrás de la placa opaca de Gráficos. Esos cruces se revisan en los PNG: deben distinguirse de un texto recortado o de un control visible que tape otro control.

Para añadir otra pantalla, crea su jerarquía en escena o prefab, asigna referencias en Inspector, añade `LocalizedText` a cada texto fijo y usa `LocalizedString` serializado para formatos dinámicos. Reutiliza el estilo actual, el bus de eventos y `UpdateManager` para animaciones continuas. No añadas generadores visuales ni controles duplicados con valores que ya existan en TMP, Image o RectTransform.

## Verificación de la corrección del HUD (6 de octubre de 2026, noche)

Comprobado en una copia del proyecto con Unity en modo batch (la herramienta **Corregir HUD** y una prueba de Play Mode en `LEVEL 1` que sube una mejora, baja la vida, abre la subida de nivel y mantiene pulsada una tarjeta normal):

| Comprobación | Resultado |
| --- | --- |
| Compilación tras los cambios | Sin errores de C# |
| Relleno de mantener pulsado en tarjeta normal | Visible: cian con alfa 0.78 y 0.40 s de avance a los 0.45 s de pulsar; antes el color era (0, 0, 0, 0) porque `SetPremiumStyle` corría antes de `Awake` en la tarjeta inactiva |
| Niveles del HUD de sobrecargas | `Nv. 0/3` → `Nv. 1/3` tras una mejora, 28 vértices dibujados; antes el texto tenía elipsis y 11 pt fijos en una caja de 12 unidades y no se dibujaba |
| Barra de vida | 520×66 unidades a escala 1, cara de 104, lectura `VIDA 250/250` y `VIDA 55/250` con latido rojo al 22 % de vida |
| Panel de sobrecargas | Reactivado y bajado 29 unidades, 12 por debajo de la cara |
| Números de daño | (1, 0.35, 0.27, 0.77): rojo de alerta semitransparente |
| Auditoría de solapes | 0 avisos en las cuatro capturas (HUD, vida baja, sobrecargas y tarjetas) |

Las capturas quedaron en `Logs/UIReview/hud-2026-10-06-*.png`.

## Verificación de la entrega anterior

Resultados del 6 de octubre de 2026, manteniendo la UI actual:

| Comprobación | Resultado |
| --- | --- |
| Compilación de código de juego y editor | Sin errores ni advertencias de C# |
| Referencias, componentes e idiomas | 4 escenas, 11 245 componentes, 608 etiquetas bilingües; 0 errores |
| Assets reorganizados | 73 GUID de configuraciones y 3 GUID de scripts conservados |
| Vistas en inglés y español | 336 capturas en siete resoluciones, de 4:3 a 32:9; 0 textos truncados, desbordamientos o textos fuera de pantalla |
| Regresión inicial | 71 pruebas aprobadas, 0 fallos |
| Revisión de centrado, anchors y animación | 16 pruebas de UI aprobadas, más 2 pruebas del rótulo bilingüe del HUD; 0 fallos |
| Recorrido en Play | Opciones, idioma persistente entre escenas, pausa, gráficos, cierre del tutorial, aviso del cofre, pool de daño y cinemática con cámara 3D |

La regresión inicial ejecutó `UIAuthoringTests`, `OverrideCombinationTests`, `GraphicsBudgetTests`, `PerformanceRegressionTests`, `WebBuildRegressionTests` y `ToonEnvironmentTests`. La revisión posterior ejecutó `UIAuthoringTests`, `UIResponsiveLayoutTests`, `UITextCursorTests` y `UIScreenIntroAuthoringTests`, incluyendo el recorrido en Play después de cambiar la jerarquía. Las capturas y los informes locales están en `Logs/UIReview/`; `play-chest.png` muestra el cofre durante Play. Los 28 avisos de cruce corresponden al sello de mapas y al aviso de tienda detrás de Gráficos, en ambos idiomas y las siete resoluciones. No se ejecutó una build ni se cambió la versión de Unity.
