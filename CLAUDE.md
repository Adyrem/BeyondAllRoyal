# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Working with Unity Files

**Do not read or edit Unity binary/YAML asset files** (`.unity` scenes, `.prefab` prefabs, `.asset` ScriptableObjects, `.mat` materials, etc.) unless the user explicitly asks. These files are managed by the Unity Editor — edits made outside the Editor corrupt the asset database or break references. Only edit `.cs` C# scripts and plain text files (`.md`, `.json`, etc.).

## Project Overview

**BeyondAllRoyal** is a mobile-first 2D RTS game built in **Unity**, inspired by Beyond All Reason and Clash Royale. Futuristic theme.

- **Win condition:** Destroy the enemy HQ; a Restart button on the end screen (`GameManager.RestartGame()`) reloads the scene for a rematch
- **Game mode (MVP):** 1v1 vs NPC; multiplayer planned post-MVP. A `MainMenu` scene (build index 0) lets the player pick Singleplayer or Multiplayer (Multiplayer is a disabled placeholder button) and an AI difficulty (Easy/Medium/Hard) before loading `PlayScene`
- **Unit behavior:** Fully autonomous — target priority is enemy units that have pushed onto our own side of the map > buildings in attack range > units in attack range > buildings out of range > units out of range; no micro required
- **Production:** Unit-producing buildings continuously produce units unless manually stopped
- **Perspective:** Fixed camera, 2D (mobile-first, performance-conscious)

## Resources

**Metal** — global pool shared across all buildings. Paid upfront when construction or unit production begins.

**Energy** — per-building buffer with a fixed maximum capacity. Every building passively trickles energy into its own buffer at the same base rate (configurable). Construction and unit production complete only once the required energy has accumulated — there is no fixed build timer; speed is entirely determined by energy fill rate.

The **Tesla Tower** is a support building that injects energy into all adjacent buildings whose buffers are not full, directly accelerating their construction and production. Defensive towers (Machinegun Turret, Railgun Turret) also drain their buffer each time they fire — a tower with an empty buffer cannot shoot.

A shared **minimum metal reserve** (`ResourceManager.MinimumMetalReserve`, adjustable via a HUD slider) sets a floor below which metal shouldn't be spent. It's enforced for unit production on both sides via `ResourceManager.TrySpendMetalAboveReserve` (used by `ProductionBuilding` instead of the plain `TrySpendMetal`), and used as a floor by the NPC's own building-placement reserve (see NPC section) — it does not gate the player's own building-placement costs.

## Map

