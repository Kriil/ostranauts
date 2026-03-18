## Docking Autosave Delay

Delays periodic autosaves while the docking UI is active.

### Status

Working first pass. The mod delays only the periodic BeatManager autosave cadence and leaves manual saves, save-and-quit, and other direct save calls alone.

### What It Does

When the autosave timer expires during docking mode, the mod pushes the timer back by 60 seconds instead of allowing the autosave popup and save job to start.

If the player is still docking when that extra minute expires, the timer is pushed back by another 60 seconds. This repeats until docking mode is no longer active, at which point the next elapsed autosave window proceeds normally.

### Configuration

There are no user-facing settings in the first version.

### Known Limitations

This mod keys off the active docking UI state (`GUIDockSys.instance && GUIDockSys.instance.bActive`). If the game gains a separate non-UI docking flow later, that path would need its own check.

### Troubleshooting

The BepInEx log will include messages tagged with `Docking_Autosave_Delay` for:

- plugin startup
- Harmony patch application
- each periodic autosave delay decision while docking mode remains active

### Technical Notes

The patch point is a Harmony prefix on `BeatManager.Update(double fTimeElapsed)`. Rather than blocking `LoadManager.AutoSave(...)`, the mod adjusts the private `fAutosaveRemain` timer before vanilla decrements it. That keeps the vanilla autosave popup, log message, and save job from starting until docking mode ends.
