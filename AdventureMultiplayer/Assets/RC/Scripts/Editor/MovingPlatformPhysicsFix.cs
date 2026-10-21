using UnityEditor;
using UnityEngine;

namespace AdventureMultiplayer.EditorTools
{
    /// <summary>
    /// One-off repair for <c>moving_platform.prefab</c> — the rotating stepping discs in
    /// DeathRunL1.
    ///
    /// THE BUG. The disc spins at 50°/s through <see cref="ithappy.rc.RotationScript"/>, but the
    /// GameObject has no Rigidbody. With none present that script falls through to its
    /// <c>transform.Rotate</c> path, which makes the disc a *static* collider that teleports
    /// every frame. PhysX resolves the resulting overlap with a depenetration impulse and flings
    /// whatever is standing on it. A DeathRunL1 play-test caught exactly that: bots on the disc
    /// field were grounded on only 28% of frames and their height ranged from -2.74 (the water
    /// kill plane) to +6.33 on a crossing that should be flat at y≈1.4. Every bot death in the
    /// race happened there. A human player is thrown off the same way — this is not an AI bug.
    ///
    /// THE FIX, and why it changes nothing else:
    ///   1. MeshCollider.convex = true. PhysX does not support a non-convex MeshCollider on a
    ///      non-static body, so this is a precondition for step 2. It does NOT reshape the
    ///      collider: FanCylinderCollider generates a closed 360° cylinder, which is already
    ///      convex, so the cooked hull matches the existing shape.
    ///   2. A KINEMATIC Rigidbody. This is the actual repair — RotationScript then drives the
    ///      disc through rb.MoveRotation in FixedUpdate, so PhysX treats it as an intentionally
    ///      moving body and applies a bounded, physical push instead of an ejection impulse.
    ///      Interpolation is enabled so the visual spin stays as smooth as the Update path was.
    ///
    /// Deliberately left alone: rotationSpeed (50) and alternateByPosition — the discs spin at
    /// the same rate, on the same axis, in the same alternating directions. The
    /// FanCylinderCollider is KEPT even though the project bans it, because its generated mesh
    /// is the disc's only collision: the two MeshColliders have no mesh asset assigned, so
    /// removing the script before a Blender proxy exists would leave the discs with no collision
    /// at all. Swapping in a proxy is a separate, later job. DynamicPlatform is untouched — it
    /// reads transform deltas in LateUpdate, which MoveRotation still updates correctly.
    ///
    /// Scope: moving_platform.prefab only — 13 instances, all in DeathRunL1, none in L2/L3.
    /// The other RotationScript users (dumbbell, rotating_beam, spinner, rotating_log, …) are
    /// separate prefabs and are not touched.
    ///
    /// Safe to run more than once: it reports and skips anything already correct.
    /// </summary>
    public static class MovingPlatformPhysicsFix
    {
        private const string k_prefabPath = "Assets/RC/Prefabs/Obstacles/moving_platform.prefab";

        [MenuItem("Adventure Multiplayer/Fix/Moving Platform Disc Physics")]
        public static void Fix()
        {
            var root = PrefabUtility.LoadPrefabContents(k_prefabPath);
            if (root == null)
            {
                Debug.LogError($"[MovingPlatformPhysicsFix] Could not load {k_prefabPath}.");
                return;
            }

            try
            {
                // The disc is the object that actually spins. Identified by its RotationScript
                // rather than by name, since the prefab is assembled from nested prefab
                // instances whose GameObjects are unnamed in the asset.
                var spinner = root.GetComponentInChildren<ithappy.rc.RotationScript>(true);
                if (spinner == null)
                {
                    Debug.LogError("[MovingPlatformPhysicsFix] No ithappy.rc.RotationScript found " +
                                   "in the prefab — nothing changed.");
                    return;
                }

                GameObject disc    = spinner.gameObject;
                bool       changed = false;

                // 1. Convex collider — required before a Rigidbody may be attached.
                var mc = disc.GetComponent<MeshCollider>();
                if (mc == null)
                {
                    Debug.LogWarning("[MovingPlatformPhysicsFix] The spinning object has no " +
                                     "MeshCollider. Skipping the convex step.");
                }
                else if (!mc.convex)
                {
                    mc.convex = true;
                    changed   = true;
                    Debug.Log("[MovingPlatformPhysicsFix] MeshCollider.convex -> true " +
                              "(shape unchanged: the generated fan cylinder is already convex).");
                }

                // 2. Kinematic Rigidbody — the actual fix for the ejection.
                var rb = disc.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    // AddComponent is a runtime prohibition; this is one-off editor tooling,
                    // matching the other bulk-setup tools in this folder.
                    rb      = disc.AddComponent<Rigidbody>();
                    changed = true;
                    Debug.Log("[MovingPlatformPhysicsFix] Added a Rigidbody to the spinning disc.");
                }

                if (!rb.isKinematic || rb.useGravity ||
                    rb.interpolation != RigidbodyInterpolation.Interpolate)
                {
                    rb.isKinematic    = true;   // driven by MoveRotation, never by physics forces
                    rb.useGravity     = false;
                    rb.interpolation  = RigidbodyInterpolation.Interpolate; // keep the spin visually smooth
                    changed           = true;
                    Debug.Log("[MovingPlatformPhysicsFix] Rigidbody set kinematic, gravity off, " +
                              "interpolation on.");
                }

                if (!changed)
                {
                    Debug.Log("[MovingPlatformPhysicsFix] Already correct — nothing to change.");
                    return;
                }

                PrefabUtility.SaveAsPrefabAsset(root, k_prefabPath);
                AssetDatabase.SaveAssets();

                Debug.Log($"[MovingPlatformPhysicsFix] Saved {k_prefabPath}. The 13 DeathRunL1 " +
                          "instances inherit this automatically — no scene edit needed. " +
                          "Spin rate, direction and collision shape are unchanged.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
