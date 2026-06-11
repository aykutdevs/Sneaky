# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**Sneaky** — a Unity 2022.3.35f1 LTS stealth/possession game. The player controls a ghost who can possess objects to hide from security AI guards, using ghost mode and distractions as abilities.

## Development Workflow

Open `Sneaky.sln` in Visual Studio (requires the "Managed Game" workload) or use any C# IDE. All gameplay logic is in `Assets/Scripts/`. Changes to `.cs` files are compiled by the Unity Editor when it regains focus — there is no separate build step for scripts.

The main playable scene is `Assets/Scenes/SampleScene/SampleScene.unity`. It is **not** added to `EditorBuildSettings` — add it manually before making a standalone build.

## Architecture

All scripts are in `Assets/Scripts/`. The system has these interacting components:

### Core Loop
1. **`PlayerController.cs`** — Rigidbody first-person movement (WASD + mouse look). The player's physical body; replaced by `PossessManager` as the active controller when possessing.
2. **`PossessManager.cs`** — Central possession mechanic. Manages raycasting to find `PossessableObject` targets (15 m), smooth camera transitions between bodies, visibility toggling of the original ghost body, and speed delegation to the target's `speedMultiplier`.
3. **`PossessableObject.cs`** — Marker component on any possessable object. Exposes `speedMultiplier` and `suspicionRadius` (read by `SecurityAI`).

### Abilities
4. **`GhostModeManager.cs`** — Z key. 5 s active / 10 s cooldown. Doubles speed, narrows FOV, and sets `isGhostModeActive = true`, which `SecurityAI` polls to instantly break pursuit.
5. **`DistractionAbility.cs`** — F key. Spawns a physics capsule at throw velocity 15 m/s; after 1.5 s impact it expands to a 15 m noise radius and calls `SecurityAI.HearNoise()` on all guards in scene.

### AI
6. **`SecurityAI.cs`** — NavMeshAgent patrol AI. Cycles waypoints; switches to Investigate (orange light, 4 s rotation) on sound, or Chase (red light) on visual detection. Vision cone: 90°, 15 m. Hearing: 25 m. Stationary possessed objects pass as scenery. Loses the player immediately when `GhostModeManager.isGhostModeActive` is true.

### Key Interactions
- `PossessManager` disables `PlayerController` and makes the original body kinematic while possessing; restores on Q.
- `DistractionAbility` ignores collisions with the player and all possessed objects to prevent clipping.
- `SecurityAI` checks both `PossessableObject.suspicionRadius` and whether the possessed object is currently moving to decide whether to trigger detection.

## Packages
- `com.unity.ai.navigation` 1.1.7 — NavMesh baking and `NavMeshAgent` (required for `SecurityAI`)
- `com.unity.recorder` 4.0.3 — in-editor screen recording
- `com.unity.textmeshpro` 3.0.6 — UI text
- `com.unity.timeline` 1.7.6 — cutscene/timeline support
