# DeathRunL1 — Collider Status

All unique meshes used in the scene, their current collider setup, and recommended action.

**Status legend:**
- ✅ Done — custom Blender collision mesh in place
- 🟢 OK — current collider is accurate enough, no action needed
- 🟡 Upgrade — replace mesh collider with a primitive (Box/Capsule/Sphere) for performance
- 🔴 Needs Custom — complex or concave shape that needs a Blender collision proxy
- ⚪ Decorative — no collision needed, can remove collider entirely

---

## Obstacles (Gameplay Critical)

| Mesh | Verts | Current Collider | Status | Action |
|------|-------|-----------------|--------|--------|
| obstacle_13_002 | 700 | Mesh (custom, non-convex) | ✅ Done | Custom Blender proxy created (gap filled) |
| obstacle_13_001 | 98 | Mesh (proxy, non-convex) | ✅ Done | Blender proxy — gap filled |
| obstacle_12_001 | 593 | Mesh (proxy, non-convex) | ✅ Done | Blender proxy — boundary edges filled |
| obstacle_14_002 | 910 | Mesh (proxy, non-convex) | ✅ Done | Blender proxy — boundary edges filled |
| obstacle_6_001 | 788 | Mesh (proxy, non-convex) | ✅ Done | Blender proxy — boundary edges filled |
| obstacle_001 | 341 | Mesh (proxy, non-convex) | ✅ Done | Blender proxy — boundary edges filled |
| obstacle_4_001 | 50 | FanCylinder (convex) | 🟢 OK | Convex cylinder — fine |
| obstacle_4_002 | 252 | Mesh (proxy, non-convex) | ✅ Done | Blender proxy — FanCylinder replaced |
| obstacle_11_001 | 86 | Mesh (proxy, non-convex) | ✅ Done | Blender proxy — FanCylinder replaced |
| obstacle_11_002 | 504 | Box | 🟢 OK | Box collider — fine |
| obstacle_14_001 | 50 | Mesh (proxy, non-convex) | ✅ Done | Blender proxy — FanCylinder replaced |

---

## Props & Environment

| Mesh | Verts | Current Collider | Status | Action |
|------|-------|-----------------|--------|--------|
| land_001 | 536 | Mesh (non-convex) | 🟢 OK | Platform terrain — OK |
| land_002 | 296 | Mesh (non-convex) | 🟢 OK | Platform terrain — OK |
| land_003 | 233 | Mesh (non-convex) | 🟢 OK | Platform terrain — OK |
| land_005 | 178 | Mesh (non-convex) | 🟢 OK | Platform terrain — OK |
| land_006 | 811 | Mesh (non-convex) | 🟢 OK | Platform terrain — OK |
| land_007 | 178 | Mesh (non-convex) | 🟢 OK | Platform terrain — OK |
| land_008 | 575 | Mesh (non-convex) | 🟢 OK | Platform terrain — OK |
| checkpoint_tree_001 | 1641 | Mesh (proxy, non-convex) | ✅ Done | Blender proxy — boundary edges filled |
| checkpoint_001 | 546 | FanCylinder (non-convex) | 🟢 OK | Checkpoint post — fine |
| big_log_001 | 68 | Mesh (proxy, non-convex) | ✅ Done | Blender proxy — FanCylinder replaced |
| log_001 | 68 | Mesh (proxy, non-convex) | ✅ Done | Blender proxy — FanCylinder replaced |
| box_001 | 241 | Box | 🟢 OK | Box collider — fine |
| fence_pillar_001 | 50 | Mesh (proxy, non-convex) | ✅ Done | Blender proxy — exact mesh collider |
| fence_pillar_003 | 97 | Mesh (proxy, non-convex) | ✅ Done | Blender proxy — exact mesh collider |
| fence_wood_001 | 54 | Mesh (proxy, non-convex) | ✅ Done | Blender proxy — exact mesh collider |
| fence_wood_002 | 54 | Mesh (proxy, non-convex) | ✅ Done | Blender proxy — exact mesh collider |
| stake_001 | 32 | Mesh (proxy, non-convex) | ✅ Done | Blender proxy — exact mesh collider |
| ladder_001 | 172 | Mesh (non-convex) | 🟢 OK | Keep mesh — shape matters |
| hive_001 | 188 | Mesh (non-convex) | 🟢 OK | Decorative prop — fine |
| indicator_002 | 99 | Mesh (non-convex) | 🟢 OK | Fine |
| finish_001 | 144 | Mesh (non-convex) | 🟢 OK | Fine |

---

## Rocks & Stones

| Mesh | Verts | Current Collider | Status | Action |
|------|-------|-----------------|--------|--------|
| rock_001 | 334 | Mesh (non-convex) | 🟢 OK | Organic shape — mesh is fine |
| rock_002 | 361 | Mesh (non-convex) | 🟢 OK | Organic shape — mesh is fine |
| rock_003 | 187 | Mesh (non-convex) | 🟢 OK | Organic shape — mesh is fine |
| rock_004 | 257 | Mesh (non-convex) | 🟢 OK | Organic shape — mesh is fine |
| stone_001 | 122 | Mesh (non-convex) | 🟢 OK | Fine |
| stone_002 | 143 | Mesh (non-convex) | 🟢 OK | Fine |
| stone_003 | 142 | Mesh (non-convex) | 🟢 OK | Fine |
| stone_004 | 214 | Mesh (non-convex) | 🟢 OK | Fine |

---

## Decorative (No Collision Needed)

| Mesh | Verts | Current Collider | Status | Action |
|------|-------|-----------------|--------|--------|
| bush_001 | 168 | None | ✅ Done | Collider removed |
| bush_002 | 132 | None | ✅ Done | Collider removed |
| tree_001 | 242 | None | ✅ Done | Collider removed |
| tree_002 | 500 | None | ✅ Done | Collider removed |
| rope_001 | 35 | None | ✅ Done | Collider removed |
| flag_001 | 219 | None | ✅ Done | Collider removed |
| water_001 | 4 | Mesh (non-convex) | 🟢 OK | Trigger — fine |
| glider | 53 | None | 🟢 OK | No collider — correct |

---

## Summary

| Priority | Count | Items |
|----------|-------|-------|
| ✅ Done | 23 | All red/yellow obstacles + decorative colliders removed |
| 🟢 OK | 20+ | Platforms, rocks, boxes — no action needed |
