# BeyondAllRoyal

A mobile-first 2D RTS built in Unity, inspired by *Beyond All Reason* and *Clash Royale*, with a futuristic theme. Units move and fight autonomously — no micro required. Destroy the enemy HQ to win.

For the full game design (resources, map, units, buildings), see [CLAUDE.md](CLAUDE.md), [Requirements.md](Requirements.md), and [UnitDesign.md](UnitDesign.md). For current implementation status, see [Checklist.md](Checklist.md).

## Requirements

- Unity **6000.4.5f1** (see `ProjectSettings/ProjectVersion.txt`)
- 2D (URP) template

## Getting started

1. Clone the repo and open it in Unity Hub (it will offer to install the matching editor version if you don't have it).
2. Enable the versioned git hooks once per clone (see [Git hooks](#git-hooks) below).
3. Open `PlayScene` and run the one-time content setup from the Unity menu, in order:
   - `BeyondAllRoyal → 1 - Setup Project Assets` — creates/wires ScriptableObjects, prefabs, and sprites (no scene needed)
   - `BeyondAllRoyal → 2 - Setup Scenes` — run once `GameManager`/`HUD`/`MapGrid`/`BuildingShopPanel`/`NPCController` exist in `PlayScene` and it's the open scene. One consolidated step for everything scene-related: wires `PlayScene` (shop panel, minimum-reserve slider, Cancel/Demolish/Main Menu buttons, NPC building pool), applies the dark-purple theme, creates `MainMenu` if it doesn't exist yet, and creates/refreshes `TestScene` — then reopens `PlayScene`. Safe to re-run any time.
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
  Effects/    AttackBeamSpawner, ExplosionSpawner, ShootSfxSpawner — placeholder VFX/SFX
  Testing/    TestSceneBootstrap — pre-places starter buildings in TestScene
  Editor/     ProjectSetup's two menu items generate/wire ScriptableObjects, prefabs, sprites, and all
              three scenes' UI; MainMenuSetup/ThemeSetup/TestSceneSetup do the scene-specific work and
              are called from ProjectSetup, not run directly; UITheme holds the shared palette
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
- `Assets/Scripts/Editor/ProjectSetup.cs` is the source of truth for how ScriptableObjects, prefabs, sprites, and all scene UI are generated/wired — re-run its two menu items after pulling changes that touch unit/building data, sprites, or scene wiring. `MainMenuSetup.cs`, `ThemeSetup.cs`, and `TestSceneSetup.cs` do the `MainMenu`/theme/`TestScene`-specific parts of that but aren't runnable on their own — `ProjectSetup`'s `2 - Setup Scenes` calls all three.
