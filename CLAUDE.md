# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Working with Unity Files

**Do not read or edit Unity binary/YAML asset files** (`.unity` scenes, `.prefab` prefabs, `.asset` ScriptableObjects, `.mat` materials, etc.) unless the user explicitly asks. These files are managed by the Unity Editor — edits made outside the Editor corrupt the asset database or break references. Only edit `.cs` C# scripts and plain text files (`.md`, `.json`, etc.).

## Project Overview

**BeyondAllRoyal** is a mobile-first 2D RTS game built in **Unity**, inspired by Beyond All Reason and Clash Royale. Futuristic theme.

- **Win condition:** Destroy the enemy HQ
- **Game mode (MVP):** 1v1 vs NPC; multiplayer planned post-MVP
- **Unit behavior:** Fully autonomous — units auto-move toward enemies and the enemy HQ; no micro required
- **Production:** Unit-producing buildings continuously produce units unless manually stopped
- **Perspective:** Fixed camera, 2D (mobile-first, performance-conscious)

## Resources

**Metal** — global pool shared across all buildings. Paid upfront when construction or unit production begins.

**Energy** — per-building buffer with a fixed maximum capacity. Every building passively trickles energy into its own buffer at the same base rate (configurable). Construction and unit production complete only once the required energy has accumulated — there is no fixed build timer; speed is entirely determined by energy fill rate.

The **Tesla Tower** is a support building that injects energy into all adjacent buildings whose buffers are not full, directly accelerating their construction and production. Defensive towers (Machinegun Turret, Railgun Turret) also drain their buffer each time they fire — a tower with an empty buffer cannot shoot.

## Map

- Layout is configurable via lobby settings
- **Default:** Clash Royale-style, two lanes, symmetric
- Each side has space for ~40 building slots; buildings can occupy multiple slots
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

The MVP NPC continuously produces 3 pre-selected unit types at equal rates. No strategic decision-making.

---

## Unity Project Setup

1. Open Unity Hub → New Project → **2D (URP)** → set location to this folder
2. Unity generates `Packages/` and `ProjectSettings/` — all scripts are already in `Assets/Scripts/`
3. In the scene, create GameObjects for `GameManager`, `ResourceManager`, `MapGrid`, `HUD`, `NPCController`
4. Run the `Assets/Scripts/Editor/ProjectSetup.cs` menu items in order — these generate and wire almost everything else:
   - `BeyondAllRoyal → 1 - Create ScriptableObjects` (stats, counter chart, game settings, map layout)
   - `BeyondAllRoyal → 2 - Create Prefabs (run step 1 first)` (unit/building prefabs)
   - `BeyondAllRoyal → 3 - Import Sprites and Assign to Data + Prefabs` (imports `Assets/Sprites/**` and assigns them to `UnitData`/`BuildingData` and prefab `SpriteRenderer`s)
5. Assign the generated `GameSettings` asset to `GameManager` in the Inspector (everything else `ProjectSetup` created is already cross-referenced)

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
| `HQData : BuildingData` | Adds metal income + energy injection (HQ acts as a weak Tesla Tower) |
| `CounterChartData` | Flat matrix of `CounterResult`, sized from `EntityType`'s enum length; right-click → "Initialize Default Counter Chart" |
| `MapLayoutData` | List of slot definitions (grid position + world position + owner side) |

### Building hierarchy (`Scripts/Buildings/`)

`Building` is the base class. It owns the **energy buffer**, the **construction tick** (energy drains from buffer until `energyCostToBuild` is met), `TakeDamage`, the **grid origin** it was placed at (`GridOrigin`, set by `MapGrid.TryPlaceBuilding`, freed via `MapGrid.RemoveBuilding` on destruction), and the two-frame **sprite cycle** (`data.spriteFrameA`/`spriteFrameB`, swapped every `data.spriteCycleInterval` seconds — also shared as the build-menu icon). Subclasses override `Update` and call `base.Update()`.

- `ProductionBuilding` — reserves metal upfront, then drains energy until one unit's `energyCostPerUnit` is reached, then spawns the unit
- `DefenseTower` — scans for nearest enemy unit each frame, fires when in range if energy buffer allows
- `TeslaTower` / `HQ` — both inject energy into nearby friendly buildings via the shared `Building.InjectEnergyIntoNearby(rate, range)` helper
- `MetalFactory` — adds metal to `ResourceManager` each frame when constructed
- `HQ` — combines MetalFactory + TeslaTower behaviour; calls `GameManager.OnHQDestroyed` when killed

### Unit (`Scripts/Units/`)

`Unit` holds stats, health, and its `idleSprite`/`shootingSprite` (the latter flashes briefly via `FlashShootingSprite()` whenever `UnitAI` fires). `UnitAI` (required component) drives behaviour: find nearest enemy unit → chase and attack; if none, advance toward enemy HQ. Counter multipliers are applied via `CounterSystem.GetDamageMultiplier`.

### Map (`Scripts/Map/`)

`MapGrid` (singleton) instantiates `BuildingSlot` objects from a `MapLayoutData` SO. `TryPlaceBuilding` validates slot ownership and occupancy before marking slots as occupied.

### Key singletons

`GameManager`, `ResourceManager`, `MapGrid`, `HUD` — all follow the standard Unity singleton pattern (destroy duplicate on `Awake`).

## Version Control

Git hooks are versioned in `.githooks/` (not `.git/hooks/`), enabled per clone via `git config core.hooksPath .githooks`. `.githooks/pre-commit` scans staged changes for likely leaked credentials/tokens and blocks the commit if it finds one — see [README.md](README.md#git-hooks) for details and the bypass escape hatch.
