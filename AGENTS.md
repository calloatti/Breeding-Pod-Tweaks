# Breeding Pod Tweaks — Mod-Specific AGENTS.md

## Build & Deploy
```powershell
cd "C:\Users\calloatti\source\repos\Mods\Breeding Pod Tweaks\Version-1.0"
dotnet build            # Debug: builds + deploys to live mods folder
dotnet build -c Release # Release: same deploy (deploy is config-agnostic)
```
- **Deploy target:** `%USERPROFILE%\Documents\Timberborn\Mods\Breeding Pod Tweaks\Version-1.0`
- Deploy destination is **computed by `prebuild.ps1`/`postbuild.ps1` from the csproj path**, not any config value. `prebuild.ps1` wipes both the local `bin` target dir and the deployed folder; `postbuild.ps1` copies `$(TargetDir)` over it.
- `prebuild.ps1` copies the deployed `workshop_data.json` to the repo root as a safety backup — that repo-root file is a build artifact, not source.
- Hardcoded references (build fails if absent): `TimberbornPath` = Steam game `Timberborn_Data\Managed` (in csproj); `0Harmony.dll` from Steam Workshop `...\workshop\content\1062090\3284904751\0Harmony.dll` (`CommonModSettings.props`).
- Game install: `C:\Program Files (x86)\Steam\steamapps\common\timberborn_main\Timberborn_Data\Managed`

## Mod Architecture
- **Entry point:** `Source\ModStarter.cs` → `IModStarter` → `new Harmony("Calloatti.BreedingPodTweaks").PatchAll()`
- **DI:** `Source\BreedingPodTweaksConfigurator.cs` → `AddDecorator<BreedingPod, BreedingPodTweaks>()` — no blueprints, attaches to vanilla BreedingPod
- **Core:** `Source\BreedingPodTweaks.cs` — `TickableComponent` (ticks every game tick) that blocks/unblocks the pod via `BlockableObject` and reimplements automation pause
- Vanilla `BreedingPod` obtained via `GetComponent<BreedingPod>()`; `CalculateProgress()` called **directly** (Reproduction DLL publicized) — **no reflection used**
- Patches `PausableBuildingTerminal.UpdateBlockable` / `OnPausedChanged` to suppress vanilla automation pause on pods

## Critical Pitfalls
- **Harmony patches are global.** `BlockableObject.Block/Unblock` and `PausableBuildingTerminal.UpdateBlockable/OnPausedChanged` are shared vanilla classes. Every prefix MUST early-return the original method (`return true`) when `GetComponent<BreedingPodTweaks>()` is null, else the mod breaks blocking/automation for **all buildings**. See `BlockableObjectPatch` / `PausableBuildingTerminalPatch`.
- **Target progress per cycle:** generated in `EnsureTargetProgressGenerated()` when `_targetProgress <= 0`: adopts current progress if already ≥ 0.90, else random `0.90–0.99`. No save/load — regenerated each `OnEnterFinishedState` (manifest: 90–99%).
- **Publicized DLLs** in this mod's csproj (all with `IncludeCompilerGeneratedMembers="false"`): `BlueprintSystem`, `BlockingSystem`, `Reproduction`, `Emptying`, `StatusSystem`. Shared publicizer config lives in `CommonModSettings.props` — never edit that file.

## Verification
- No automated tests. Build, then launch the game and check `%USERPROFILE%\AppData\LocalLow\Mechanistry\Timberborn\Player.log` for errors.

## Game Version
- Targets **Timberborn 1.0.x.x** (`Version-1.0` folder); decompiled source at `C:\Users\calloatti\source\repos\timberborn-decompiled-1.0.13.1-b769e88-sw`