- Layout is configurable via lobby settings
- **Default:** Clash Royale-style, two lanes, symmetric
- Each side has space for ~63 building slots (9×8 grid minus the HQ's own 3×3 footprint); buildings can occupy multiple slots. The grid is 9 columns wide (odd) specifically so the HQ's 3-wide footprint centers exactly (`MapGrid.PlaceHQs`'s `(columns - hqSize) / 2` needs an odd/odd or even/even pairing to land on a whole number without drifting off-center)
- Map layout is data-driven so different configurations can be swapped in

## Units & Counter System

There are **5 units**, each produced by its own dedicated building. The counter chart is a circulant tournament: every unit counters exactly 2 others and is countered by exactly 2 others. The two tower types are also included in the chart — each tower counters 2 unit types and is weak to 2 others, with Unit C being neutral against both towers. See [UnitDesign.md](UnitDesign.md) for the full matrix and stat templates.

Unit names and stats are TBD (fill in UnitDesign.md).

## Buildings

- **HQ** — win-condition target; must be destroyed to win
- **5 unit-production buildings** — one per unit type; continuously produce their unit when active
- **2 tower structures** — defensive buildings that auto-attack nearby enemy units; each counters a different subset of units
- Buildings occupy a variable number of slots on the map grid

## NPC (MVP)

At the start of each match, the NPC is randomly assigned 3 of the 5 production building types (`NPCController.AssignRandomBuildingTypes`, picked from the full pool in `allProductionBuildingTypes`) and continuously produces them at equal rates — so the AI's army composition varies between matches instead of always being the same fixed 3. No strategic decision-making beyond that. It only places a new production building once metal exceeds the build cost by a reserve margin — that reserve is `max(ResourceManager.MinimumMetalReserve, sum of metalCostPerUnit across all active production buildings * metalReserveMultiplier)` (multiplier default 1.1), so it automatically grows as more buildings go up instead of being a flat number, keeping enough metal free to actually fund unit production, while never dropping below the shared minimum. It also fills free slots back-to-front (`MapGrid.TryGetFreeSlot` searches rows near its own HQ first), so new buildings tuck in behind existing ones instead of exposing themselves at the front line. Each placement check it has a chance (`economyBuildChance`, default 25%) to place from a separate `economyBuildingTypes` list (e.g. Metal Factory, Tesla Tower) instead of a production building — and if `forceEconomyBuildAfterSeconds` (default 15s) passes without placing anything at all, it forces an economy building through regardless of the reserve, so it can't stall forever.

The `AIDifficulty` picked on the main menu (`GameSetup.Difficulty`) scales all of the above pacing knobs at `Awake` (`NPCController.ApplyDifficulty`) — Easy checks less often and keeps a bigger metal-reserve margin before spending, Hard checks more often and spends closer to the edge — rather than changing unit/building stats, which stay symmetric between Player and NPC.

---

## Unity Project Setup

1. Open Unity Hub → New Project → **2D (URP)** → set location to this folder
2. Unity generates `Packages/` and `ProjectSettings/` — all scripts are already in `Assets/Scripts/`
3. In the scene, create GameObjects for `GameManager`, `ResourceManager`, `MapGrid`, `HUD`, `NPCController`, and add a `BuildingShopPanel` under HUD's Canvas
4. Run `Assets/Scripts/Editor/ProjectSetup.cs`'s two menu items, in order:
   - `BeyondAllRoyal → 1 - Setup Project Assets` — no scene needed; creates/wires ScriptableObjects, prefabs, and sprites (internally: ScriptableObjects → Prefabs → Sprites)
   - `BeyondAllRoyal → 2 - Wire Scene` — run once the GameObjects from step 3 exist; populates the shop panel with one button per placeable building, backfills any missing shop icons, creates the minimum-reserve slider under HUD's Canvas, adds a Cancel button under `HUD.placementInfoPanel`, a Demolish button under `HUD.buildingInfoPanel`, a Restart button under `HUD.endScreen` (also registers the current scene in Build Settings, required for `GameManager.RestartGame()`'s scene reload to work), and populates `NPCController.allProductionBuildingTypes` with all 5 production buildings (internally: Populate Shop Panel → Auto-Wire Shop Icons → Create Minimum Reserve Slider → Create Cancel Placement Button → Create Demolish Button → Create Restart Button → Auto-Wire NPC Building Pool). Reposition/style the new UI as needed, then save the scene.
5. Assign the generated `GameSettings` asset to `GameManager` in the Inspector (everything else `ProjectSetup` created is already cross-referenced)
6. Run `Assets/Scripts/Editor/MainMenuSetup.cs`'s `BeyondAllRoyal → 3 - Create Main Menu Scene` — builds a standalone `MainMenu` scene from scratch (Camera, EventSystem, Canvas, title, AI difficulty dropdown, Singleplayer/Multiplayer buttons) and registers it as build index 0. Can be run any time after step 1 (doesn't depend on `PlayScene`'s GameObjects). Reposition/style the UI as needed, then save the scene.
7. Run `Assets/Scripts/Editor/ThemeSetup.cs`'s `BeyondAllRoyal → 4 - Apply Dark Purple Theme to Play Scene` once `PlayScene`'s HUD/BuildingShopPanel are wired — recolors HUD's buttons/text/panel backgrounds/sliders and the shop panel to `UITheme`'s dark-purple palette (the same palette `MainMenuSetup` uses for `MainMenu`). Unlike Step 2/3, this force-reapplies colors to whatever's already referenced, so it's safe to re-run any time (e.g. after tweaking `UITheme`) without deleting/recreating anything first.
8. Run `Assets/Scripts/Editor/TestSceneSetup.cs`'s `BeyondAllRoyal → 5 - Create Test Scene` once `PlayScene` is fully set up and saved — duplicates it to a new `TestScene` (via `AssetDatabase.CopyAsset`, so it inherits every manually-wired reference for free) and adds a `TestSceneBootstrap` that pre-places an economy-heavy starter loadout (`StarterBuildingNames`: 5x Metal Factory, 3x Tesla Tower, 1x Barracks, 1x GunRange) for both sides once the match starts, for quick testing without building an economy up from scratch. Registers `TestScene` in Build Settings; open it directly and hit Play to use it. Unlike Step 3, re-running this once `TestScene` already exists doesn't recreate it — it just re-wires the bootstrap's loadout to match whatever `StarterBuildingNames` currently says, so it's safe to re-run any time the list is tuned.

## Code Architecture

All runtime data lives in **ScriptableObjects** (`Assets/Scripts/Data/`). MonoBehaviours read from SOs and never hard-code stats.

### Data layer (`Scripts/Data/`)

| Class | Purpose |
|---|---|
| `UnitData` | Stats + prefab for one unit type |
| `BuildingData` | Base stats for any building |
| `ProductionBuildingData : BuildingData` | Adds the unit type this building produces |
| `TowerData : BuildingData` | Adds attack stats and `EntityType` for counter lookups |
| `TeslaTowerData : BuildingData` | Adds injection rate and range |
| `MetalFactoryData : BuildingData` | Adds metal-per-second income |
| `HQData : BuildingData` | Adds metal income + energy injection (HQ acts as a weak Tesla Tower) + a self-defense attack (flat damage, no counter multiplier) |
| `CounterChartData` | Flat matrix of `CounterResult`, sized from `EntityType`'s enum length; right-click → "Initialize Default Counter Chart" |
| `MapLayoutData` | List of slot definitions (grid position + world position + owner side) |

### Building hierarchy (`Scripts/Buildings/`)

`Building` is the base class. It owns the **energy buffer**, the **construction tick** (energy drains from buffer until `energyCostToBuild` is met), `TakeDamage`, the **grid origin** it was placed at (`GridOrigin`, set by `MapGrid.TryPlaceBuilding`, freed via `MapGrid.RemoveBuilding` on destruction), and the two-frame **sprite cycle** (`data.spriteFrameA`/`spriteFrameB`, swapped every `data.spriteCycleInterval` seconds — also shared as the build-menu icon). `Demolish()` lets the player voluntarily remove a building they own (wired to a HUD button, hidden for HQ); `HQ` overrides it to refuse, since the HQ can only be lost in combat. Subclasses override `Update` and call `base.Update()`.

- `ProductionBuilding` — reserves metal upfront, then lets the energy buffer accumulate from the passive trickle until it reaches one unit's `energyCostPerUnit`, then consumes that much in one go and spawns the unit — it used to drain a little every single frame as energy trickled in, which kept the buffer (and its progress bar) pinned near zero the whole time instead of visibly filling. Its progress bar shows the raw energy buffer relative to capacity (not just progress toward the current unit, so it doesn't freeze while paused or waiting on a metal reservation), with an `Indicator` tick (`HealthBar.SetIndicator`) marking the energy threshold for one unit
- `DefenseTower` — scans for nearest enemy unit each frame (via `Building.FindNearestEnemyUnitInRange`), fires when in range if energy buffer allows. Every attack (`DefenseTower`, `HQ`, and `UnitAI`) draws a placeholder `AttackBeamSpawner` line from attacker to target — blue for the player, red for the NPC (`Assets/Scripts/Effects/`) — swap its internals for a fancier effect later without touching call sites
- `TeslaTower` / `HQ` — both inject energy into nearby friendly buildings via the shared `Building.InjectEnergyIntoNearby(rate, range)` helper
- `MetalFactory` — adds metal to `ResourceManager` each frame when constructed
- `HQ` — combines MetalFactory + TeslaTower behaviour, plus its own `DefenseTower`-like auto-attack (also via `FindNearestEnemyUnitInRange`) so a fast rush can't end the game before real defenses go up; calls `GameManager.OnHQDestroyed` when killed

