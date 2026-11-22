---
paths:
  - "assets/shaders/**"
  - "**/*.gdshader"
  - "Assets/**/*.{shader,hlsl,cginc,compute,shadergraph}"
  - "Shaders/**"
  - "**/*.{usf,ush}"
---

# Shader Code Standards

manners.exe renders with URP 17.3 and targets WebGL 2 first, then desktop.
Shaders live in `Assets/Shaders/` (toon pipeline in `Assets/Shaders/Toon/`).

## Naming Conventions
- File and shader names follow the existing ones: PascalCase, named after the effect
  (`CryoField.shader`, `LaserBeam.shader`, `WarningStripe.shader`); the shader path
  matches the folder
- Renaming a shader breaks the materials that use it: keep the `.meta` (GUID) and
  update the scripts that look it up by name (the toon tools use shader names)

## Code Quality
- All properties have descriptive names and Inspector labels; group related ones
  with property attributes (`[Header]`, `[Toggle]`, `[KeywordEnum]`)
- No magic numbers — use named properties or constants
- The project's shaders carry no comments; intent goes in names and in the effect's
  config ScriptableObject (`CryoFieldConfig`, `LaserBeamConfig`, `EmpPulseConfig`)
- Values that designers tune live in the effect's config asset, not in the material

## Performance Requirements
- WebGL 2 is the budget: no compute shaders or features WebGL 2 lacks in anything the
  web build uses; check with `-force-gles30` in batch mode, the closest renderer on
  this machine
- Use appropriate precision: `half` where full precision isn't needed
- Minimize texture samples in fragment shaders
- Avoid dynamic branching in fragment shaders — use `step()`, `smoothstep()` and `lerp()`
- No texture reads inside loops
- Two-pass approach for blur effects (horizontal then vertical)
- Effects that hundreds of enemies or bullets use must keep GPU instancing and the
  SRP Batcher working

## Cross-Platform
- Test shaders on the WebGL build and on desktop
- Provide fallback/simplified versions for the Eco graphics preset where needed
- Every shader targets URP; do not mix shaders from different render pipelines

## Variant Management
- Minimize shader variants — each variant is a separate compiled shader and adds to the WebGL download
- Document keywords in the effect's config or the design doc, not in the shader
- Use feature stripping where possible to reduce build size
- `Tools > Manners > Visual toon > 7. Validar configuración toon` checks the toon setup
