---
paths:
  - "**/*.tscn"
  - "**/*.tres"
  - "**/*.unity"
  - "**/*.prefab"
  - "Assets/**/*.asset"
---

# Scene and Asset File Rules

The engine's editor writes these files. A hand edit that breaks one can stop a
scene loading, or drop references without an error. Change them in the editor
when you can.

## Unity (`.unity`, `.prefab`, `.asset`)

- Never hand-edit `.unity`, `.prefab` or `.asset` YAML while the Unity Editor has
  this project open (check `tasklist` for `Unity.exe`; "Unity Hub.exe" alone is fine).
  The Editor holds its own copy, and its next save can overwrite or conflict with the
  edit. Make the change through the Editor, or hand the user the steps. If you cannot
  tell whether the Editor is open, treat it as open.
- Prefer an editor tool over a text edit: changes that the project's tools own are
  made in the tool and applied from its menu (`Tools > Manners > UI` for what
  `UIStyleApplier` writes, `Tools > Manners > Sandbox > 4` for sandbox values,
  `Tools > Manners > Visual toon` for materials and renderers). If a tool does not
  carry a change, fix the tool.
- With the Editor closed, keep every `fileID` and `guid` reference intact, never
  invent a GUID, never edit or delete a `.meta` file, and load the scene afterwards
  (batch mode or the Editor) to confirm it opens without errors.
- Balance lives in `_Production` / `_Sandbox` ScriptableObjects: experiments go to the
  `_Sandbox` copies and `Sandbox.unity`; never delete and paste assets between them.
- `MilitaryBase.unity` and the `L2*` prefabs belong to the paused Map 2: do not open,
  edit or re-save them.
- Batch runs dirty some files that are not real changes (TMP font assets,
  `ProjectSettings/TimeManager.asset`, `ShaderGraphSettings.asset`, the minimap render
  texture): revert them with `git checkout --` before handing over.
