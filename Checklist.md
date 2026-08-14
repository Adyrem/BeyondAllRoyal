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
- [x] Return to the main menu after a win/loss — `GameManager.ReturnToMainMenu()` loads the `MainMenu` scene (also explicitly clears `BuildingRegistry`/`UnitRegistry`, since those are plain static lists a scene load wouldn't otherwise touch, so a later match doesn't start with stale entries); wired to a Main Menu button on the end screen via `BeyondAllRoyal → 2 - Setup Scenes`, which also registers the scene in Build Settings (required for the load) — still needs to actually be run + scene saved. Previously this restarted `PlayScene` directly instead of going through the menu

## Main Menu

- [x] `MainMenu` scene (build index 0) with a title, an AI difficulty dropdown (Easy/Medium/Hard), a Singleplayer button, and a disabled Multiplayer placeholder button (`MainMenuController.cs`) — built from scratch (Camera, EventSystem with `InputSystemUIInputModule`, Canvas) by `BeyondAllRoyal → 2 - Setup Scenes` (`MainMenuSetup.CreateMainMenuScene`, only creates it once — delete it first to regenerate); still needs to actually be run + scene saved
- [x] Singleplayer button writes the chosen difficulty/mode to `GameSetup` (a plain static class, not a `DontDestroyOnLoad` singleton — only needs to survive the one scene load) and loads `PlayScene`; defaults to Singleplayer/Medium so `PlayScene` can still be tested directly in the Editor without going through the menu
- [ ] Multiplayer — button present but non-interactive; not implemented (see Multiplayer (Post-MVP))
- [x] Dark purple UI theme (`UITheme.cs`) applied to `MainMenu` (baked in by `MainMenuSetup`) and `PlayScene`'s HUD/shop panel as part of `BeyondAllRoyal → 2 - Setup Scenes` (`ThemeSetup.ApplyPlaySceneTheme`) — re-applied every time that step runs, since it force-recolors whatever HUD/BuildingShopPanel already reference instead of skipping already-wired UI; still needs to actually be run + scene saved

## Testing

