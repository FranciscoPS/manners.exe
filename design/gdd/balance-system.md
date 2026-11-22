<!-- atlassian-mirror
source: confluence
id: 10846209
kind: page
title: Sistema de Balance
url: https://blackdoorstudios.atlassian.net/wiki/spaces/~701213bd1c0c92aa748079c0e5c6d387aa64b/pages/10846209
remote_version: v:1
pulled_at: 2026-11-21T23:22:05Z
body_sha1: 9142e426fa346eec66283cb27eccfb24901ce8e1
-->
# Sistema de Balance

> [Confluence: texto subrayado (formato), ubicación no determinada — ver la página]

**Nombre del juego: manners.exe**

**Descripción general**

 Esta documentación tiene como finalidad explicar, describir y facilitar toda la información relacionada con el sistema de balance empleado en el juego *manners.exe*. Esta es una documentación separada pero a su vez, totalmente relacionada con los sistemas operativos y generales del juego por lo que es de vital importancia tener contexto del mismo leyendo el documento: [manners.exe GDD](https://docs.google.com/document/d/1VHdlG3BN7sk4V4f3zeJlx5bTEJi8c6vYmmk0Y1Bu7Mo/edit?usp=drive_link)

**¿Por qué un documento por separado?**

 Este juego es un clon o réplica de juegos de disparo automático con sistemas de mejoras tales como Vampire Survivors y Megabonk por lo que existen muchas variables interconectadas que afectan el balance y ritmo del juego, al ser tantas propiedades resulta imposible memorizar por lo que este documento pretende fungir como guía de entendimiento y biblia técnica que ayudará a encontrar y recordar de mejor manera el sistema completo de balance junto con sus características y propiedades.


## **ÍNDICE RÁPIDO**


1. Sistema de Estadísticas del Jugador
2. Sistema de Experiencia y Niveles
3. Sistema de Mejoras (Upgrades)
4. Sistema de Tienda
5. Sistema de Enemigos
6. Sistema de Oleadas (Waves)
7. Sistema de Spawn Continuo
8. Sistema de Edificios
9. Sistema de Monedas y Diamantes
10. Sistema de Orbes de Experiencia
11. Fórmulas y Cálculos
12. Relaciones entre Sistemas


## **1. SISTEMA DE ESTADÍSTICAS DEL JUGADOR**


Ubicación: GameBalanceConfig.asset (Assets/Resources/)


Este archivo es un ScriptableObject que se edita directamente en Unity (no código). Es el archivo maestro de configuración.

### Propiedades Disponibles:

#### INITIAL PLAYER STATS (Estadísticas Iniciales del Jugador)


|  |  |  |
| --- | --- | --- |
| Propiedad | Valor Actual | Qué Hace |
| Player Max Health | 100 | La vida máxima con la que empieza el jugador. Si llega a 0, Game Over. |
| Player Base Damage | 10 | El daño base que hacen los proyectiles del jugador. Se multiplica con upgrades de Daño. |
| Player Move Speed | 5 | Velocidad de movimiento base (unidades por segundo). Se modifica con upgrades de Velocidad. |
| Player Attack Range | 10 | Distancia máxima a la que el jugador puede detectar y atacar enemigos. |
| Player Attack Cooldown | 0.5 | Tiempo en segundos entre cada disparo. Menor = más rápido. Se reduce con upgrades de Attack Speed. |


Dónde editar: Selecciona GameBalanceConfig en la carpeta Resources, verás estos valores en el Inspector de Unity.


Afecta a:

- PlayerHealth.cs (vida)
- PlayerController.cs (velocidad)
- PlayerStatsManager.cs (base para cálculos de upgrades)
- Weapon/Attack systems

## **2. SISTEMA DE EXPERIENCIA Y NIVELES**

### Ubicación: GameBalanceConfig.asset (Assets/Resources/)

### Propiedades Disponibles:

#### EXPERIENCE SYSTEM


|  |  |  |
| --- | --- | --- |
| Propiedad | Valor Actual | Qué Hace |
| Base Experience Required | 100 | Experiencia necesaria para alcanzar el nivel 2. Esta es la base de la fórmula. |
| Experience Multiplier | 1.5 | Multiplicador exponencial para cada nivel. Determina qué tan rápido escala la XP necesaria. |

### Cómo Funciona:

La experiencia requerida para cada nivel se calcula con esta fórmula:

Ejemplo con valores actuales (Base=100, Mult=1.5):


- Nivel 2: 100 × (1.5^1) = 100 XP
- Nivel 3: 100 × (1.5^2) = 150 XP
- Nivel 4: 100 × (1.5^3) = 225 XP
- Nivel 5: 100 × (1.5^4) = 337 XP
- Nivel 10: 100 × (1.5^9) = 3,837 XP



Cómo ajustar:


|  |  |  |
| --- | --- | --- |
| Quieres que... | Cambia... | Ejemplo |
| Los niveles se obtengan más lento | Aumenta Base Experience | 100 → 150 |
| Los niveles escalen más rápido | Aumenta Multiplier | 1.5 → 1.8 |
| Los niveles escalen más lento | Reduce Multiplier | 1.5 → 1.3 |


Dónde editar: GameBalanceConfig → Experience System

Afecta a:


- PlayerExperience.cs (maneja el sistema de niveles)
- ExperienceManager.cs (calcula XP requerida)
- LevelUpManager.cs (muestra panel al subir de nivel)

## **3. SISTEMA DE MEJORAS (UPGRADES)**

### Ubicación: Cada upgrade es un archivo .asset individual (Assets/ScriptableObjects/Upgrades/)

Cada mejora (Daño, Velocidad, etc.) tiene su propio archivo de configuración.

### Propiedades de Cada Upgrade:

#### IDENTITY (Identidad)


|  |  |
| --- | --- |
| Propiedad | Qué Hace |
| Upgrade Name | Nombre que aparece en la UI (ej: "Damage Module") |
| Description | Descripción del upgrade que ve el jugador |
| Upgrade Type | Tipo de mejora (Damage, AttackSpeed, MoveSpeed, etc.) - NO CAMBIAR |
| Icon | Imagen que representa el upgrade en la UI |

#### PROGRESSION (Progresión)


|  |  |  |
| --- | --- | --- |
| Propiedad | Valor Típico | Qué Hace |
| Base Value | 15 | El valor del upgrade en nivel 1. Si es porcentaje, 15 = +15%. Si es absoluto, 15 = +15 unidades. |
| Multiplier Per Level | 1.2 | Multiplicador por cada nivel. 1.2 = cada nivel es 20% más fuerte que el anterior. |
| Max Level | 999 | Nivel máximo que puede alcanzar este upgrade. 999 = infinito. |

### Cómo Funciona:

El valor de un upgrade en cualquier nivel se calcula así:


Ejemplo: DAMAGE con Base=15, Mult=1.2:


- Nivel 1: 15 × (1.2^0) = +15% daño
- Nivel 2: 15 × (1.2^1) = +18% daño
- Nivel 3: 15 × (1.2^2) = +21.6% daño
- Nivel 5: 15 × (1.2^4) = +31.1% daño
- Nivel 10: 15 × (1.2^9) = +77.4% daño



|  |  |  |
| --- | --- | --- |
| Quieres que... | Cambia... | Ejemplo |
| El upgrade sea más fuerte al inicio | Aumenta Base Value | 15 → 20 |
| El upgrade sea más débil al inicio | Reduce Base Value | 15 → 10 |
| El upgrade escale más rápido | Aumenta Multiplier | 1.2 → 1.3 |
| El upgrade escale más lento | Reduce Multiplier | 1.2 → 1.15 |
| Limitar cuántas veces se puede mejorar | Cambia Max Level | 999 → 10 |

#### DISPLAY SETTINGS (Configuración de Visualización)


|  |  |
| --- | --- |
| Propiedad | Qué Hace |
| Is Percentage | Si está marcado, muestra como porcentaje (+15%). Si no, muestra como valor absoluto (+15 HP). |
| Value Suffix | Texto que se agrega al final del número (ej: "HP", "Range", "m/s"). |

#### RARITY & WEIGHT (Rareza y Peso)


|  |  |  |
| --- | --- | --- |
| Propiedad | Valor Típico | Qué Hace |
| Is Premium | ☐ / ☑ | Si está marcado, este upgrade SOLO aparece en niveles 5, 10, 15, 20, etc. (Niveles milestone). |
| Spawn Weight | 50 | Probabilidad de aparecer. Mayor número = más probable. 100 = el doble de probable que 50. |


Ejemplo de Spawn Weight:


- Upgrade A: Weight = 100
- Upgrade B: Weight = 50
- Upgrade C: Weight = 25


Probabilidad real:


- Upgrade A: 100÷175 = 57% de aparecer
- Upgrade B: 50÷175 = 29% de aparecer
- Upgrade C: 25÷175 = 14% de aparecer

#### SHOP SETTINGS (Configuración de Tienda)


|  |  |  |
| --- | --- | --- |
| Propiedad | Valor Típico | Qué Hace |
| Is Available In Shop | ☑ | Si está marcado, este upgrade puede aparecer en la tienda. Si no, solo en level ups. |
| Shop Base Cost | 100 | Costo en monedas de oro para comprar el nivel 1. |
| Shop Cost Multiplier | 1.5 | Multiplicador de precio por nivel. 1.5 = cada nivel cuesta 50% más que el anterior. |
| Shop Spawn Weight | 50 | Probabilidad de aparecer en la tienda (funciona igual que Spawn Weight). |

### Cálculo de Precio en Tienda:

Ejemplo con Base=100, Mult=1.5:


- Nivel 1: 100 × (1.5^0) = 100 monedas
- Nivel 2: 100 × (1.5^1) = 150 monedas
- Nivel 3: 100 × (1.5^2) = 225 monedas
- Nivel 4: 100 × (1.5^3) = 337 monedas


Dónde editar: Project → Assets → ScriptableObjects → Upgrades → Selecciona el upgrade específico

Afecta a:


- LevelUpManager.cs (muestra upgrades al subir nivel)
- ShopScript.cs (muestra upgrades en tienda)
- PlayerStatsManager.cs (aplica el efecto del upgrade al jugador)


## **4. SISTEMA DE TIENDA**

### Ubicación: ShopUpgradeDatabase.asset (Assets/Resources/)

### Propiedades Disponibles:

#### AVAILABLE SHOP UPGRADES


|  |  |
| --- | --- |
| Propiedad | Qué Hace |
| Shop Upgrades | Lista de upgrades que PUEDEN aparecer en la tienda. Arrastra aquí los archivos .asset de upgrades. |


IMPORTANTE: Para que un upgrade aparezca en tienda:


1. Debe estar en esta lista
2. Debe tener "Is Available In Shop" marcado
3. No debe estar en nivel máximo

#### SHOP SETTINGS


|  |  |  |
| --- | --- | --- |
| Propiedad | Valor Actual | Qué Hace |
| Upgrades Per Refresh | 3 | Cuántos upgrades diferentes se muestran cada vez que abres la tienda. |
| Shop Global Cooldown | 120 | Tiempo en segundos que debes esperar después de comprar algo antes de poder comprar de nuevo. |


Ejemplo de Cooldown:


- 120 segundos = 2 minutos entre compras
- 180 segundos = 3 minutos entre compras
- 60 segundos = 1 minuto entre compras


Dónde editar: ShopUpgradeDatabase en Resources

Afecta a:


- ShopScript.cs (abre/cierra tienda)
- LevelUpManager.cs (maneja compras y cooldown)

## 5**. SISTEMA DE ENEMIGOS**

### Ubicación: Cada tipo de enemigo es un archivo .asset (Assets/ScriptableObjects/Enemies/)

Puedes tener múltiples configuraciones: BasicEnemy, FastEnemy, TankEnemy, etc.

### Propiedades de cada Enemy Configuration:

#### POOL SETTINGS


|  |  |
| --- | --- |
| Propiedad | Qué Hace |
| Enemy Pool Type | Tipo de pool a usar. BasicEnemy, FastEnemy, etc. Debe coincidir con el prefab asignado en PoolManager. |

#### STATS (Estadísticas)


|  |  |  |
| --- | --- | --- |
| Propiedad | Valor Típico | Qué Hace |
| Max Health | 30 | Vida del enemigo. Más alto = más difícil de matar. |
| Move Speed | 3 | Velocidad de movimiento (unidades/segundo). Más alto = se mueve más rápido. |
| Contact Damage | 10 | Daño que hace al tocar al jugador. |

#### EXPERIENCE DROP


|  |  |  |
| --- | --- | --- |
| Propiedad | Valor Típico | Qué Hace |
| Orb Config | Ninguno/Archivo | Si asignas un OrbConfiguration, usará esos valores. Si está vacío, usa valores por defecto. |
| Min Orbs | 1 | Mínimo de orbes de XP que suelta al morir. |
| Max Orbs | 3 | Máximo de orbes de XP que suelta al morir. El número real es aleatorio entre Min y Max. |
| Orb Spawn Radius | 1 | Radio (en metros) donde se dispersan los orbes alrededor del enemigo muerto. |


Ejemplo:


- Min Orbs = 1, Max Orbs = 3
- El enemigo puede soltar 1, 2 o 3 orbes aleatoriamente
- Promedio: 2 orbes por enemigo

#### CURRENCY DROPS (Drops de Monedas)


|  |  |  |
| --- | --- | --- |
| Propiedad | Valor Típico | Qué Hace |
| Coin Drop Chance | 0.5 | Probabilidad de soltar monedas (0-1). 0.5 = 50% de probabilidad. |
| Min Coins | 1 | Mínimo de monedas si decide soltar. |
| Max Coins | 3 | Máximo de monedas si decide soltar. |
| Diamond Drop Chance | 0.1 | Probabilidad de soltar diamantes (0-1). 0.1 = 10% de probabilidad. |
| Min Diamonds | 1 | Mínimo de diamantes si decide soltar. |
| Max Diamonds | 1 | Máximo de diamantes si decide soltar. |


Ejemplo con 100 enemigos muertos:


- 50 de ellos soltarán monedas (50% chance)
- Cada uno suelta 1-3 monedas
- Total aproximado: 100 monedas
- 10 de ellos soltarán diamantes (10% chance)
- Total aproximado: 10 diamantes


Dónde editar: Project → ScriptableObjects → Enemies → Selecciona la configuración

Afecta a:


- EnemyHealth.cs (aplica stats y drops)
- EnemyController.cs (aplica velocidad y daño)
- PoolManager.cs (spawnea el prefab correcto)


## **6. SISTEMA DE OLEADAS (WAVES)**

### Ubicación: Cada oleada es un archivo .asset (Assets/ScriptableObjects/Waves/)

Las oleadas son eventos temporales que spawnean enemigos. Puedes tener Wave1, Wave2, BossWave, etc.

### Propiedades de Cada Wave:

#### WAVE INFO


|  |  |  |
| --- | --- | --- |
| Propiedad | Valor Típico | Qué Hace |
| Wave Name | "Wave 1" | Nombre de la oleada (solo para referencia, no se muestra al jugador). |
| Total Enemies | 10 | Número TOTAL de enemigos que spawneará esta oleada completa. |

#### ENEMY DISTRIBUTION (Distribución de Enemigos)


Esta es una lista de entradas. Cada entrada tiene:


|  |  |
| --- | --- |
| Propiedad | Qué Hace |
| Enemy Config | Qué tipo de enemigo (BasicEnemy, FastEnemy, etc.). |
| Percentage | Peso probabilístico. Mayor número = más probable que aparezca este tipo. |


Ejemplo de Distribución:


IMPORTANTE: Los porcentajes NO tienen que sumar 100. El sistema calcula la probabilidad relativa.

Ejemplo 2:

#### SPAWN SETTINGS


|  |  |  |
| --- | --- | --- |
| Propiedad | Valor Típico | Qué Hace |
| Spawn Interval | 3 | Tiempo en segundos entre cada "batch" de enemigos. |
| Enemies Per Batch | 4 | Cuántos enemigos aparecen al mismo tiempo en cada intervalo. |


Ejemplo de Spawning:


Cómo ajustar:


|  |  |
| --- | --- |
| Quieres que... | Cambia... |
| La oleada sea más larga | Aumenta Total Enemies |
| Spawnee enemigos más rápido | Reduce Spawn Interval |
| Spawnee más enemigos a la vez | Aumenta Enemies Per Batch |
| La oleada sea más difícil | Cambia Enemy Distribution para más enemigos fuertes |


Dónde editar: Project → ScriptableObjects → Waves → Selecciona la wave


Afecta a:


- EnemySpawnManager.cs (ejecuta las oleadas)


## **7. SISTEMA DE SPAWN CONTINUO**

### Ubicación: EnemySpawnManager (GameObject en la escena)

Este sistema spawnea enemigos DE FORMA CONSTANTE entre oleadas.

### Propiedades del Spawn Manager:

#### WAVE CONFIGURATION


|  |  |  |
| --- | --- | --- |
| Propiedad | Valor Actual | Qué Hace |
| Wave Queue | Lista de WaveData | ORDEN en que se ejecutarán las oleadas. La primera es Wave 1, luego Wave 2, etc. |
| Time Between Waves | 30 | Tiempo en segundos de descanso DESPUÉS de terminar una oleada y ANTES de empezar la siguiente. |
| Auto Loop Waves | ☑ | Si está marcado, cuando terminen todas las oleadas, vuelve a empezar desde la primera. Si no, se detiene. |


Ejemplo de Wave Queue:

#### CONTINUOUS SPAWN (Spawn Continuo)


|  |  |  |
| --- | --- | --- |
| Propiedad | Valor Actual | Qué Hace |
| Enable Continuous Spawn | ☑ | Si está marcado, spawnea enemigos constantemente. Si no, solo oleadas. |
| Continuous Spawn Interval | 3 | Cada cuántos segundos spawnea enemigos continuos. |
| Continuous Enemies Per Spawn | 2 | Cuántos enemigos spawnea cada vez (en diferentes spawn points). |
| Continuous Enemy Types | Lista de EnemyConfig | Tipos de enemigos que pueden aparecer en spawn continuo. Elige uno al azar cada vez. |


Cómo Funciona el Spawn Continuo:


Cómo ajustar la dificultad:


|  |  |  |
| --- | --- | --- |
| Quieres que... | Cambia... | Ejemplo |
| Aparezcan menos enemigos | Aumenta Continuous Spawn Interval | 3 → 6 segundos |
| Aparezcan más enemigos a la vez | Aumenta Continuous Enemies Per Spawn | 2 → 3 |
| Enemigos más difíciles al final | Ordena Continuous Enemy Types de fácil a difícil y usa scripting para cambiar dinámicamente |  |


IMPORTANTE: El spawn continuo se PAUSA cuando hay una oleada activa. Solo funciona en los "descansos".


Dónde editar: Hierarchy → EnemySpawnManager → Inspector

Afecta a:


- EnemySpawnManager.cs (lógica de spawn)
- SpawnPoint.cs (puntos donde aparecen)


## **8. SISTEMA DE EDIFICIOS**

### Ubicación: GameBalanceConfig.asset (Assets/Resources/)

Los edificios son objetos destructibles del mundo que dan recompensas al destruirlos.

### Propiedades Disponibles:

#### BUILDING DROP RATES (Probabilidades de Drop)


|  |  |  |
| --- | --- | --- |
| Propiedad | Valor Actual | Qué Hace |
| Building Coin Drop Chance | 0.7 | Probabilidad de soltar monedas (0-1). 0.7 = 70% de probabilidad. |
| Building Min Coins | 2 | Mínimo de monedas si decide soltar. |
| Building Max Coins | 5 | Máximo de monedas si decide soltar. |
| Building Diamond Drop Chance | 0.15 | Probabilidad de soltar diamantes (0-1). 0.15 = 15% de probabilidad. |
| Building Min Diamonds | 1 | Mínimo de diamantes si decide soltar. |
| Building Max Diamonds | 2 | Máximo de diamantes si decide soltar. |


Ejemplo con 10 edificios destruidos:


- 7 soltarán monedas (70% chance)
- Cada uno suelta 2-5 monedas (promedio 3.5)
- Total aproximado: 24.5 monedas
- 1-2 soltarán diamantes (15% chance)
- Cada uno suelta 1-2 diamantes (promedio 1.5)
- Total aproximado: 2.25 diamantes

#### BUILDING EXPERIENCE


|  |  |  |
| --- | --- | --- |
| Propiedad | Valor Actual | Qué Hace |
| Building Min Orbs | 0 | Mínimo de orbes de XP que suelta. |
| Building Max Orbs | 0 | Máximo de orbes de XP que suelta. |
| Building Orb Spawn Radius | 0 | Radio (metros) donde se dispersan los orbes. |
| Building Default Experience Value | 0 | Cuánta XP vale cada orbe que suelta el edificio. |


Ejemplo:

Impacto en el ritmo:

Dónde editar: GameBalanceConfig → Building Drop Rates / Building Experience

Afecta a:

- BuildingsScript.cs (maneja destrucción y drops)


## **9. SISTEMA DE MONEDAS Y DIAMANTES**

### Ubicación: GameBalanceConfig.asset (Assets/Resources/) + Configuraciones de Enemigos/Edificios

### Propiedades de Collectibles:

#### PICKUP ATTRACTION (Atracción Magnética)


|  |  |  |
| --- | --- | --- |
| Propiedad | Valor Actual | Qué Hace |
| Coin Attraction Range | 5 | Radio (metros) donde las monedas empiezan a volar hacia el jugador. |
| Diamond Attraction Range | 5 | Radio (metros) donde los diamantes empiezan a volar hacia el jugador. |


IMPORTANTE: Esto NO es lo mismo que el upgrade "Magnet Range". Este es el valor BASE.

#### PICKUP LIFETIME (Tiempo de Vida)

|  |  |  |
| --- | --- | --- |
| Propiedad | Valor Actual | Qué Hace |
| Coin Lifetime | 30 | Segundos antes de que una moneda desaparezca. |
| Diamond Lifetime | 30 | Segundos antes de que un diamante desaparezca. |


Sistema de Parpadeo:


- A los últimos 10 segundos: Empieza a parpadear lento
- A los últimos 2 segundos: Parpadea muy rápido (advertencia crítica)

### Valor de las Monedas:

Las monedas y diamantes tienen un valor fijo de 1 por defecto. Esto significa:

¿Por qué existen ambos entonces?


- Diamantes tienen menor probabilidad de drop
- Son visualmente diferentes (color cyan)
- Sistema preparado para futura diferenciación


Dónde editar:


- GameBalanceConfig → Pickup Attraction / Pickup Lifetime
- Collectible.cs (código) si quieres cambiar valores individuales


Afecta a:


- Collectible.cs (comportamiento de monedas/diamantes)
- CurrencyManager.cs (lleva la cuenta del dinero)

## **10. SISTEMA DE ORBES DE EXPERIENCIA**

### Ubicación: OrbConfiguration.asset (Assets/ScriptableObjects/Orbs/) - Opcional

Los orbes son los ítems que dan experiencia al jugador.

### Propiedades de Orb Configuration:


|  |  |  |
| --- | --- | --- |
| Propiedad | Valor Típico | Qué Hace |
| Experience Value | 10 | Cuánta XP da este orbe al recogerlo. |
| Attraction Range | 5 | Radio (metros) donde el orbe empieza a volar hacia el jugador. |
| Move Speed | 8 | Velocidad de movimiento del orbe cuando vuela hacia el jugador. |
| Orb Lifetime | 30 | Segundos antes de que el orbe desaparezca. |


IMPORTANTE: Si un enemigo/edificio NO tiene asignado un OrbConfiguration, usa valores por defecto del sistema.


Valores por Defecto:


- Experience Value: Se toma de la configuración del enemigo/edificio
- Attraction Range: Se toma de GameBalanceConfig.OrbAttractionRange
- Move Speed: 8 (hardcoded)
- Lifetime: Se toma de GameBalanceConfig.OrbLifetime


Dónde editar: Project → ScriptableObjects → Orbs → Crear/Editar configuración


Afecta a:


- ExperienceOrb.cs (comportamiento del orbe)
- EnemyHealth.cs (spawnea orbes al morir)
- BuildingsScript.cs (spawna orbes al destruirse)


## **11. FÓRMULAS Y CÁLCULOS**

### EXPERIENCIA REQUERIDA POR NIVEL


Tabla de referencia (Base=100, Mult=1.5):


|  |  |  |
| --- | --- | --- |
| Nivel | XP Requerida | XP Acumulada |
| 2 | 100 | 100 |
| 3 | 150 | 250 |
| 4 | 225 | 475 |
| 5 | 337 | 812 |
| 6 | 506 | 1,318 |
| 7 | 759 | 2,077 |
| 8 | 1,139 | 3,216 |
| 9 | 1,708 | 4,924 |
| 10 | 2,563 | 7,487 |

### VALOR DE UPGRADE POR NIVEL

Ejemplo: Damage (Base=15, Mult=1.2):


|  |  |  |
| --- | --- | --- |
| Nivel | Valor Calculado | Daño real con base 10 |
| 1 | +15% | 11.5 |
| 2 | +18% | 11.8 |
| 3 | +21.6% | 12.16 |
| 4 | +25.9% | 12.59 |
| 5 | +31.1% | 13.11 |
| 10 | +77.4% | 17.74 |
| 20 | +568% | 66.8 |

### PRECIO EN TIENDA POR NIVEL


Tabla de referencia (Base=100, Mult=1.5):


|  |  |  |
| --- | --- | --- |
| Nivel Compra | Precio | Monedas Acumuladas Necesarias |
| 1 | 100 | 100 |
| 2 | 150 | 250 |
| 3 | 225 | 475 |
| 4 | 337 | 812 |
| 5 | 506 | 1,318 |
| 10 | 3,837 | 14,762 |

### ENEMIGOS SPAWNEADOS POR MINUTO


Spawn Continuo:

Ejemplo: Interval=3, Enemies=2

Spawn de Oleadas:

Ejemplo: Total=10, SpawnInterval=3, Batch=4

### INGRESOS DE MONEDAS POR ENEMIGO


Ejemplo: Chance = 0.5, Min = 1, Max = 3

## **12. RELACIONES ENTRE SISTEMAS**

### FLUJO DE PROGRESIÓN:

### FLUJO ECONÓMICO:

### DEPENDENCIAS CRÍTICAS:


GameBalanceConfig afecta a:


- PlayerHealth (vida base)
- PlayerController (velocidad base)
- Weapon Systems (daño base, cooldown)
- BuildingsScript (drops de edificios)
- ExperienceManager (cálculo de niveles)
- Collectibles (lifetimes, attraction range)


UpgradeData afecta a:


- LevelUpManager (opciones al subir nivel)
- ShopScript (opciones en tienda)
- PlayerStatsManager (aplicación de mejoras)


EnemyConfiguration afecta a:


- EnemyHealth (vida, drops)
- EnemyController (velocidad, daño)


WaveData afecta a:


- EnemySpawnManager (qué enemigos spawnear y cuándo)


EnemySpawnManager afecta a:


- Ritmo general del juego
- Dificultad progresiva
- Generación de recursos (XP y monedas)


## **CHECKLIST DE BALANCE**

### Para ajustar la velocidad de progresión:

-  GameBalanceConfig → Base Experience Required
-  GameBalanceConfig → Experience Multiplier
-  GameBalanceConfig → Building Default Experience Value
-  EnemyConfiguration → Min/Max Orbs

### Para ajustar la economía:

-  GameBalanceConfig → Building Coin Drop settings
-  EnemyConfiguration → Coin Drop Chance/Min/Max
-  UpgradeData → Shop Base Cost
-  UpgradeData → Shop Cost Multiplier
-  ShopUpgradeDatabase → Shop Global Cooldown

### Para ajustar la dificultad:

-  EnemyConfiguration → Max Health
-  EnemyConfiguration → Move Speed
-  EnemyConfiguration → Contact Damage
-  EnemySpawnManager → Continuous Spawn Interval
-  EnemySpawnManager → Continuous Enemies Per Spawn
-  WaveData → Total Enemies
-  WaveData → Spawn Interval

### Para ajustar el poder de los upgrades:

-  UpgradeData → Base Value
-  UpgradeData → Multiplier Per Level
-  UpgradeData → Spawn Weight
-  UpgradeData → Is Premium


## **CONSEJOS IMPORTANTES**


NO CAMBIES:


- Upgrade Type (romperá el código)
- Enemy Pool Type sin cambiar también el prefab en PoolManager
- Nombres de ScriptableObjects una vez referenciados


PUEDES CAMBIAR LIBREMENTE:


- Todos los valores numéricos
- Probabilidades (0-1)
- Nombres visuales (Wave Name, Upgrade Name)
- Descripciones
- Icons/Sprites


DESPUÉS DE CAMBIAR:


1. Guarda (Ctrl+S)
2. Prueba en Play Mode
3. Si los números no cambian, revisa que estés editando el archivo correcto en Resources


PARA TESTEAR BALANCE:


1. Anota cuánto tardas en llegar a nivel 5
2. Anota cuántas monedas tienes al minuto 3
3. Ajusta valores y repite
4. Compara resultados
