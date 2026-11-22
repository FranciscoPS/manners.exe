---
paths:
  - "src/ui/**"
  - "Assets/**/{UI,Ui,ui}/**/*.cs"
  - "Source/**/{UI,Ui,ui}/**/*.{h,cpp}"
  - "Assets/Scripts/{Menus,Localization}/**/*.cs"
---

# UI Code Rules

manners.exe uses uGUI with TextMesh Pro, designed in scenes and prefabs, styled by
the `UIStyle_Production` kit. Guide: `Docs/UI_AUTHORING.md`.

- UI must NEVER own or directly modify game state — display only; requests go through `GameEvents` or the owning manager
- All player-visible text goes through `LocalizedText` (fixed labels) or `LocalizedString` (dynamic formats): English by default, Spanish editable in the Inspector. Check both languages with the captures and `UIAuthoringTests`
- The visual hierarchy is authored in scenes/prefabs with serialized references: never built at runtime, never found by name. Persistent notices come from `RuntimeUIPrefabs_Production`
- Reuse the existing visual language (palette from the art document, parallelogram plates, stripes, Orbitron, the A.I. face, "?"); what the style writes into scenes and prefabs changes in `UIStyleApplier` and is applied with `Tools > Manners > UI`
- Buttons and text boxes have fixed sizes: a changed text must still fit (font, size, auto-size); run the overlap audit with the captures
- Continuous animations register in `UpdateManager`; transitions use DOTween or coroutines; anything that must run in pause uses unscaled time (as `HoldToSelectButton` does)
- UI sounds trigger through the audio managers, not directly from widgets
- UI must never block the game thread
- Player-facing text never mentions secret override combinations
- Test screens at 1920×1080 and the extremes the project supports (Retina/4K, ultrawide 3440×1440; the event demo also 3840×1600 and 5120×2160)
- Keyboard/mouse is the input; gamepad navigation, colorblind modes and scalable text apply only when `accessibility.target` or a design doc asks for them
