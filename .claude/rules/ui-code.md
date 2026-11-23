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
- All player-visible text goes through `LocalizedText` (fixed labels) or `LocalizedString` (dynamic formats) in English and Spanish. The text lives in a serialized field of the object that shows it, so the user changes it in the Inspector without opening code; never a string assigned from code at runtime. The starting language comes from `PlayerSettingsDefaults_Production`. Check both languages with the captures and `UIAuthoringTests`
- Every value the user may want to tweak (text, color, size, position, timing, default) is a serialized field on the scene object, prefab or ScriptableObject that uses it
- The visual hierarchy is authored in scenes/prefabs with serialized references: never built at runtime, never found by name. Persistent notices come from `RuntimeUIPrefabs_Production`
- Reuse the existing visual language (palette from the art document, parallelogram plates, stripes, Orbitron, the A.I. face, "?"); what the style writes into scenes and prefabs changes in `UIStyleApplier` and is applied with `Tools > Manners > UI`
- Claude applies UI changes itself: it runs the tools, scripts or edits in the worktree copy and copies the resulting scenes and prefabs into the project. The user is never handed tool steps to run, and no new `Tools` menu is created for a one-off change. Tools never overwrite what the user already adjusted in the Inspector
- Buttons and text boxes have fixed sizes: a changed text must still fit (font, size, auto-size); run the overlap audit with the captures
- Continuous animations register in `UpdateManager`; transitions use DOTween or coroutines; anything that must run in pause uses unscaled time (as `HoldToSelectButton` does)
- UI sounds trigger through the audio managers, not directly from widgets
- UI must never block the game thread
- Player-facing text never mentions secret override combinations
- Test screens at 1920×1080 and the extremes the project supports (Retina/4K, ultrawide 3440×1440; the event demo also 3840×1600 and 5120×2160)
- Keyboard/mouse is the input; gamepad navigation, colorblind modes and scalable text apply only when `accessibility.target` or a design doc asks for them
