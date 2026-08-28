# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Rush Champions** (repo/project name `AdventureMultiplayer`) is a mobile multiplayer obstacle-racing game built in Unity 6 (`6000.3.17f1`, URP). Up to 4 players race through 50+ obstacle levels via Unity Relay; each of the 5 characters (Gale, Blaze, Bolt, Bruno, Spike) has one unique ability. See `Assets/MCP/Context/GameDesign.md` for the design doc.

The project is built on top of two third-party asset packages under `Assets/ThirdParty/`:
- **PLAYER TWO Platformer Project** (`Assets/ThirdParty/PLAYER TWO/`) — player movement/state machine, camera, and base environment components. See the **PLAYER TWO Package** rules below.
- **ithappy** (`Assets/ThirdParty/ithappy/`) — environment/obstacle art and scripts (e.g. `RotationScript`, `OscillateRotation`). See the **Rotating/Moving Obstacles** issues below before touching anything driven by these. Working copies of the animation scripts already live at `Assets/RC/Scripts/ObstacleAnimation/` (namespace `ithappy.rc`) per the duplicate-before-modifying rule — prefer editing those over the ones under `Assets/ThirdParty/`.

## Working in This Repo

This is a Unity Editor project — there is no CLI build, lint, or test command, and no CI workflow, and no automated test suite. All changes are made through the Unity Editor, or via Unity MCP, which exposes Editor operations (scene/prefab edits, console logs, play-mode control, menu items) as tools.