### Unit (`Scripts/Units/`)

`Unit` holds stats, health, and its `idleSprite`/`shootingSprite` (the latter flashes briefly via `FlashShootingSprite()` whenever `UnitAI` fires). `UnitAI` also calls `Unit.PlayShootSfx()` on every shot — the same `GameSettings.unitShootSfx` clip for all units, but pitched by the shooting unit's own `maxHealth` (`shootPitchReferenceMinHealth`/`MaxHealth` map to `shootPitchForMinHealth`/`MaxHealth`), so bigger units sound deeper and smaller ones sound higher — same "based on maxHealth" pattern as the death explosion's damage/radius. `UnitAI` (required component) picks a target each frame by priority: nearest enemy unit that has crossed onto our own side of the map (via `MapGrid.IsOnSide`), else nearest enemy building in attack range, else nearest enemy unit in attack range, else nearest enemy building (move toward it), else nearest enemy unit (move toward it). Counter multipliers are applied via `CounterSystem.GetDamageMultiplier`. Against a building specifically, a unit keeps advancing between shots (buildings don't move, so there's no risk of overrunning a retreating target) instead of holding at the range boundary, stopping only once it reaches the building's own footprint edge (`UnitAI.BuildingStoppingDistance`, approximated as a circle from `BuildingData.slotSize`) — unit-vs-unit combat is unaffected and still holds at range.

