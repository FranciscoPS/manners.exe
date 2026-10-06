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
| Tooltip | `Assets/Prefabs/UI/Overlays/HoverTooltip.prefab` |
| Daño y cantidades flotantes | `Assets/Prefabs/UI/FloatingText.prefab`; colores y pool en `FloatingTextManager` de la escena |
| Foil, rayos y partículas de cofre | `Assets/Prefabs/UI/Effects/`; materiales en `Assets/Materials/UI/Authored/` |
| Menú principal | `MainMenu.unity → Canvas` |
| HUD, pausa, tarjetas, tienda y Game Over | `LEVEL 1.unity → Canvas` |
| Diálogo del tutorial | `LEVEL 1.unity → TutorialCanvas`; referencias y los 41 pasos bilingües en `TutorialManager` |
| Sprites del estilo actual | `Assets/Sprites/UI/Style/` |

Las referencias a textos, imágenes, grupos y botones están serializadas. Los controladores ya no buscan hijos por nombre para reemplazar tus referencias visuales al iniciar. Cada tarjeta conserva sus componentes de selección, relleno, foil, aura y feedback. Los números flotantes reutilizan su pool y tienen un canvas real en las escenas.

Para usar un spritesheet propio, impórtalo como **Sprite (2D and UI)**, configura **Multiple** y corta sus regiones en **Sprite Editor**. Arrastra la región al **Source Image** de la Image correspondiente. Usa **Sliced** con bordes si la placa debe estirarse. Cambia materiales, forma y tipografía directamente en los componentes del prefab. Las instancias de gráficos y de idioma son compartidas; una modificación del prefab llega al menú y a pausa. Las posiciones específicas de cada pantalla se ajustan en su instancia de escena.

**Editar → Estilo y sprites** selecciona el recurso común de la identidad. La herramienta antigua **Aplicar todo el estilo** vuelve a escribir propiedades visuales: úsala cuando quieras reaplicar la identidad global, no después de hacer ajustes manuales que quieras conservar.

## Inglés y español

El primer inicio usa inglés. El selector está en **Opciones** y en **Pausa**, junto a los controles existentes. La elección se guarda en `PlayerPrefs` con la clave `GameLanguage` y permanece al cambiar de escena o reiniciar el juego.

Los textos fijos usan **LocalizedText** junto al TMP. Los textos que incluyen números o cambian de modo tienen pares **English/Spanish** en su controlador: `LevelUpManager`, `UpgradeButton`, `GameOverUI`, `GraphicsSettingsMenu`, `AudioSettingsMenu`, `CurrencyUI`, `ExperienceUI`, `PlayerStatsHUD` y `ChestOpeningSequence`. Los nombres y descripciones de mejoras, objetos de cofre y sobrecargas se editan en sus assets. El tutorial tiene sus pasos y botones en el componente `TutorialManager`; ya no carga un JSON separado.

Cambiar idioma actualiza el contenido mediante eventos. No reinicia un paso del tutorial, no repite descubrimientos ni vuelve a consultar la clasificación por cambiar los textos. `OverrideData.PersistentId` mantiene la identidad de los desbloqueos separada del idioma y del nombre del archivo.

## Configuraciones

| Carpeta | Uso |
| --- | --- |
| `Assets/Configurations/Production/` | Datos de las escenas de producción; archivos con sufijo `_Production` |
| `Assets/Configurations/Production/Resources/` | Recursos globales cargados por el juego |
| `Assets/Configurations/Production/Resources/UI/` | Estilo y registro de prefabs compartidos de UI |
| `Assets/Configurations/Sandbox/` | Copias de pruebas; archivos con sufijo `_Sandbox` |

**Tools → Manners → Configuraciones** selecciona cada carpeta. Los ajustes de experimentación se conectan desde `SandboxTuning` y se promueven exclusivamente con **Tools → Manners → Sandbox → 4**. Las herramientas usan `GameAssetPaths` para emparejar los archivos por nombre base. Un cambio de carpeta conserva su `.meta` y GUID. Los archivos del Mapa 2 permanecen en sus ubicaciones anteriores mientras ese mapa está pausado.

La estructura compartida de UI se edita en prefabs; no hace falta duplicarla entre Production y Sandbox. Los parámetros de partida y de cinemática que ya pertenecían a configuraciones de balance mantienen sus copias independientes.

## Comprobar cambios

1. **Tools → Manners → UI → Validar componentes e idiomas** revisa referencias, scripts ausentes, traducciones, destinos del tutorial y recursos de las cuatro escenas soportadas.
2. **Tools → Manners → UI → Capturar inglés y español** genera vistas y auditoría de texto en `Logs/UIReview/`: 1920×1080, 1280×720 y 2560×1080.
3. En **Window → General → Test Runner → EditMode**, ejecuta `UIAuthoringTests` para comprobar las referencias y el flujo real de idioma, gráficos, pausa, daño y cofre. Conserva también las pruebas de sobrecargas y gráficos existentes.

Las capturas son vistas de edición con ejemplos de datos. Las animaciones, el relleno y la cámara del cofre se comprueban durante Play. El Mapa 2 queda excluido de escenas, validación y pruebas.

La auditoría utiliza cajas de texto y puede señalar la marca diagonal de «Próximamente» del selector de mapas como un cruce con el título de la tarjeta. Esa marca forma parte de la UI actual; debe distinguirse de un texto recortado o de un control que tape otro control.

Para añadir otra pantalla, crea su jerarquía en escena o prefab, asigna referencias en Inspector, añade `LocalizedText` a cada texto fijo y usa `LocalizedString` serializado para formatos dinámicos. Reutiliza el estilo actual, el bus de eventos y `UpdateManager` para animaciones continuas. No añadas generadores visuales ni controles duplicados con valores que ya existan en TMP, Image o RectTransform.

## Verificación de esta entrega

Resultados del 6 de octubre de 2026, manteniendo la UI actual:

| Comprobación | Resultado |
| --- | --- |
| Compilación de código de juego y editor | Sin errores ni advertencias de C# |
| Referencias, componentes e idiomas | 4 escenas, 11 205 componentes, 608 etiquetas bilingües; 0 errores |
| Assets reorganizados | 73 GUID de configuraciones y 3 GUID de scripts conservados |
| Vistas en inglés y español | 96 capturas a 1920×1080, 1280×720 y 2560×1080; sin desbordamientos ni texto fuera de pantalla |
| Regresión automatizada | 71 pruebas aprobadas, 0 fallos |
| Recorrido en Play | Opciones, idioma persistente entre escenas, pausa, gráficos, cierre del tutorial, aviso del cofre, pool de daño y cinemática con cámara 3D |

La regresión ejecutó `UIAuthoringTests`, `OverrideCombinationTests`, `GraphicsBudgetTests`, `PerformanceRegressionTests`, `WebBuildRegressionTests` y `ToonEnvironmentTests`. Las capturas y los informes locales están en `Logs/UIReview/`; `play-chest.png` muestra el cofre durante Play. Los seis avisos de cruce de la auditoría corresponden únicamente al sello diagonal existente del selector de mapas. No se ejecutó una build ni se cambió la versión de Unity.
