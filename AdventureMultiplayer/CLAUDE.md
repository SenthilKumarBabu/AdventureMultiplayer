# AdventureMultiplayer – Coding Rules

## Component References
- Never use `AddComponent<T>()` in scripts. Assign components via the Inspector or find them in `Awake`/`Start`.

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
