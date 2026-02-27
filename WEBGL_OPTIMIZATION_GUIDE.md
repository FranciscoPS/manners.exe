# 🎯 SOLUCIÓN PARA PERFORMANCE EN WEBGL

## 🔴 PROBLEMA PRINCIPAL IDENTIFICADO

Tu juego tiene **configuraciones de rendering muy altas** que están diseñadas para PC standalone, pero WebGL es **10-20x más lento** en rendering que nativo.

### **Problemas Encontrados:**

1. ✅ **Sombras activadas** (shadows: 2) → WebGL NO puede manejar sombras dinámicas bien
2. ✅ **Shadow distance: 40m** → Renderiza sombras muy lejos (costoso)
3. ✅ **Pixel lights: 2** → Múltiples luces en tiempo real por objeto
4. ✅ **Sin frame cap** → Browser puede intentar 144+ FPS y fallar
5. ✅ **PlayerController** modificaba animator.speed cada frame innecesariamente

---

## ✅ SOLUCIONES IMPLEMENTADAS

### **1. WebGLOptimizer.cs - Script Automático**

**Archivo creado:** `Assets/Scripts/Core/WebGLOptimizer.cs`

Este script detecta automáticamente WebGL y aplica optimizaciones críticas:

- ❌ **Desactiva sombras** completamente (mayor impacto)
- 🔆 **Limita luces** a 0 pixel lights (solo direccional)
- 🎯 **Framerate cap** a 60 FPS (previene throttling del browser)
- 🖼️ **Desactiva anti-aliasing** (costoso en WebGL)
- ⚡ **Desactiva VSync** (browsers lo manejan automáticamente)
- 💾 **Reduce budgets** de partículas y async upload

**Cómo usarlo:**

1. **Crear un GameObject vacío** en tu escena de inicio (la que carga primero)
2. **Nombrar**: `WebGLOptimizer`
3. **Agregar el componente** `WebGLOptimizer.cs`
4. **Configurar en Inspector:**
   - ✅ Auto Apply On Start: `true`
   - ✅ Disable Shadows: `true`
   - ✅ Disable Realtime Lights: `true`
   - ✅ Cap Frame Rate: `true`
   - Target Frame Rate: `60`
   - ✅ Disable Anti Aliasing: `true`
   - ✅ Disable VSync: `true`
   - ✅ Show Debug Info: `true` (para ver el log)

5. **IMPORTANTE**: Hacer el GameObject **DontDestroyOnLoad** si tienes múltiples escenas

### **2. PlayerController Optimizado**

- Ahora solo modifica `animator.speed` cuando el valor cambia
- Cache del último valor para evitar sets innecesarios
- Reduce calls al Animator de 60/s a solo cuando cambia speed modifier

---

## 📋 CONFIGURACIÓN MANUAL (OPCIONAL PERO RECOMENDADO)

Si quieres optimizar manualmente para WebGL en el Editor:

### **1. Quality Settings para WebGL:**

1. `Edit > Project Settings > Quality`
2. En la tabla de arriba, buscar la columna `WebGL`
3. Hacer clic en la celda para crear un perfil específico (o usar uno existente)
4. **Configurar el perfil asignado a WebGL:**

```
✅ CRÍTICO - SHADOWS:
Shadows: Disable
Shadow Resolution: N/A (ya desactivado)
Shadow Distance: 0

✅ LUCES:
Pixel Light Count: 0 (solo luz direccional)
Realtime Reflection Probes: OFF

✅ RENDERING:
Anti Aliasing: Disabled
Soft Particles: OFF
VSync Count: 0

✅ PERFORMANCE:
Async Upload Time Slice: 1ms (bajo)
Async Upload Buffer Size: 8MB (bajo)
Particle Raycast Budget: 128 (bajo)
```

### **2. URP (Universal Render Pipeline) Settings:**

Si usas URP (que es tu caso), también necesitas optimizar el Renderer:

1. Buscar tu archivo `UniversalRenderPipelineAsset` en Project
   - Probablemente en `Assets/Settings/` o similar
   - Hay uno para "Mobile" y otro para "PC"

2. **Para el asset que usa WebGL (Mobile):**

```
Rendering:
  Renderer: Universal Renderer
  
Shadow Settings:
  Max Distance: 0 (desactivado)
  Cascade Count: 0
  
Lighting:
  Main Light: Pixel (si es necesario) o Disabled
  Additional Lights: Disabled
  
Quality:
  HDR: OFF (si no lo necesitas)
  MSAA: Off
  Render Scale: 1.0
```

