# BeyondAllRoyal

A mobile-first 2D RTS built in Unity, inspired by *Beyond All Reason* and *Clash Royale*, with a futuristic theme. Units move and fight autonomously — no micro required. Destroy the enemy HQ to win.

For the full game design (resources, map, units, buildings), see [CLAUDE.md](CLAUDE.md), [Requirements.md](Requirements.md), and [UnitDesign.md](UnitDesign.md). For current implementation status, see [Checklist.md](Checklist.md).

## Requirements

- Unity **6000.4.5f1** (see `ProjectSettings/ProjectVersion.txt`)
- 2D (URP) template

## Getting started

1. Clone the repo and open it in Unity Hub (it will offer to install the matching editor version if you don't have it).
2. Enable the versioned git hooks once per clone (see [Git hooks](#git-hooks) below).
3. Open the main scene and run the one-time content setup from the Unity menu, in order:
   - `BeyondAllRoyal → 1 - Setup Project Assets` — creates/wires ScriptableObjects, prefabs, and sprites (no scene needed)
   - `BeyondAllRoyal → 2 - Wire Scene` — run once `GameManager`/`HUD`/`MapGrid`/`BuildingShopPanel`/`NPCController` exist in-scene; populates the shop panel, minimum-reserve slider, Cancel/Demolish/Restart buttons, and the NPC's building pool
   - `BeyondAllRoyal → 3 - Create Main Menu Scene` — builds the standalone `MainMenu` scene from scratch and registers it as build index 0; can be run any time after step 1
   - `BeyondAllRoyal → 4 - Apply Dark Purple Theme to Play Scene` — recolors `PlayScene`'s HUD/shop panel to match `MainMenu`'s theme; re-runnable any time, unlike steps 2/3
4. Wire the remaining scene references by hand (HUD panels, `MapGrid.slotPrefab`, shop button icons, etc. — see the checklist for what's still manual).

## Project layout

```
Assets/Scripts/
  Core/       GameManager, ResourceManager, MapGrid registries, settings, input helpers
  Data/       ScriptableObjects — all unit/building stats live here, never hard-coded
  Buildings/  Building base class + subclasses (production, towers, Tesla, HQ, ...)
  Units/      Unit + UnitAI (autonomous move/attack)
  Map/        MapGrid, BuildingSlot
  AI/         NPCController (MVP opponent)
  UI/         HUD, shop panel, health bars, main menu controller
  Editor/     ProjectSetup/MainMenuSetup/ThemeSetup — generate and wire ScriptableObjects, prefabs,
              sprites, and both scenes' UI, run from the Unity menu; UITheme holds the shared palette
```

Architecture details (data-driven stats, singleton conventions, building/unit hierarchy) are documented in [CLAUDE.md](CLAUDE.md).

## Git hooks

A pre-commit hook scans staged changes for likely leaked credentials (API keys, private keys, Android/iOS signing files, `.env` files, generic secret-looking assignments) and blocks the commit if it finds one. It's checked into `.githooks/` instead of `.git/hooks/` so it travels with the repo — enable it once per clone with:

```sh
git config core.hooksPath .githooks
```

A blocked commit that's a genuine false positive can be bypassed with `git commit --no-verify`, but treat that as the exception, not the default.

## Notes for contributors

- Don't hand-edit Unity binary/YAML assets (`.unity`, `.prefab`, `.asset`, `.mat`) outside the Editor — it corrupts references. Only `.cs` and plain text files are safe to edit directly.
- All gameplay stats live in ScriptableObjects (`Assets/Scripts/Data/`); MonoBehaviours should read from them, never hard-code values.
- `Assets/Scripts/Editor/ProjectSetup.cs` is the source of truth for how ScriptableObjects, prefabs, and sprites are generated/wired — re-run its menu items after pulling changes that touch unit/building data or sprites. `MainMenuSetup.cs` and `ThemeSetup.cs` do the same for the `MainMenu` scene and the dark-purple theme, respectively.
