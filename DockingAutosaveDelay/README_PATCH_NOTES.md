## Release 3.1.0 - 2026-03-18
- Added the new Docking Autosave Delay mod.
- Delayed periodic autosaves by 60 seconds whenever the docking UI is active.
- Continued postponing autosaves in one-minute increments until docking mode ends.
- Left manual saves and save-on-exit behavior unchanged by patching the autosave scheduler instead of the save serializer.
- Added durable repo notes for the autosave scheduler and docking-mode hook point.
