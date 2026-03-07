# GAME_DATA_CHANGES

## What FFU-BR Changes
`FFU-BR` does not primarily add its own gameplay JSON content. Instead, it changes how data is parsed, merged, referenced, and synchronized.

## Data Handling Changes
1. Enables partial overwrite of existing JSON entries without replacing full objects.
2. Adds reference-copy semantics through `strReference` across many DTO types.
3. Adds per-mod selective removal (`removeIds`) by registry and ID.
4. Adds save/template migration map support (`changesMap`) for slotted items and condition sync/update workflows.
5. Adds safer/more tolerant JSON mapper behavior and loot parsing behavior.

## Registries Affected (Likely via patched `LoadMod` / `JsonToData`)
- Conditions / CondOwners / CondRules / CondTriggers
- Interactions / Interaction Overrides
- Items / Installables / Slots / Slot Effects
- Loot / Tickers / GUI prop maps
- Rooms / Ships / ShipSpecs / Star systems
- Careers / Traits / Homeworlds / Person specs
- Economy registries (ledgerdefs, market, cargospecs)
- UI and text registries (strings, info, tips, pda apps)

## Gameplay Impact Pattern
- Most gameplay impact comes from downstream data mods using FFU API extensions, not from FFU core adding standalone content by itself.