- [x] `TestScene` — a duplicate of `PlayScene` (via `AssetDatabase.CopyAsset`, so it inherits every manually-wired reference for free) created as part of `BeyondAllRoyal → 2 - Setup Scenes` (`TestSceneSetup.CreateTestScene`), with a `TestSceneBootstrap` component that pre-places a starter loadout (5x Metal Factory, 3x Tesla Tower, 1x Barracks, 1x GunRange — economy-heavy so testing doesn't stall waiting on metal) for both Player and NPC once the match starts (free, not paid for through `ResourceManager`, same as how `MapGrid.PlaceHQs` places the HQ) — registered in Build Settings; open it directly and hit Play instead of building an economy up from scratch every time. Re-running that step once `TestScene` already exists just refreshes the loadout in place (re-reads `StarterBuildingNames`) instead of recreating the scene, so it's safe to re-run any time the list is tuned

## Map System

- [x] Building slot grid — each side has ~63 free slots (9 cols × 8 rows; back 3 rows reserved for HQ, 3×3); columns changed from 8 to 9 so the HQ's 3-wide footprint centers exactly instead of sitting one slot off-center — re-run `ProjectSetup` Step 1 to update the existing `DefaultMap` asset
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
- [x] HQ building (win-condition target, metal + energy generation, self-defense auto-attack so a rush can't end the game instantly — default `attacksPerSecond` = 5/3 (≈1.67), cut by a factor of 3 from the original 5 since it was firing way too fast; needs `attackDamage`/`attackRange`/`attacksPerSecond`/`energyCostPerShot` populated on the `HQData` asset; re-run `ProjectSetup` Step 1)
- [x] Base `ProductionBuilding` class (continuous production loop, start/stop)
- [x] 5 unit-production buildings (data + prefabs created)
- [x] Machinegun Turret (auto-attacks; counters Soldier, Heavy Gunner)
- [x] Railgun Turret (auto-attacks; counters Hovercraft, Heavy Tank)
- [x] Tesla Tower (injects energy into adjacent non-full buildings)
- [x] Metal Factory (passive metal income)
- [x] Defensive towers drain energy buffer on each shot; stop firing when empty
- [x] All buildings passively trickle-fill their own energy buffer
- [x] Building placement on slots — player input (BuildingPlacer + BuildingGhost); ghost/placement footprint is centered on the tapped cell (`MapGrid.GetPlacementOrigin`) instead of treating it as the top-left corner, so it now visually spans exactly the cells that will be affected; `BuildingGhost` forces a full-bleed solid sprite (not whatever "badge"-style icon sprite happened to be assigned in the Inspector, which had lots of transparent padding and made the ghost look tiny), forces `SpriteRenderer.drawMode = Simple`, and detaches from any scene parent on `Awake`, so its size can't be thrown off by scene-side Inspector state
- [x] Fixed the ghost defaulting to the bottom-left slot on mobile whenever the player wasn't actively touching: `InputHelper.TapPosition()` falls back to screen `(0,0)` when nothing is pressed and there's no mouse hardware (the normal case on a phone), and `BuildingPlacer` was updating/showing the ghost every frame regardless of touch state. It now only shows the ghost while a finger/mouse button is held (`InputHelper.IsPressed()`), hides it otherwise, and places on release (`InputHelper.TapEnded()`) instead of on initial touch — you drag to preview a slot, lift to commit, matching how there's no such thing as "hovering" without touching on a touchscreen
- [x] Opening the shop panel always cancels any in-progress building placement (`HUD.ToggleShop`), so a previously-selected building doesn't linger as a ghost underneath the menu; opening the shop also deselects any selected building (and selecting a building closes the shop, `HUD.OnBuildingSelected`), so the shop and building-info panels can't be open at the same time competing for screen space
- [x] Fixed selecting a building from the shop instantly placing it at whatever the release happened to land on: closing the shop panel (part of the same click) removed the UI element that would otherwise have blocked `BuildingPlacer`'s place-on-release check, so the very release that triggered the shop button's own `onClick` was immediately reinterpreted as a "place here" release. `BuildingPlacer` now ignores all input after a shop selection until the player begins a brand-new press (`awaitingFreshPress`), so a placement can never fire from the same press/release cycle that selected the building
- [x] Dragging a placement ghost onto the enemy's side of the map (`MapGrid.IsOnSide`) shows no ghost at all instead of an invalid/red one, and releasing there cancels the whole placement instead of silently doing nothing
- [x] Shop buttons are only interactable if the player can currently afford them (`BuildingShopPanel.Update`, checked against `ResourceManager.PlayerMetal` every frame the shop is open); Unity's built-in disabled-button tint greys them out and blocks the click, so unaffordable buildings can't be selected at all
- [x] NPC builds continuously from a randomly-assigned set of 3 production types as metal allows (see NPC / AI)
- [x] Any building except HQ can be voluntarily demolished to free its slot (`Building.Demolish()`, HUD Demolish button shown when a non-HQ building is selected; `HQ.Demolish()` refuses as a second line of defense) — still needs `BeyondAllRoyal → 2 - Setup Scenes` run + scene saved
- [x] Fixed Demolish/Pause Production appearing not to work: `BuildingSelector`/`BuildingPlacer` never checked whether a tap landed on a UI element, so clicking those HUD buttons also registered as a world-space tap and immediately deselected the building right after the action ran. Both now skip via `InputHelper.TapHitInteractiveUI()` when the tap hit an interactive element (Button/Slider) specifically — an initial blanket `IsPointerOverGameObject()` check caused a regression where the info panel couldn't be closed at all for production buildings (they show an extra `productionPanel`, covering enough screen area that a "tap elsewhere to deselect" often landed on that panel's own background and got swallowed too); the precise Selectable-only check lets taps on passive panel backgrounds still reach the world
- [x] Pause/Resume Production button is now explicitly hidden for anything that isn't a `ProductionBuilding` (Tesla Tower, Metal Factory, towers) instead of only relying on the parent panel's visibility

## Units

- [x] Base `Unit` class (health, damage, attack range, speed, owner)
- [x] Autonomous movement: target priority is enemy units on our own side of the map (home defense) > buildings in attack range > units in attack range > buildings out of range > units out of range (units still fight back when attacked, not just when nothing else is around)
- [x] When assaulting a building, units keep advancing between attacks instead of holding at the range boundary — stationary buildings can't be overrun by mistake, so there's no downside to closing the distance; they stop once they reach the building's own footprint edge (`UnitAI.BuildingStoppingDistance`). Unit-vs-unit combat still holds at range, unchanged
- [x] Auto-attack when in range (no player input)
- [x] Shoot sound effect on every attack (`Unit.PlayShootSfx`, `GameSettings.unitShootSfx`) — same clip for all units, pitched by the shooting unit's own `maxHealth` so bigger units sound deeper and smaller ones sound higher (`shootPitchReferenceMinHealth`/`MaxHealth` → `shootPitchForMinHealth`/`MaxHealth`, defaults 50/350 → 1.3/0.7) — since audio can't be procedurally generated like the sprites, `ProjectSetup` Step 1 auto-wires it from `Assets/Resources/shot.mp3` by convention if present (leaves it alone otherwise, so a manual Inspector assignment isn't clobbered); re-run Step 1 to seed the pitch fields and pick up the clip
- [x] Death / cleanup on health reaching zero
- [x] Death explosion (`Unit.Explode`): flat splash damage (no counter multiplier) to all units in range — friendly and enemy — but only to enemy buildings; damage/radius scale with the dying unit's own `maxHealth` via `GameSettings.explosionDamageFraction`/`explosionRadiusPerHealth` (defaults 0.3, 0.01); plays a placeholder expanding ring (`ExplosionSpawner`/`ExplosionEffect`) and `GameSettings.explosionSfx`, auto-wired by `ProjectSetup` Step 1 from `Assets/Resources/splosion.mp3` by convention if present — re-run Step 1 to seed the damage/radius fields and pick up the clip
- [x] Fixed a Unity (not just game) crash from unit explosions: a chain reaction could hit an already-dying unit again before `Destroy` removed it, re-entering `Die()` → `Explode()` indefinitely until the stack overflowed — most likely to happen right as a match ends and a cluster of units die together. `Unit` now guards with `isDead` so a unit can only die once
- [x] 5 unit types defined (Soldier, Heavy Gunner, Explosive Specialist, Hovercraft, Heavy Tank); default move speeds set to 1/4 of the original design values — re-run `ProjectSetup` Step 1 to update existing `UnitData` assets
- [x] Unit metal costs lowered and energy costs (build time) raised a bit across the board — metal is the tighter bottleneck since it's a shared pool across all production buildings, energy isn't — re-run `ProjectSetup` Step 1 to update existing `UnitData` assets

## Counter System

- [x] Counter chart defined for 5 units + 2 towers
- [x] Combat damage modifier applied based on counter relationships

## NPC / AI

- [x] NPC resource accumulation (same rules as player)
- [x] NPC is randomly assigned 3 of the 5 production building types at match start (`NPCController.AssignRandomBuildingTypes`, picked from `allProductionBuildingTypes` — auto-wired with all 5 by `BeyondAllRoyal → 2 - Setup Scenes`), and cycles through them, placing new instances only once metal has a surplus over a reserve threshold (previously it out-built its own economy and never had metal left to actually produce units)
- [x] NPC's metal reserve threshold scales with its own base: `(sum of metalCostPerUnit across active production buildings) * metalReserveMultiplier` (default 1.1) — grows automatically as more buildings go up, instead of a flat number
- [x] NPC fills free slots back-to-front (`MapGrid.TryGetFreeSlot` searches rows closest to its own HQ first), so new buildings tuck in behind existing ones instead of exposing themselves at the front line
- [x] NPC keeps all placed production buildings continuously producing
- [x] NPC occasionally places an economy building (Metal Factory, Tesla Tower, ...) instead of a production building — `economyBuildingTypes`/`economyBuildChance` (default 25%) on `NPCController`, still needs at least Metal Factory assigned in the Inspector
- [x] NPC's reserve threshold is floored by the shared `ResourceManager.MinimumMetalReserve` (set via the HUD slider), on top of its own dynamic per-building calculation
- [x] NPC forces an economy building through (ignoring the reserve) if `forceEconomyBuildAfterSeconds` (default 15s) passes without placing anything at all, so it can't get stuck never building
- [x] AI difficulty (chosen on the main menu, `GameSetup.Difficulty`) scales the NPC's economy pacing — `NPCController.ApplyDifficulty` multiplies `placementCheckInterval`/`metalReserveMultiplier`/`economyBuildChance`/`forceEconomyBuildAfterSeconds` by an Easy/Medium/Hard factor at `Awake`; unit/building stats stay symmetric between Player and NPC

## UI / Mobile

- [x] Touch + mouse input (legacy Input API; migrate to Input System later)
- [x] Building shop panel (BuildingShopPanel.cs); closes automatically once a building is selected so it doesn't block the placement view; entries (one button per placeable building) auto-generated via `BeyondAllRoyal → 2 - Setup Scenes` — still needs to actually be run + scene saved in the Editor
- [x] Building placement with ghost preview (BuildingPlacer + BuildingGhost)
- [x] Tap placed building to select it (BuildingSelector)
- [x] Production start/stop per selected building (HUD production panel)
- [x] Win/lose screen (HUD.ShowEndScreen — needs UI GameObject in scene) with a Main Menu button (see Core Game Loop)
- [x] Metal cost shown during placement (placementInfoPanel in HUD; wire in Inspector)

## Feedback / Polish

- [x] Health bar on all units and buildings (red → green gradient, auto-generated by ProjectSetup)
- [x] Production bar on production buildings shows the raw energy buffer relative to its capacity (not just progress toward the current unit, which used to freeze while paused or waiting on a metal reservation, and could jump instantly when there was already a surplus stored), with a yellow indicator tick marking the energy threshold needed for one unit (`HealthBar.SetIndicator`) — re-run `ProjectSetup` Step 1 to backfill the indicator onto already-created production building prefabs. Also fixed the bar being frozen at its stale prefab default (full bar, indicator at 100%) for the entire construction phase — it was gated behind `IsConstructed`, so it only got its first real update the instant construction finished, snapping down and looking exactly like a unit had just been produced (none was)
- [x] Fixed the energy bar only ever appearing to fill when energy income outpaced metal income: `TickProduction` was draining a little energy out of the buffer every single frame as it trickled in, instead of letting the buffer accumulate up to `energyCostPerUnit` first and consuming it all at once — same total production time, but now the bar actually visibly fills toward the indicator instead of sitting pinned near zero
- [x] Cancel placement with Escape/right-click (desktop) or the new Cancel button under `placementInfoPanel` (touch — Escape/right-click don't exist on mobile, so placement was previously uncancelable there; automated via `BeyondAllRoyal → 2 - Setup Scenes`, still needs to actually be run + scene saved)
- [x] Placeholder sprite art for all units and buildings (procedurally generated, `Assets/Sprites/`; higher native resolution for multi-tile buildings so they stay crisp when stretched)
- [x] Buildings cycle between two sprite frames on a timer (`Building.spriteCycleInterval`); same sprite doubles as the build-menu icon
- [x] Units swap to a brief "shooting" sprite on attack, then revert to idle
- [x] Every attack (units, towers, HQ) draws a placeholder beam from attacker to target, blue for the player and red for the NPC (`AttackBeamSpawner`, `Assets/Scripts/Effects/`) — explicitly a placeholder, swap out later
- [ ] Wire `BuildingShopPanel.ShopEntry.icon` to each shop button — `BeyondAllRoyal → 2 - Setup Scenes` sets it directly when creating each entry (and backfills any added by hand later); still needs to actually be run + scene saved in the Editor
- [ ] Minimum-metal-reserve HUD slider — automated via `BeyondAllRoyal → 2 - Setup Scenes` (creates + wires an unstyled `Slider`/label under HUD's Canvas), still needs to actually be run, then repositioned/styled and the scene saved

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