- The working Unity MCP server is **MCPForUnity** (Unity package `com.coplaydev.unity-mcp`, tool-visible as `UnityMCP`). The Editor self-registers it over HTTP (`http://127.0.0.1:8080/mcp`) into the user's `~/.claude.json` **project entry** whenever it's open — it is *not* configured via the checked-in `.mcp.json`. The `unity`, `blender`, and `mcp-unity` entries in `.mcp.json` are stale (paths under a nonexistent `C:/Users/Senthil/...` profile, and a `Library/PackageCache` hash for a package that's since been replaced by `com.coplaydev.unity-mcp`) — don't treat them as a description of what's actually running.
- If Unity MCP tools don't show up even though the Editor is open, the `~/.claude.json` registration is keyed by exact working-directory path and can end up scoped to the wrong folder (e.g. one directory above the real project root) — check that before assuming the bridge itself is down. A restart of Claude Code is needed after fixing the key, since MCP servers only load at session start.
- Don't drive Play mode yourself via MCP — repeated Play-mode calls hang the bridge. Ask the user to play-test manually and read results from the Editor log instead.
- Editor log path: `C:/Users/<username>/AppData/Local/Unity/Editor/Editor.log` (check `$env:USERNAME` if unsure which profile — on this machine it's `Admin`). Read the tail and look for the most recent `PLAY MODE` separator.

## Architecture

### Player & Ability System (`Assets/RC/Scripts/Player/`)
Per-character abilities subclass PLAYER TWO's `PlayerState` types directly rather than wrapping them — e.g. `BrunoRollingState : RollingPlayerState`, `SpikeHighJumpState : AirDivePlayerState`. The pattern is to override lifecycle hooks (`OnEnter`, `OnExit`, `HandleFriction`, `HandleDeceleration`, `HandleGroundTurning`, `HandleUncurl`, etc.) to change *behavior* of the existing state while reusing its transition plumbing — this is what the **PLAYER TWO Package** rule below means by "extend rather than write a new state system." Separate per-character cooldown components (`BrunoCooldown`, `SpikeCooldown`, `BlazeCooldown`) gate reuse; `ManaSystem` gates abilities via a 0–100 mana pool by swapping in a runtime-cloned `PlayerStats` (disabling `canGlide`/`canRun` flags) and exposes a one-shot "SuperCharge" bonus consumed by ability states/HUD. `PlayerBodyCollider` adds a second, non-trigger `CapsuleCollider` so players physically collide with each other, since PLAYER TWO's own `EntityController` collider is trigger-based. Each character's base movement tuning (speed, acceleration, jump height, dash, roll, wall-run, etc.) is data-driven via one PLAYER TWO `PlayerStats` ScriptableObject asset per character in `Assets/RC/Stats/` (`BlazeStats`, `BoltStats`, `BrunoStats`, `GaleStats`, `SpikeStats`) — this is the asset `ManaSystem` clones at runtime to gate abilities.

### Networking (`Assets/RC/Scripts/Network/`) — Netcode for GameObjects + Unity Relay
Player **movement is owner/client-authoritative**: `NetworkedMovementSync` writes the owner's position/velocity/rotation/state into one `NetworkVariable<PlayerNetworkState>` (write permission `Owner`) every frame; non-owner ghosts disable the PLAYER TWO `Player`/`EntityController` components, kinematic-ize the Rigidbody, and dead-reckon from the last synced velocity. `NetworkedPlayerSync` just disables `Player` on ghosts so PLAYER TWO's singleton `FindFirstObjectByType` lookups never grab a remote player. `ClientNetworkTransform` (`OnIsServerAuthoritative() => false`) exists as an alternative but the player prefab uses `NetworkedMovementSync` instead.

**Gameplay economy is server-authoritative** — power-up boxes, pickups, checkpoints, and respawns are all `NetworkBehaviour`s with server-write `NetworkVariable`s (see PowerUps below).

`LobbyManager` drives the join flow: Unity Services auth → Relay `CreateAllocationAsync`/`GetJoinCodeAsync` (host) or `JoinAllocationAsync` (client), all via UniTask — then a ready-up handshake over `CustomMessagingManager` named messages, plus level/character-tab UI and bot-count toggles. `CharacterPicker` is a `DontDestroyOnLoad` singleton tracking `clientId → character index` via custom messages, including late-joiner roster sync.

### AI Bots (`Assets/RC/Scripts/AI/`, `Assets/RC/Scripts/Training/`)
Bots reuse the human input pipeline: `AIPlayerInputManager : PlayerInputManager` (the same PLAYER TWO base class humans use) overrides the `Get*()` methods to return bot-set values instead of reading the Input System — the rest of the player controller doesn't know it's a bot. Three separate brains sit on top of it, and only one should be active on a given bot at a time: `AIBotBrain` is a NavMesh-steered FSM (Idle/WalkToCollectible/UseSpring/ClimbPole/UseRail/UsePortal/platform-boarding) used for single-player "AI mode" collectible scavenging; `RaceBotBrain` (server-only `NetworkBehaviour`) is the actual race AI — checkpoint-order steering with weave, braking, raycast avoidance, and stuck-recovery escalation, used in DeathRun races; `AIBotAgent : Agent` is a reinforcement-learning bot (Unity ML-Agents, package `com.unity.ml-agents`) with a 37-float observation vector and 2 continuous + 6 discrete actions, explicitly meant to replace `AIBotBrain` rather than run alongside it. It's trained via PPO (`ml-agents/config.yaml`, `mlagents-learn` CLI) inside a dedicated headless `TrainingScene` bootstrapped by `TrainingSceneBootstrap`/`TrainingLevelScore` (which stub out `LevelScore` since the training scene has no real `Level`); trained models are exported as `.onnx` and assigned to the bot's `BehaviourParameters` component.

### PowerUps (`Assets/RC/Scripts/PowerUp/`)
No shared interface — each effect is its own `NetworkBehaviour`/`MonoBehaviour`: `NetworkedPowerUpBox` (server-authoritative trigger box with a `NetworkVariable<bool>` active flag, respawns via UniTask delay), `BananaPeel`/`DecoyBoxPickup`/`RocketProjectile` (server-spawned hazards keyed by `NetworkObjectId` rather than `OwnerClientId` for owner-immunity, since bots can share `OwnerClientId` 0), `PlayerPowerUpInventory` (central per-player inventory + `ClientRpc` effect application), `InvisibleEffect`/`SlipEffect`/`StunEffect` (status-effect components), `PowerUpDefinition` (ScriptableObject data), `PowerUpEnums.cs` (`PowerUpType`, `PowerUpBoxType`, `PowerUpAffectOutcome`).

### Race Flow (`Assets/RC/Scripts/Race/`)
`RaceManager` (`NetworkBehaviour` singleton) is the server-authoritative race state machine: it owns `NetworkList<RaceEntry>`/`NetworkList<FinishRecord>`, computes live standings from each player's checkpoint index plus distance-to-next-checkpoint, and resolves finish/DNF/timeout — it also exposes targeting helpers (`GetPlayerAhead`, `GetPlayerInFirst`) that the PowerUp system calls into. `RaceCheckpoint` trigger volumes report crossings to the server via `ServerRpc` (deduplicated per owner) and update each player's respawn point; `RacePlayerTracker` is the per-player component feeding position data back to `RaceManager`. `RaceCountdown` drives a server-authoritative 3-2-1-Go `NetworkVariable` sequence via static events, with `NetworkPlayerInputLocker` freezing player input/physics until it completes. `BotSpawner` fills unused human slots with AI bots server-side at race start.

### GameMode, Obstacles & Platforms
`GameModeManager` toggles scene setup between `None`/`AI`/`Multiplayer`, auto-finding the human vs. AI player object by presence of `AIPlayerInputManager`. Custom obstacles beyond PLAYER TWO/ithappy live in `Assets/RC/Scripts/Obstacles/`: `TreadmillObstacle` (conveyor push force), `RotatingLogObstacle` (tangential push derived from `RotationScript`'s rotation), `JumpingPlatform`, `MushroomBounce`, `ObstacleKnockback`/`ObstacleKnockbackDatabase` (data-driven per-obstacle-type knockback/damage lookup — see `Assets/RC/ScriptableObject/ObstacleKnockbackDatabase.asset`), `RandomStoneFall`. Pure visual animation (no gameplay logic) is factored out into `Assets/RC/Scripts/ObstacleAnimation/` (`RotationScript`, `OscillateRotation`/`OscillatePosition`/`OscillateScale`, `BlendShapeAnimator`) — obstacle and platform scripts read the transform these drive rather than duplicating the motion. `DynamicPlatform : Platform` (`Assets/RC/Scripts/Platform/`) extends PLAYER TWO's platform-attachment base to carry standing players via position delta from these animators, deliberately skipping rotation delta so players don't tilt on spinning discs. Lobby/menu UI (`CharacterSelectUI`, `ModeSelectUI`, `UIHomingTargetDeferred`) lives in `Assets/RC/Scripts/UI/`, separate from the in-race `Assets/RC/Scripts/HUD/`.

### Editor Tooling (`Assets/RC/Scripts/Editor/`)
One-off automation, not runtime code: `RaceSceneBuilder` (menu-driven scene assembly), `SceneLightingSetup`, `BreakableNetworkSetup`/`CollectibleNetworkSetup`/`ItemBoxNetworkSetup` (bulk-add/remove NGO components across scene objects), `PlayerBodyColliderSetup`, `PrefabRootNormalizer` (centers an Environment hierarchy at the origin), `AIBotDebugWindow`.

## Component References
- Never use `AddComponent<T>()` in scripts. Assign components via the Inspector or find them in `Awake`/`Start`.

## Prefab Workflow
- **Always edit the prefab, never the scene instance directly.** Changes made to a prefab automatically propagate to every scene instance. Editing instances directly creates per-instance overrides that diverge from the prefab and break consistency.
- Use `PrefabUtility.LoadPrefabContents` / `SaveAsPrefabAsset` when editing prefabs via editor code.
- After fixing a prefab, revert any stale scene-instance overrides with `PrefabUtility.RevertAllObjectOverrides`.

## Field Visibility
- Use `[SerializeField] private` instead of `public` for Inspector-exposed fields.
- Only use `public` when the field must be accessed from other classes.

## Async / Coroutines
- See **UniTask** section below — always use UniTask, never coroutines or Task/async Task.

## Tweening / Animation
- See **DOTween** section below — always use DOTween, never manual Lerp loops in Update.

## Input
- Always use the **new Unity Input System** (`UnityEngine.InputSystem`). Never use legacy `Input.GetKey`, `Input.GetAxis`, etc.

## Logging
- Add `Debug.Log` / `Debug.LogWarning` / `Debug.LogError` whenever it helps diagnose a problem.
- Remove debug logs once the issue is resolved and they are no longer needed.

## PLAYER TWO Package
- Use the **PLAYER TWO** package (`Assets/ThirdParty/PLAYER TWO/`) as the foundation for all player-related systems and environment design.
- For player states: extend or override the existing state machine classes (e.g. `PlayerState`, `EntityState`) rather than writing a new state system from scratch.
- For environment design: reuse PLAYER TWO obstacle, platform, and hazard prefabs/components as the starting point; build custom obstacles on top of them.
- Never modify plugin scripts directly (anything under `Assets/ThirdParty/` or `Assets/Plugins/`). Duplicate into `Assets/RC/Scripts/` first, then modify the copy.

## Physics — Rigidbody vs Transform
- **Do NOT add a Rigidbody to static objects** (ground, walls, non-moving props). A Collider alone is correct — adding an unnecessary Rigidbody makes PhysX treat them as dynamic and wastes performance.
- Always move or rotate physics objects via **Rigidbody**, never via `transform.Rotate` / `transform.Translate` / `transform.position`.
- For objects driven by script (rotating platforms, moving obstacles, logs): add a **kinematic Rigidbody** (`isKinematic = true`, `useGravity = false`) and move/rotate using `rb.MovePosition()` / `rb.MoveRotation()`.
- Non-kinematic Rigidbodies should use `rb.AddForce()` / `rb.AddTorque()` or set `rb.velocity` / `rb.angularVelocity` rather than directly setting the transform.
- Any MeshCollider on a **moving or rotating** object (kinematic or dynamic Rigidbody) **must have `Convex = true`** — Unity's PhysX does not support non-convex MeshColliders on non-static objects. Static props, ground, and walls that never move can keep `Convex = false`.

## Colliders — Blender Proxy Meshes
- Every prefab in the scene must use a Blender-created MeshCollider proxy. No custom procedural collider scripts are allowed.
- Banned components: `FanCylinderCollider`, `TorusCollider`, `ConeCollider`, and any other script under `Assets/Plugins/CustomPrimitiveColliders/`. Remove them and replace with a MeshCollider.
- Proxy FBX files live in `Assets/RC/Meshes/` with the naming convention `<mesh_name>_collider.fbx`. Cylinder proxies use `cyl_r<radius>_h<height>_collider.fbx`.
- Blender export settings: `axis_forward='-Z'`, `axis_up='Y'`, `apply_scale_options='FBX_SCALE_ALL'`. After `transform_apply`, apply a -90° X rotation before export to correct the Unity↔Blender axis difference.
- When adding a new obstacle or prop prefab: import its FBX into Blender, fill open boundary edges (`edge_face_add`), apply the rotation fix, export the proxy, then assign it as a MeshCollider on the prefab. Never ship the prefab with a FanCylinder or other custom collider.

## DOTween
- Use **DOTween** for all programmatic tweening and animation sequences (movement, scale, fade, shake, etc.).
- Avoid manual `Lerp` loops in `Update` — a DOTween call (`.DOMove`, `.DOScale`, `.DOFade`, etc.) is always preferred.
- Keep tween setup simple: chain `.SetEase()`, `.SetDelay()`, `.OnComplete()` rather than writing custom animation coroutines.

## UniTask
- Use **UniTask** for every async operation. Never use `IEnumerator` / `StartCoroutine` / `Task` / `async Task`.
- Replace any existing coroutine with a `UniTask` or `UniTaskVoid` method using `await`.
- Use `await UniTask.Delay(ms)` instead of `yield return new WaitForSeconds(s)`.
- Use `await UniTask.WaitUntil(condition)` instead of `yield return new WaitUntil(condition)`.

---

## Common Issues (recurring bugs — check before building anything similar)

### Rotating/Moving Obstacles

**Issue: RotationScript + Rigidbody conflict**
- `ithappy.RotationScript` and `ithappy.OscillateRotation` rotate via `transform.Rotate` (not physics).
- A non-kinematic Rigidbody on the same object fights the script rotation every frame.
- **Fix:** Always set Rigidbody `isKinematic = true`, `useGravity = false` on any object driven by these scripts.

**Issue: MeshCollider on moving object not convex**
- Unity PhysX requires `Convex = true` on any MeshCollider attached to a moving/rotating object that has a Rigidbody.
- A non-convex MeshCollider + Rigidbody will throw a PhysX error and collisions will be unreliable.
- **Fix:** Always set `mc.convex = true` when the GameObject has a Rigidbody or is animated by a rotation/position script.

**Issue: Player stacking on respawn**
- Multiple players respawn at the exact same checkpoint position and clip through each other.
- **Fix:** Offset each player using `(OwnerClientId % 4) * 90°` arc at 1.5 units from the respawn point.

**Issue: Player input overwritten by obstacle push**
- Setting `player.lateralVelocity = push` replaces the player's own movement input, causing them to freeze mid-air.
- **Fix:** Use `player.lateralVelocity += push` (additive) to preserve the player's own input.

**Issue: OverlapSphere misses player on rotating log**
- Placing the OverlapSphere at `bounds.max.y` (top of collider) is too high and too small — misses the player capsule.
- **Fix:** Use `bounds.center` as sphere origin, and expand radius by `extents.y + 1.5f`.

### UI / Input

**Issue: Jump button also triggers UI button (Submit double-fire)**
- Unity EventSystem selects the last-touched UI button; any action bound to Submit (including Jump) re-activates it.
- **Fix:** Add `ClearUISelection` component to the HUD root (clears `EventSystem.currentSelectedGameObject` every frame). Also set `Navigation.Mode.None` on all buttons so they can't be keyboard/gamepad-selected.

**Issue: Mobile joystick interactive during countdown**
- The joystick canvas accepts touch input during the race countdown, allowing premature player movement.
- **Fix:** Disable `GraphicRaycaster` on the joystick canvas parent (keeps it visible but non-interactive). Re-enable on race start.

### Networking

**Issue: NGO NetworkConfig mismatch on client join**
- Host has `ConnectionApproval = true`; client doesn't set it before `StartClient()` — config hash mismatch, client kicked.
- **Fix:** Set `NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true` on the client before calling `StartClient()`.

### C# / Unity Editor Code

**Issue: `OperationCanceledException` not found**
- Used without `using System;` — the type lives in `System`, not `UnityEngine`.
- **Fix:** Add `using System;` to any file that catches `OperationCanceledException`.

**Issue: `Object` ambiguous in `unity_execute_code`**
- `Object` is ambiguous between `UnityEngine.Object` and `System.object` in editor scripts.
- **Fix:** Always use the fully qualified `UnityEngine.Object.DestroyImmediate(...)` in execute_code blocks.

**Issue: `using` directives fail in `unity_execute_code`**
- Top-level `using` statements are not supported in the execute_code context.
- **Fix:** Use fully qualified type names (e.g. `AdventureMultiplayer.SpectatorController`, `TMPro.TextMeshProUGUI`).

### Blender Proxy Workflow

**Issue: Proxy mesh collider looks jagged/wrong in Unity**
- Exporting without the -90° X rotation correction causes the mesh to be misaligned in Unity (Z-up vs Y-up mismatch).
- **Fix:** After importing the FBX into Blender, **first apply scale** (`transform_apply(scale=True)`), then apply -90° X rotation (`rotation_euler[0] = radians(-90)`, `transform_apply(rotation=True)`), then export. Applying scale is required because ithappy FBXes import at 0.01 scale in Blender, and applying scale first swaps Y/Z; the -90° X rotation corrects the swap. Skipping the scale step and only rotating gives wrong results because the scale is still 0.01 and the exporter can't compensate correctly.

**Issue: Scene instance edited instead of prefab**
- Modifying components directly on a scene instance creates per-instance overrides. Other scenes and future instances of the same prefab stay broken.
- **Fix:** Always edit the source prefab using `PrefabUtility.LoadPrefabContents` / `SaveAsPrefabAsset`. Then revert stale scene overrides with `PrefabUtility.SetPropertyModifications(root, null)`. (`RevertAllObjectOverrides` does not exist in this Unity version.)

**Issue: Open boundary edges cause collider gaps**
- Meshes with open holes (missing faces) produce colliders with gaps — players can clip through edges.
- **Fix:** In Edit mode, select all → `mesh.edge_face_add()` to fill all open boundary loops before exporting.
