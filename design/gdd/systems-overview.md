<!-- atlassian-mirror
source: confluence
id: 5701639
kind: live_doc
title: Sistemas
url: https://blackdoorstudios.atlassian.net/wiki/spaces/~701213bd1c0c92aa748079c0e5c6d387aa64b/pages/5701639
remote_version: d1:1
pulled_at: 2026-11-21T23:14:29Z
body_sha1: 843151eb1b83521801af2caa1cb97fe173231683
-->
# Sistemas

> [Confluence: tabla de contenidos (macro) — ver la página]

# Economía interna del juego

Basada principalmente en tres divisas: Monedas, Experiencia y Gemas

## Monedas

Las monedas son un tipo de divisa dentro del juego las cuales se obtienen destruyendo edificios y derrotando enemigos.

Las monedas son las que tienen la mayor cantidad de usos de entre las tres divisas presentes en el juego. Su uso dentro del gameplay es que las puedes utilizar para comprar mejoras especiales dentro de la tienda. Fuera del gameplay, las monedas se pueden utilizar para comprar mejoras para el árbol de habilidades y para comprar aspectos para el robot de rarezas normales.

## Experiencia

La experiencia es lo que el jugador necesita para subir de nivel dentro de los niveles. Únicamente se consigue derrotando enemigos.

Para saber más sobre el uso de la experiencia, leer la sección de Aumento de Nivel.

## Gemas

Las gemas son la divisa que se utiliza únicamente en la tienda del juego. Tienen una probabilidad baja de aparecer al derrotar enemigos y se pueden utilizar para conseguir atuendos de rarezas mayores.

# Ataque automático

En el juego, el jugador no controla (completamente) sus disparos. La computadora se encarga de calcular la distancia del jugador con respecto a todos los enemigos existentes, para después decidir a cuál disparará. El enemigo que tendrá mayor prioridad para ser eliminado será aquel que se encuentre más cerca del jugador.

# Progresión

Para pensar en la progresión, debemos tener pensada un “Ancla” para nuestro juego, una estadística alrededor de la cual todos los demás números se tendrán que balancear.

Para nuestro juego decidimos que el ancla fuera la Experiencia (con la que se sube de nivel).

> [Confluence: "Experiencia" con resaltado de color — ver la página]

Se tiene un sistema bastante complejo sobre cómo funciona la progresión del juego. El siguiente link lleva a un Google Sheets donde están definidas los números para la progresión.