### **3. Desactivar Sombras en Todas las Luces:**

Si WebGLOptimizer no funciona correctamente, puedes usar el método manual desde el script:

1. Seleccionar el GameObject con `WebGLOptimizer`
2. En Inspector, hacer clic derecho en el componente
3. Seleccionar `Apply WebGL Optimizations Now`
4. Esto llamará a `DisableAllLightShadows()` también

---

## 🧪 TESTING

### **Después de implementar WebGLOptimizer:**

1. **Hacer WebGL Build**
2. **Abrir Console del navegador** (F12)
3. **Buscar logs del WebGLOptimizer:**

```
[WebGLOptimizer] Aplicando optimizaciones para WebGL...
[WebGLOptimizer] ✓ Sombras desactivadas
[WebGLOptimizer] ✓ Luces limitadas a 0 pixel lights
[WebGLOptimizer] ✓ Frame rate limitado a 60 FPS
[WebGLOptimizer] ✓ Anti-aliasing desactivado
[WebGLOptimizer] ✓ VSync desactivado
```

4. **Verificar FPS** - debería estar steady en ~60 FPS ahora

### **Si TODAVÍA hay problemas:**

El script tiene métodos de diagnóstico:

```csharp
// En el Inspector, click derecho en WebGLOptimizer → "Show Current Quality Settings"
// Esto mostrará en Console todos los settings actuales
```

---

## 📊 MEJORAS ESPERADAS

Con estas optimizaciones deberías ver:

| Aspecto | Antes | Después | Mejora |
|---------|-------|---------|--------|
| **FPS (idle)** | 10-20 FPS | 55-60 FPS | **3-6x mejora** |
| **FPS (gameplay)** | 5-15 FPS | 40-60 FPS | **4-8x mejora** |
| **Tiempo de frame** | 50-100ms | 16-20ms | **~80% reducción** |
| **Draw calls** | Alto | Medio | Reducción por sombras |

---

## 🔥 SI TODAVÍA ESTÁ LENTO DESPUÉS DE ESTO

Significa que el problema es **otra cosa**, probablemente:

### **1. Demasiados NavMeshAgents activos:**
   - Reducir `maxPathLength` en NavMeshAgent
   - Reducir update frequency: `agent.updatePosition = false` y actualizar manualmente cada N frames

### **2. Demasiados Draw Calls:**
   - Activar Static Batching en objetos que no se mueven
   - Combinar meshes similares
   - Usar un atlas de texturas

### **3. Shaders complejos:**
   - Verificar que uses shaders del tipo "Mobile" o "Simple"
   - Evitar shaders con muchos `if` statements o loops
   - Usar URP/Lit (Mobile) en lugar de Standard

### **4. Resolución muy alta:**
   - En Build Settings > Player Settings > Resolution
   - Set Default Canvas Height: 720 o 900 (no 1080)
   - Disable "Run in Background"

### **5. Physics settings:**
   - `Edit > Project Settings > Physics`
   - Fixed Timestep: 0.02 o incluso 0.03 (reduce updates)
   - Default Solver Iterations: 4 (default 6)

---

## 📝 RESUMEN DE ARCHIVOS MODIFICADOS

1. ✅ **NUEVO**: `Assets/Scripts/Core/WebGLOptimizer.cs`
   - Optimizador automático para WebGL
   - DefaultExecutionOrder(-1000)
   
2. ✅ **MODIFICADO**: `Assets/Scripts/Player/PlayerController.cs`
   - Cachea animator.speed
   - Solo actualiza cuando cambia el value

3. ✅ **MODIFICADO** (anteriormente): 
   - `AutoAttackSystem.cs` - Physics optimizado
   - `UpgradeDatabase.cs` - LINQ eliminado
   - `ExperienceUI.cs` - String caching

---

## ⚡ PRÓXIMO PASO INMEDIATO

1. **Agregar WebGLOptimizer a tu escena principal**
2. **Hacer WebGL build**
3. **Testear en navegador**
4. **Reportar resultados**

Si después de esto SIGUE injugable, necesitamos profiling más profundo (F12 > Performance tab en Chrome/Firefox).

---

**NOTA**: Las sombras son el problema #1 en WebGL. Desactivarlas puede hacer tu juego visualmente diferente, pero es necesario para que sea jugable. Considera usar "baked shadows" (lightmapping) si necesitas sombras estáticas.
