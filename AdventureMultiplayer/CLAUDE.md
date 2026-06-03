# AdventureMultiplayer – Coding Rules

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

## Diagnosing Issues — Editor Log
- When logs are needed to diagnose a problem and the user has not provided them, read the Unity Editor log file directly.
- Editor log path on Windows: `C:/Users/<username>/AppData/Local/Unity/Editor/Editor.log`
- For this project the path is: `C:/Users/Senthil/AppData/Local/Unity/Editor/Editor.log`
- Read the tail of the file to find the last play session — look for the `PLAY MODE` separator or the most recent timestamped entries.

## PLAYER TWO Package
- Use the **PLAYER TWO** package (`Assets/PLAYER TWO/`) as the foundation for all player-related systems and environment design.
- For player states: extend or override the existing state machine classes (e.g. `PlayerState`, `EntityState`) rather than writing a new state system from scratch.
- For environment design: reuse PLAYER TWO obstacle, platform, and hazard prefabs/components as the starting point; build custom obstacles on top of them.
- Never modify plugin scripts directly (anything under `Assets/PLAYER TWO/` or `Assets/Plugins/`). Duplicate into `Assets/Scripts/` first, then modify the copy.

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