[https://docs.google.com/spreadsheets/d/1W7p7lwMxVXPfTiWSjUTQGrrnyBvi6NO3k1oU-u\_8JUI/edit?gid=0#gid=0](https://docs.google.com/spreadsheets/d/1W7p7lwMxVXPfTiWSjUTQGrrnyBvi6NO3k1oU-u_8JUI/edit?gid=0#gid=0) 

También incluimos un enlace a la plataforma de Machinations, en la cual se puede ver una simulación de cómo funciona el sistema de aumento de nivel.

<https://my.machinations.io/d/mannersexe-diagrams/c7e9488210e411f18bb5060b7ed1946d> 

# Enemigos

Para ver información más completa, leer el documento de Sistema de Balance en la carpeta de Programación.

Existen dos tipos de enemigos (por ahora)

- Tanques (normales)
- Camionetas (rápidos)

Cada uno cuenta con estadísticas propias, éstas siendo la vida, la velocidad de movimiento y el daño que infringe al jugador.

## Comportamiento (Todo un sistema por aparte)

El comportamiento de los enemigos por el momento es el mismo para ambos tipos: se mueven en una línea recta hacia el jugador.

Por el momento, todos los enemigos tienen solamente una forma de causarle daño al jugador, la cual es tener contacto directo con él.

## Oleadas de enemigos

Las oleadas son la forma en la que los enemigos aparecen y siguen al jugador.

## Número de enemigos por oleada

Cada oleada tiene un número definido de enemigos, las cuales también incluyen cuántos enemigos hay de cada tipo. Una vez derrotados, las nuevas oleadas contarán con estadísticas aumentadas, dando pie al balance del juego entre estadísticas de los enemigos con las del jugador.

## Puntos de aparición

Los enemigos tienen puntos específicos en el mapa en los cuales pueden aparecer. Estos puntos se encuentran principalmente en las esquinas del mapa y algunos en el centro.

# Aumento de nivel

Cada vez que el jugador haya recolectado las suficientes orbes de experiencia, las cuales incrementan cada vez, el jugador podrá “subir de nivel”.

Subir de nivel significa que automáticamente aparecerán algunas opciones de estadísticas base que el jugador podrá elegir para aumentar.

El escalado de los incrementos para cada estadística está balanceado alrededor del impacto que cada una tiene hacia el gameplay.

# Sinergias

Cuando el jugador haya alcanzado un cierto nivel para dos estadísticas bases diferentes, ocurrirá un evento (por ahora) llamado: Sinergias.

Las sinergias son una forma en la que integraremos “novedad” o “replay value” al juego, ya que agregan un elemento de sorpresa y de intriga; de manera óptima, una vez el jugador descubra su primera sinergia, debería pensar en cuáles son las posibles combinaciones posibles y aunque no la descubra en esa partida, quiera iniciar una nueva para descubrirlo.

Para conocer más a detalle las sinergias, revisar el documento.

# Escalado de dificultad

Las estadísticas de los enemigos aumentarán conforme avances en el juego, llegando a su máxima expresión cercanos al tiempo límite.

Para la dificultad se estableció que se utilizará una curva exponencial controlada. Esto lo que hace es que comienza con un crecimiento rápido para enganchar al jugador, seguido por un aumento progresivo de dificultad o costos que estabiliza el avance. Este tipo de curva permite mantener la sensación de progreso mientras se conserva el equilibrio del sistema a largo plazo.

## Estadísticas balanceables de los enemigos

Las oleadas de enemigos tienen un número especificado de enemigos totales que pueden aparecer. Al eliminar a los enemigos, la cantidad de enemigos eliminados será la misma que la oleada o pool que regenerará, es decir, si la oleada tiene 10 enemigos y el jugador elimina a 5, la oleada regenerará 5 para llenar ese espacio que se generó. 

Las estadísticas de los enemigos irán escalando conforme pase el tiempo; las estadísticas que se subirán serán velocidad de movimiento, vida y daño. Cuando se llegue a las oleadas finales, en caso de que el jugador esté lo suficientemente fuerte para eliminar de manera instantánea a las oleadas, estas reaparecerán de manera casi instantánea, y poco a poco los enemigos de estas iran escalando hasta que pueden eliminar al jugador, en caso de que el jugador no pueda eliminar a los enemigos, el numero de la oleada se mantendra igaul hasta que el jugador los pueda eliminar o hasta que este muera.    

- Siempre lo mismo, + spd, hp y dmg
- Spawn de enemigos constante
- Overwhelm de enemigos

# Límite de tiempo

Después de que el jugador haya llegado al minuto 20-25 (TBD), la run terminará. (WIP) Tenemos que decidir si llegará algún jefe final que no se puede derrotar o que “se le acabe la pila al robot y simplemente se deje de mover” o quizás llegando al minuto, un jefe aparecerá y cuando lo derrote aparecerá un mensaje avisándole al jugador que terminó el nivel.

- Terminar el nivel y cuando lo termines ofreceremos mejoras mas chidas asi de que más macabras, bien bien, acá bien infernales.

# Estadísticas

## Estadísticas base

Cada personaje cuenta con un set de estadísticas principales, las cuales son:

- Velocidad de Movimiento.
- Velocidad de ataque (La cadencia de disparo).
- Daño del disparo.
- Imán (Radio de recolección de pickups).
- Rango de ataque.

## Estadísticas premium

Además de las estadísticas principales, el jugador también tiene acceso a estadísticas premium, las cuales podrá conseguir llegando a niveles múltiplos de 5, comprándolas en la tienda.

Éstas estadísticas agregan comportamientos adicionales al disparo normal del jugador, lo que le permitirá aumentar sus probabilidades de supervivencia. Por el momento, las estadísticas premium son:

- Multi Shot: Implementa la probabilidad de disparar tres balas en lugar de solo una. Con cada mejora a esta estadística, la probabilidad y la cantidad de disparos aumentan.
- Balas Explosivas: Con cada disparo hay una probabilidad de que cuando impacte genere una explosión que causa daño a enemigos cercanos a la zona de impacto. Mejorar esta estadística aumenta la probabilidad de que la explosión suceda.
- Knockback: Cuando tus disparos impacten a los enemigos, hay una probabilidad de que sean empujados hacia atrás y no se muevan durante unos instantes.

## Estadísticas iniciales

Cada skin del juego contará con un set distinto de estadísticas [base iniciales](https://blackdoorstudios.atlassian.net/wiki/spaces/~701213bd1c0c92aa748079c0e5c6d387aa64b/pages/5701639/Sistemas#Estad%C3%ADsticas-base), cambiando sus valores y fomentando la existencia de estilos de juego y expresión del jugador.

Una estadística inicial que siempre se mantendrá constante, incluso subiendo de nivel es la vida máxima del jugador.

# Condiciones de Victoria/Derrota

## Victoria

Derrotar al jefe del minuto 20-30 (TBD).

## Derrota

La principal condición de derrota del juego es que el jugador se quede sin vida.

# Cámara

La cámara está situada para proporcionar una vista isométrica del entorno, permitiéndole al jugador tener una visión más completa de sus alrededores.

La cámara sigue automáticamente al jugador sin que éste tenga que hacer algo para moverla.

La distribución de la pantalla está en una proporción de 16:9.

# Minimapa

El juego cuenta con un minimapa, el cual se actualiza constantemente.

Los elementos presentes en el minimapa son:

- Ubicación del jugador
- Ubicación de los enemigos
- Ubicación de la tienda

# Tienda

La tienda siempre estará presente para el jugador.

Una vez que el jugador haya comprado algo en la tienda, ésta cambiará de lugar en el mapa y se trasladará a otra posición; aunque siempre estará mostrada en el minimapa.

# Combate

> [Confluence: comentario en línea sobre "Combate" — ver la página]

## Trade-Offs

Dentro del juego, las relaciones de tradeoff entre daño, vida de enemigos y duración de los encuentros están diseñadas para alinearse con el sistema económico central del juego, cuyo recurso principal es la experiencia. Dado que la experiencia se obtiene al derrotar enemigos, la velocidad con la que el jugador puede eliminar a los enemigos impacta directamente el ritmo de progresión y el balance general del sistema.

Por esta razón, el daño del jugador, la vida de los enemigos y la frecuencia con la que aparecen están cuidadosamente relacionados. Si los enemigos murieran demasiado rápido, el jugador obtendría experiencia a un ritmo excesivo, acelerando la progresión y rompiendo el balance del juego. Por otro lado, si los enemigos fueran demasiado resistentes, el flujo de experiencia disminuiría, generando una sensación de progreso lento y reduciendo la satisfacción del combate.

## ¿Cómo es que la cantidad de daño encaja con el macro económico del juego?

La cantidad de daño que el jugador puede infligir está directamente relacionada con la macroeconomía del juego. Debido a que los enemigos son la principal fuente de experiencia, la posibilidad de escalar el daño debería ser coherente con el incremento de dificultad a lo largo de la partida.

Durante los primeros minutos del juego, el daño del jugador es suficiente para derrotar enemigos en pocos impactos, permitiendo que la experiencia se obtenga rápidamente y reforzando el ciclo de recompensa temprana. Conforme avanza la partida, el daño del jugador aumenta progresivamente, permitiendo que los enemigos sean eliminados en menor tiempo y con menos impactos, pero esto es balanceado con el incremento de enemigos conforme avanza la partida.

## Protecciones de los enemigos

Los enemigos no poseen sistemas de armadura o defensas especiales. Su aguante es percibido en la cantidad; entre más enemigos hay en el mapa, más complicado se vuelve eliminarlos. Esto mantiene el sistema de combate simple y enfocado en el posicionamiento del jugador y el manejo de las hordas.
