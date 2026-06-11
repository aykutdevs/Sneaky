# Sneaky

A stealth game prototype built in Unity where the player controls a ghost that can possess objects to evade security guards.

## Gameplay

The player starts as an invisible ghost and must navigate a guarded environment by possessing objects, triggering distractions, and using a temporary ghost mode to avoid detection.

| Key | Action |
|-----|--------|
| WASD | Move |
| Mouse | Look |
| E | Possess nearby object |
| Q | Return to ghost body |
| F | Throw distraction |
| Z | Activate Ghost Mode |

## Features

- **Possession system** — possess any tagged object; the camera smoothly transitions to a third-person view of the possessed object. Speed is inherited from the object's properties.
- **Security AI** — guards patrol waypoints using Unity NavMesh. They detect the player via a 90° vision cone (15 m) with raycasting and a 25 m hearing radius. Stationary possessed objects appear as normal scenery.
- **Ghost Mode** — 5-second ability (10 s cooldown) that doubles speed and immediately breaks AI pursuit.
- **Distraction ability** — thrown projectile that lands and emits a noise pulse in a 15 m radius, pulling nearby guards away.

## Technical Details

- **Engine**: Unity 2022.3.35f1 LTS
- **Language**: C# (.NET 4.7.1)
- **AI**: Unity AI Navigation (NavMesh) with custom FSM (patrol → investigate → chase)
- **Physics**: Rigidbody-based movement, physics projectiles, raycast line-of-sight checks

## Project Structure

```
Assets/
├── Scripts/
│   ├── PlayerController.cs      # Rigidbody first-person movement
│   ├── PossessManager.cs        # Possession mechanic & camera transitions
│   ├── PossessableObject.cs     # Marker component for possessable objects
│   ├── GhostModeManager.cs      # Ghost Mode ability
│   ├── SecurityAI.cs            # Patrol/investigate/chase AI (NavMeshAgent)
│   └── DistractionAbility.cs   # Throw & noise-pulse distraction
├── Scenes/
│   └── SampleScene.unity        # Main scene with baked NavMesh
└── Materials/
```

## Setup

1. Open the project in **Unity 2022.3.35f1 LTS** (or newer 2022.3.x patch).
2. Open `Assets/Scenes/SampleScene.unity`.
3. Press **Play**.
