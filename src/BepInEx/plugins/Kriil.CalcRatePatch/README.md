# Kriil CalcRate Patch

This is an uncompiled BepInEx plugin source scaffold for Ostranauts.

What it does:
- Adds a Harmony transpiler for `Interaction.CalcRate()`.
- Targets only the clamp applied to `fCTThemModifierUs`.
- Replaces the original `Mathf.Clamp(value, min, max)` call with a hook that can inspect and modify the clamp inputs before calling the real `Mathf.Clamp`.

Why this approach:
- It avoids replacing the whole `CalcRate()` method.
- It is more compatible with mods like FFU_BR, because it only patches the specific clamp block instead of copying the entire method body.

Current example behavior:
- If the interaction is in the `"Work"` action group, the hook raises the clamp max to at least `25f`.

Notes:
- These are source files only. They will not load in-game until you compile them into a DLL.
- After compiling, place the compiled DLL in the real game's `BepInEx/plugins` folder.
- The plugin expects references to `BepInEx`, `0Harmony`, the game's `Assembly-CSharp`, and UnityEngine assemblies when you build it.
