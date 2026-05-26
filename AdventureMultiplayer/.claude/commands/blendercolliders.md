Replace all custom collider components (FanCylinderCollider, TorusCollider, ConeCollider) on prefabs with Blender-created MeshCollider proxies, then verify alignment.

## When to run
- A new obstacle or prop prefab is added to the project
- A prefab still has FanCylinderCollider / TorusCollider / ConeCollider on any child
- $ARGUMENTS (optional): a specific prefab path or mesh name to process; if omitted, process everything

---

## Excluded folders (never touch these)

These third-party packs share mesh names with other packs but have different geometry. Always skip them in every scan:
- `Platformer_2_Obstacles` — pre-existing missing scripts, `EditPrefabContentsScope` cannot save them
- `Platformer_10_Neon` — reuses same mesh names as Platformer Deathrun but with different proportions

---

## Step 1 — Scan for remaining custom colliders

Run this in Unity to find every prefab that still needs fixing:

```csharp
var sb = new System.Text.StringBuilder();
int count = 0;
foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
{
    string path = AssetDatabase.GUIDToAssetPath(guid);
    if (path.Contains("Platformer_2_Obstacles") || path.Contains("Platformer_10_Neon")) continue;
    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
    if (prefab == null) continue;
    foreach (var c in prefab.GetComponentsInChildren<Component>(true))
    {
        if (c == null) continue;
        string t = c.GetType().Name;
        if (t == "FanCylinderCollider" || t == "TorusCollider" || t == "ConeCollider")
        {
            var so = new UnityEditor.SerializedObject(c);
            float r = so.FindProperty("m_radius") != null ? so.FindProperty("m_radius").floatValue : -1f;
            float h = so.FindProperty("m_height") != null ? so.FindProperty("m_height").floatValue : -1f;
            sb.AppendLine(path + " | " + c.gameObject.name + " | " + t + " r=" + r + " h=" + h);
            count++;
        }
    }
}
return "Found " + count + ":\n" + sb;
```

Note the mesh names and FanCylinder parameters (radius, height) from the output.

---

## Step 2 — Create Blender proxy FBX files

For each unique mesh or cylinder shape found, run in Blender MCP:

### A) Visual mesh objects (have a MeshFilter with a named mesh)
Import the exact FBX, fill open edges, apply the -90° rotation fix, export:

```python
import bpy, os, math

SRC = "D:/Unity Projects/AdventureMultiplayer/AdventureMultiplayer/Assets/ThirdParty/ithappy/Platformer Deathrun/Meshes/<folder>/<mesh_name>.fbx"
DST = "D:/Unity Projects/AdventureMultiplayer/AdventureMultiplayer/Assets/RC/Meshes/<mesh_name>_collider.fbx"

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for b in bpy.data.meshes: bpy.data.meshes.remove(b)

bpy.ops.import_scene.fbx(filepath=SRC)
meshes = [o for o in bpy.context.selected_objects if o.type == 'MESH']
bpy.ops.object.select_all(action='DESELECT')
for o in meshes: o.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]
if len(meshes) > 1: bpy.ops.object.join()
obj = bpy.context.active_object

# Bake import rotation into vertices
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

# Fill open boundary edges (gaps / open ends)
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='DESELECT')
bpy.ops.mesh.select_non_manifold(extend=False, use_wire=False, use_boundary=True,
    use_multi_face=False, use_non_contiguous=False, use_verts=False)
bpy.ops.mesh.edge_face_add()
bpy.ops.object.mode_set(mode='OBJECT')

# Apply -90° X rotation correction (Unity↔Blender axis fix)
obj.rotation_euler[0] = __import__('math').radians(-90)
bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)

bpy.ops.export_scene.fbx(filepath=DST, use_selection=True,
    axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
    object_types={'MESH'}, mesh_smooth_type='OFF')
```

### B) NO_MESH child objects (procedural cylinders — Top, Bot, Drum, Fan1, etc.)
Create a cylinder in Blender matching the FanCylinder's radius and height:

```python
import bpy, os, math

RADIUS = 0.45   # from FanCylinder m_radius
HEIGHT = 13.75  # from FanCylinder m_height
DST = "D:/Unity Projects/AdventureMultiplayer/AdventureMultiplayer/Assets/RC/Meshes/cyl_r<R>_h<H>_collider.fbx"

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=RADIUS, depth=HEIGHT, location=(0,0,0))
obj = bpy.context.active_object
obj.rotation_euler[0] = math.radians(-90)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

bpy.ops.export_scene.fbx(filepath=DST, use_selection=True,
    axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
    object_types={'MESH'}, mesh_smooth_type='OFF')
```

**Naming convention:** `cyl_r<radius>_h<height>_collider.fbx`  
If the cylinder has a Y offset (center.y != 0), pass `location=(0, center_y, 0)` in `primitive_cylinder_add`.

---

## Step 3 — Assign proxies to prefabs in Unity