On death, `Unit.Explode()` deals flat splash damage (no counter multiplier, like HQ's self-defense) to every unit within a radius — both friendly and enemy — but only to enemy buildings, so a unit's death can't hurt its own team's structures. Damage and radius both scale off the dying unit's own `maxHealth` (`GameSettings.explosionDamageFraction`/`explosionRadiusPerHealth`), so tankier units go out with a bigger bang; `ExplosionSpawner` plays a placeholder expanding-ring effect (`ExplosionEffect`, a procedural annulus sprite that scales up and fades out) + `GameSettings.explosionSfx`. Both registries (`UnitRegistry`/`BuildingRegistry`) are snapshotted to a fresh `List` before iterating, since the damage dealt can kill other units/buildings whose own death unregisters them — mutating the very list being iterated (and potentially chaining into further explosions). `Unit` guards against re-entrant death (`isDead`) since a chained explosion can otherwise hit an already-dying unit again before `Destroy` has actually removed it, re-entering `Die()` → `Explode()` indefinitely and crashing Unity with a stack overflow instead of just ending the match.

### Map (`Scripts/Map/`)

`MapGrid` (singleton) instantiates `BuildingSlot` objects from a `MapLayoutData` SO. `TryPlaceBuilding` validates slot ownership and occupancy before marking slots as occupied. `GetPlacementOrigin(worldPos, owner, size)` converts a tap/cursor position to a footprint origin centered as closely as possible on that position (not anchored at one corner), clamped to stay on the grid — used by `BuildingPlacer` so the ghost (and the eventual placed building) span the cells actually centered under the cursor.

### Key singletons

`GameManager`, `ResourceManager`, `MapGrid`, `HUD` — all follow the standard Unity singleton pattern (destroy duplicate on `Awake`). `GameManager.RestartGame()` reloads the active scene, which resets every MonoBehaviour singleton for free (none are `DontDestroyOnLoad`); it also explicitly clears `BuildingRegistry`/`UnitRegistry`, since those are plain static lists that a scene reload alone wouldn't touch.

### Main menu (`Scripts/UI/MainMenuController.cs`, `Scripts/Core/GameSetup.cs`)

`MainMenuController` lives in the `MainMenu` scene (build index 0): its Singleplayer button writes the chosen `AIDifficulty` (from a dropdown) and `GameMode.Singleplayer` to `GameSetup`, then loads `PlayScene`; its Multiplayer button is present but `interactable = false` (not implemented). `GameSetup` is a plain static class (like `BuildingRegistry`/`UnitRegistry`) rather than a `DontDestroyOnLoad` singleton, since it only needs to survive the one scene transition — it defaults to `Singleplayer`/`Medium` so `PlayScene` can still be tested directly in the Editor without going through the menu first. `NPCController` reads `GameSetup.Difficulty` at `Awake`.

### UI theme (`Scripts/Editor/UITheme.cs`)

A dark-purple color palette shared by `MainMenuSetup` (`MainMenu`) and `ThemeSetup` (`PlayScene`), so both scenes are styled from one source of truth instead of each keeping its own copy. Buttons/dropdowns are recolored via `Selectable.colors` (`UITheme.ApplyButtonColors`), not by setting the `Image` color directly — Unity's ColorTint transition overwrites `Image.color` with `colors.normalColor` on the first state change, so a direct color set gets silently clobbered at runtime.

## Version Control

Git hooks are versioned in `.githooks/` (not `.git/hooks/`), enabled per clone via `git config core.hooksPath .githooks`. `.githooks/pre-commit` scans staged changes for likely leaked credentials/tokens and blocks the commit if it finds one — see [README.md](README.md#git-hooks) for details and the bypass escape hatch.
