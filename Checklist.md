# BeyondAllRoyal — Implementation Checklist

Derived from [Requirements.md](Requirements.md) and design decisions. Do not edit Requirements.md.

---

## Project Setup

- [x] Create Unity project (2D, mobile build target)
- [x] Folder structure (Scripts, Prefabs, Scenes, ScriptableObjects, UI, Maps)
- [x] Input System configured (new Input System package; touch primary, mouse fallback via InputHelper)

## Core Game Loop

- [x] Main game scene with fixed camera
- [x] Two-player symmetric layout in scene
- [x] Game state machine (Pregame → InGame → Victory/Defeat)
- [x] Win condition: detect HQ destruction, trigger end state

## Map System

- [x] Building slot grid — each side has ~40 slots (8 cols × 8 rows; back 5 rows reserved for HQ)
- [x] Buildings can occupy variable slot counts
- [x] Map layout loaded from ScriptableObject so layouts are swappable
- [x] Default map: two-lane Clash Royale-style layout

## Resources

- [x] Global Metal counter per player; income trickles over time
- [x] Per-building Energy buffer (fixed capacity, passive trickle fill rate)
- [x] Construction/production triggers only when required Energy has accumulated
- [x] Metal paid upfront when construction/production starts
- [x] Metal displayed in HUD
- [x] Energy buffer shown per selected building (script written; wire energyBar Slider in Inspector)
- [x] Shared minimum metal reserve (`ResourceManager.MinimumMetalReserve`) settable via a HUD slider; enforced against unit-production spending for both sides (`TrySpendMetalAboveReserve`) and used as a floor by the NPC's building-placement reserve — does not gate the player's own building costs

## Buildings

- [x] Base `Building` class (health, energy buffer, owner, slot size)
- [x] HQ building (win-condition target, metal + energy generation, self-defense auto-attack so a rush can't end the game instantly — default `attacksPerSecond` = 5; needs `attackDamage`/`attackRange`/`attacksPerSecond`/`energyCostPerShot` populated on the `HQData` asset; re-run `ProjectSetup` Step 1)
- [x] Base `ProductionBuilding` class (continuous production loop, start/stop)
- [x] 5 unit-production buildings (data + prefabs created)
- [x] Machinegun Turret (auto-attacks; counters Soldier, Heavy Gunner)
- [x] Railgun Turret (auto-attacks; counters Hovercraft, Heavy Tank)
- [x] Tesla Tower (injects energy into adjacent non-full buildings)
- [x] Metal Factory (passive metal income)
- [x] Defensive towers drain energy buffer on each shot; stop firing when empty
- [x] All buildings passively trickle-fill their own energy buffer
- [x] Building placement on slots — player input (BuildingPlacer + BuildingGhost)
- [x] NPC builds continuously from 3 types as metal allows

## Units

- [x] Base `Unit` class (health, damage, attack range, speed, owner)
- [x] Autonomous movement: target priority is buildings in attack range > units in attack range > buildings out of range > units out of range (units still fight back when attacked, not just when nothing else is around)
- [x] Auto-attack when in range (no player input)
- [x] Death / cleanup on health reaching zero
- [x] 5 unit types defined (Soldier, Heavy Gunner, Explosive Specialist, Hovercraft, Heavy Tank); default move speeds set to 1/4 of the original design values — re-run `ProjectSetup` Step 1 to update existing `UnitData` assets

## Counter System

- [x] Counter chart defined for 5 units + 2 towers
- [x] Combat damage modifier applied based on counter relationships

## NPC / AI

- [x] NPC resource accumulation (same rules as player)
- [x] NPC cycles through 3 building types, placing new instances only once metal has a surplus over a reserve threshold (previously it out-built its own economy and never had metal left to actually produce units)
- [x] NPC's metal reserve threshold scales with its own base: `(sum of metalCostPerUnit across active production buildings) * metalReserveMultiplier` (default 1.1) — grows automatically as more buildings go up, instead of a flat number
- [x] NPC fills free slots back-to-front (`MapGrid.TryGetFreeSlot` searches rows closest to its own HQ first), so new buildings tuck in behind existing ones instead of exposing themselves at the front line
- [x] NPC keeps all placed production buildings continuously producing
- [x] NPC occasionally places an economy building (Metal Factory, Tesla Tower, ...) instead of a production building — `economyBuildingTypes`/`economyBuildChance` (default 25%) on `NPCController`, still needs at least Metal Factory assigned in the Inspector
- [x] NPC's reserve threshold is floored by the shared `ResourceManager.MinimumMetalReserve` (set via the HUD slider), on top of its own dynamic per-building calculation
- [x] NPC forces an economy building through (ignoring the reserve) if `forceEconomyBuildAfterSeconds` (default 15s) passes without placing anything at all, so it can't get stuck never building

## UI / Mobile

- [x] Touch + mouse input (legacy Input API; migrate to Input System later)
- [x] Building shop panel (BuildingShopPanel.cs — needs Inspector wiring); closes automatically once a building is selected so it doesn't block the placement view
- [x] Building placement with ghost preview (BuildingPlacer + BuildingGhost)
- [x] Tap placed building to select it (BuildingSelector)
- [x] Production start/stop per selected building (HUD production panel)
- [x] Win/lose screen (HUD.ShowEndScreen — needs UI GameObject in scene)
- [x] Metal cost shown during placement (placementInfoPanel in HUD; wire in Inspector)

## Feedback / Polish

- [x] Health bar on all units and buildings (red → green gradient, auto-generated by ProjectSetup)
- [x] Production progress bar on production buildings (blue, auto-generated by ProjectSetup)
- [x] Cancel placement with Escape or right-click
- [x] Placeholder sprite art for all units and buildings (procedurally generated, `Assets/Sprites/`; higher native resolution for multi-tile buildings so they stay crisp when stretched)
- [x] Buildings cycle between two sprite frames on a timer (`Building.spriteCycleInterval`); same sprite doubles as the build-menu icon
- [x] Units swap to a brief "shooting" sprite on attack, then revert to idle
- [x] Every attack (units, towers, HQ) draws a placeholder beam from attacker to target, blue for the player and red for the NPC (`AttackBeamSpawner`, `Assets/Scripts/Effects/`) — explicitly a placeholder, swap out later
- [ ] Wire `BuildingShopPanel.ShopEntry.icon` to each shop button — automated via `BeyondAllRoyal → 4 - Auto-Wire Shop Icons` (assigns each button's own Image component), still needs to actually be run + scene saved in the Editor
- [ ] Minimum-metal-reserve HUD slider — automated via `BeyondAllRoyal → 5 - Create Minimum Reserve Slider` (creates + wires an unstyled `Slider`/label under HUD's Canvas), still needs to actually be run, then repositioned/styled and the scene saved

## Performance

- [x] UnitRegistry — O(1) unit lookup, replaces FindObjectsByType per frame
- [x] BuildingRegistry — O(1) building lookup, replaces FindObjectsByType per frame

## Multiplayer (Post-MVP)

- [ ] Networking layer
- [ ] Lobby with map selection
- [ ] 1v1 online matchmaking

## Version Control

- [x] Unity-appropriate `.gitignore` (Library/Temp/Build artifacts, IDE cruft, APKs, keystores)
- [x] README with setup steps and project layout
- [x] Pre-commit hook scanning staged changes for leaked credentials/tokens (`.githooks/pre-commit`; enable via `git config core.hooksPath .githooks`)
- [x] `git init` / first commit / remote ([Adyrem/BeyondAllRoyal](https://github.com/Adyrem/BeyondAllRoyal), public)