```csharp
AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

System.Func<string, Mesh> loadMesh = (p) => {
    foreach (var a in AssetDatabase.LoadAllAssetsAtPath(p)) if (a is Mesh m) return m;
    return null;
};
string B = "Assets/RC/Meshes/";

// Add entries for every proxy created in Step 2
var visualProxy = new System.Collections.Generic.Dictionary<string, Mesh>
{
    { "<mesh_name>", loadMesh(B + "<mesh_name>_collider.fbx") },
};
var cylProxy = new System.Collections.Generic.Dictionary<string, Mesh>
{
    { "<radius>_<height>", loadMesh(B + "cyl_r<radius>_h<height>_collider.fbx") },
};

// IMPORTANT: never include paths from Platformer_2_Obstacles or Platformer_10_Neon
string[] prefabPaths = { /* paste paths from Step 1 */ };

int total = 0;
foreach (var path in prefabPaths)
{
    using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
    {
        foreach (var t in scope.prefabContentsRoot.GetComponentsInChildren<Transform>(true))
        {
            var node = t.gameObject;
            var customs = new System.Collections.Generic.List<Component>();
            foreach (var c in node.GetComponents<Component>())
            {
                if (c == null) continue;
                string tn = c.GetType().Name;
                if (tn == "FanCylinderCollider" || tn == "TorusCollider" || tn == "ConeCollider")
                    customs.Add(c);
            }
            if (customs.Count == 0) continue;

            foreach (var mc in node.GetComponents<MeshCollider>())
                if (mc.sharedMesh == null || mc.sharedMesh.name.Contains("radius") ||
                    mc.sharedMesh.name.Contains("Cylinder") || mc.sharedMesh.name.Contains("Torus"))
                    UnityEngine.Object.DestroyImmediate(mc);

            foreach (var custom in customs)
            {
                var so = new UnityEditor.SerializedObject(custom);
                float r = so.FindProperty("m_radius") != null ? so.FindProperty("m_radius").floatValue : -1f;
                float h = so.FindProperty("m_height") != null ? so.FindProperty("m_height").floatValue : -1f;
                UnityEngine.Object.DestroyImmediate(custom);

                Mesh proxy = null;
                var mf = node.GetComponent<MeshFilter>();
                string mn = mf != null && mf.sharedMesh != null ? mf.sharedMesh.name : null;
                if (mn != null) visualProxy.TryGetValue(mn, out proxy);
                if (proxy == null && r > 0 && h > 0)
                    cylProxy.TryGetValue(System.Math.Round(r,4) + "_" + System.Math.Round(h,4), out proxy);

                if (proxy != null)
                {
                    var mc = node.AddComponent<MeshCollider>();
                    mc.sharedMesh = proxy;
                    mc.convex = false;
                    total++;
                }
            }
        }
    }
}
AssetDatabase.SaveAssets();
return "Replaced " + total;
```

---

## Step 4 — Verify alignment

Run this to confirm collider bounds match visual bounds (all should be OK, 0 MISMATCH):

```csharp
int ok = 0, bad = 0;
var log = new System.Text.StringBuilder();
foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
{
    string path = AssetDatabase.GUIDToAssetPath(guid);
    if (path.Contains("Platformer_2_Obstacles") || path.Contains("Platformer_10_Neon")) continue;
    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
    if (prefab == null) continue;
    foreach (var mf in prefab.GetComponentsInChildren<MeshFilter>(true))
    {
        if (mf.sharedMesh == null) continue;
        var mc = mf.GetComponent<MeshCollider>();
        if (mc == null || mc.sharedMesh == null || mc.sharedMesh == mf.sharedMesh) continue;
        if (mc.sharedMesh.name == "Cylinder") continue;
        var vb = mf.sharedMesh.bounds;
        var cb = mc.sharedMesh.bounds;
        float cd = Vector3.Distance(vb.center, cb.center);
        Vector3 sr = new Vector3(
            vb.size.x > 0.001f ? cb.size.x / vb.size.x : 1f,
            vb.size.y > 0.001f ? cb.size.y / vb.size.y : 1f,
            vb.size.z > 0.001f ? cb.size.z / vb.size.z : 1f);
        bool aligned = cd < 0.05f && Mathf.Abs(sr.x-1f)<0.1f && Mathf.Abs(sr.y-1f)<0.1f && Mathf.Abs(sr.z-1f)<0.1f;
        if (aligned) ok++;
        else { bad++; log.AppendLine("MISMATCH: " + mf.gameObject.name + " in " + System.IO.Path.GetFileName(path) + " | centerDist=" + cd.ToString("F3") + " sizeRatio=" + sr.ToString("F3")); }
    }
}
return "OK=" + ok + " MISMATCH=" + bad + "\n" + log;
```

If any MISMATCH appears, re-check the Blender export rotation for that mesh (see Step 2).

---

## Proxy file locations
- Visual mesh proxies: `Assets/RC/Meshes/<mesh_name>_collider.fbx`
- Cylinder proxies: `Assets/RC/Meshes/cyl_r<radius>_h<height>_collider.fbx`
- Drum (with Y offset): `Assets/RC/Meshes/cyl_drum_r<radius>_h<height>_collider.fbx`
