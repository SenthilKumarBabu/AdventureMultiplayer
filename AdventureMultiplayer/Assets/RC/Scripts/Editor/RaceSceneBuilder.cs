using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

namespace AdventureMultiplayer.Editor
{
    /// <summary>
    /// Builds the RaceMap scene using Platformer Deathrun + Platformer 10 Neon assets.
    ///
    /// Menu: Adventure Multiplayer / Build Race Scene
    ///
    /// Track layout (+Z forward):
    ///   Z   0 –  10  : Start area
    ///   Z  10 – 110  : Section 1 – Neon sprint (flat + obstacles)
    ///   Z 110 – 210  : Section 2 – Deathrun gauntlet (hazards)
    ///   Z 210 – 290  : Section 3 – Jump challenge (platforms over void)
    ///   Z 300        : Finish line
    ///
    /// Physics floor covers Sections 1+2. Section 3 is platforms only — fall = respawn.
    /// </summary>
    public static class RaceSceneBuilder
    {
        // ── Track constants ──────────────────────────────────────────────────────
        private const float TrackWidth    = 10f;
        private const float WallHeight    = 6f;
        private const float Sec1Start     = 10f;
        private const float Sec1End       = 110f;
        private const float Sec2Start     = 110f;
        private const float Sec2End       = 210f;
        private const float Sec3Start     = 210f;
        private const float Sec3End       = 290f;
        private const float FinishZ       = 300f;
        private const float FloorY        = -0.5f;  // top surface at Y=0

        // ── Verified asset paths ─────────────────────────────────────────────────
        private const string NeonRoot = "Assets/ThirdParty/ithappy/Platformer_10_Neon/Prefabs";
        private const string DrRoot   = "Assets/ThirdParty/ithappy/Platformer Deathrun/Prefabs";

        // Neon – Environment
        private const string NeonGround = NeonRoot + "/Environment/ground_001.prefab";
        private const string NeonTiles  = NeonRoot + "/Environment/neon_tiles_001.prefab";
        private const string NeonCircle = NeonRoot + "/Environment/neon_circle_001.prefab";

        // Neon – Platforms (for jump section)
        private const string NeonPlat01 = NeonRoot + "/Platforms/platform_001.prefab";
        private const string NeonPlat02 = NeonRoot + "/Platforms/platform_002.prefab";
        private const string NeonPlat03 = NeonRoot + "/Platforms/platform_003.prefab";
        private const string NeonPlat04 = NeonRoot + "/Platforms/platform_004.prefab";
        private const string NeonPlat05 = NeonRoot + "/Platforms/platform_005.prefab";
        private const string NeonPlat06 = NeonRoot + "/Platforms/platform_006.prefab";
        private const string NeonPlat07 = NeonRoot + "/Platforms/platform_007.prefab";
        private const string NeonPlat08 = NeonRoot + "/Platforms/platform_008.prefab";

        // Neon – Barriers / walls
        private const string NeonFence  = NeonRoot + "/Barriers/fence_001.prefab";
        private const string NeonFence2 = NeonRoot + "/Barriers/fence_002.prefab";
        private const string NeonPillar = NeonRoot + "/Barriers/pillar_001.prefab";
        private const string NeonTower  = NeonRoot + "/Barriers/tower_001.prefab";

        // Neon – Obstacles
        private const string NeonObs1   = NeonRoot + "/Obstacles/obstacle_1_001.prefab";
        private const string NeonObs2   = NeonRoot + "/Obstacles/obstacle_2_001.prefab";
        private const string NeonObs3   = NeonRoot + "/Obstacles/obstacle_3_001.prefab";
        private const string NeonObs4   = NeonRoot + "/Obstacles/obstacle_4_001.prefab";
        private const string NeonObs5   = NeonRoot + "/Obstacles/obstacle_5_001.prefab";
        private const string NeonObs6   = NeonRoot + "/Obstacles/obstacle_6_001.prefab";
        private const string NeonObs7   = NeonRoot + "/Obstacles/obstacle_7_001.prefab";
        private const string NeonTramp1 = NeonRoot + "/Obstacles/trampoline_1_001.prefab";
        private const string NeonTramp2 = NeonRoot + "/Obstacles/trampoline_2_001.prefab";

        // Deathrun – Landscape
        private const string DrLand1    = DrRoot + "/Landscape/land_001.prefab";
        private const string DrLand2    = DrRoot + "/Landscape/land_002.prefab";
        private const string DrLand3    = DrRoot + "/Landscape/land_003.prefab";
        private const string DrLand4    = DrRoot + "/Landscape/land_004.prefab";
        private const string DrLand5    = DrRoot + "/Landscape/land_005.prefab";

        // Deathrun – Obstacles
        private const string DrObs001   = DrRoot + "/Obstacles/obstacle_001.prefab";
        private const string DrObs2     = DrRoot + "/Obstacles/obstacle_2_001.prefab";
        private const string DrObs3     = DrRoot + "/Obstacles/obstacle_3_001.prefab";
        private const string DrObs4     = DrRoot + "/Obstacles/obstacle_4_001.prefab";
        private const string DrObs5     = DrRoot + "/Obstacles/obstacle_5_001.prefab";
        private const string DrObs6     = DrRoot + "/Obstacles/obstacle_6_001.prefab";
        private const string DrObs7     = DrRoot + "/Obstacles/obstacle_7_001.prefab";
        private const string DrObs8     = DrRoot + "/Obstacles/obstacle_8_001.prefab";
        private const string DrObs9     = DrRoot + "/Obstacles/obstacle_9_001.prefab";
        private const string DrObs10    = DrRoot + "/Obstacles/obstacle_10_001.prefab";
        private const string DrTramp1   = DrRoot + "/Interaction/trampoline_001.prefab";
        private const string DrTramp2   = DrRoot + "/Interaction/trampoline_002.prefab";
        private const string DrBarrier  = DrRoot + "/Stuff/barrier_001.prefab";

        // Deathrun – Props
        private const string DrFinish   = DrRoot + "/Props/finish_001.prefab";
        private const string DrCheckpt  = DrRoot + "/Props/checkpoint_001.prefab";

        // Skybox
        private const string NeonSkybox = "Assets/ThirdParty/ithappy/Platformer_10_Neon/Skybox/Skybox_1.mat";

        private const string ScenePath  = "Assets/Scenes/RaceMap.unity";

        private static Shader s_litShader;

        // ── Entry point ──────────────────────────────────────────────────────────

        [MenuItem("Adventure Multiplayer/Build Race Scene")]
        public static void BuildRaceScene()
        {
            s_litShader = null;

            Directory.CreateDirectory(Application.dataPath + "/../Assets/Scenes");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SetupEnvironment();
            CreateLighting();

            var track = new GameObject("Track").transform;
            BuildSolidFloor(track);
            BuildSection1_NeonSprint(track);
            BuildSection2_Deathrun(track);
            BuildSection3_JumpChallenge(track);
            BuildWalls(track);
            BuildKillPlane(track);

            BuildStartArea();
            BuildFinishArea();
            BuildCheckpoints();
            BuildSpawnPoints();
            BuildGameManager();
            BuildCamera();

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[RaceSceneBuilder] Saved to " + ScenePath);

            EditorUtility.DisplayDialog("Done",
                "RaceMap.unity saved.\n\nNext:\n" +
                "1. Assign Player prefab to Spawn points\n" +
                "2. Add NetworkManager to GameManager\n" +
                "3. Assign RaceManager.checkpoints in Inspector\n" +
                "4. Add to Build Settings", "OK");
        }

        // ── Environment ──────────────────────────────────────────────────────────

        private static void SetupEnvironment()
        {
            var skybox = AssetDatabase.LoadAssetAtPath<Material>(NeonSkybox);
            if (skybox != null) RenderSettings.skybox = skybox;
            else Debug.LogWarning("[RaceSceneBuilder] Neon skybox not found: " + NeonSkybox);

            RenderSettings.ambientMode      = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight     = new Color(0.08f, 0.04f, 0.18f);
            RenderSettings.fog              = true;
            RenderSettings.fogColor         = new Color(0.04f, 0.02f, 0.12f);
            RenderSettings.fogMode          = FogMode.Linear;
            RenderSettings.fogStartDistance = 60f;
            RenderSettings.fogEndDistance   = 320f;
        }

        private static void CreateLighting()
        {
            var go    = new GameObject("Directional Light");
            var light = go.AddComponent<Light>();
            light.type      = LightType.Directional;
            light.intensity = 1.1f;
            light.color     = new Color(0.85f, 0.8f, 1f);
            go.transform.rotation = Quaternion.Euler(45f, -30f, 0);
        }

        // ── Physics floor (Sections 1+2 only) ───────────────────────────────────

        private static void BuildSolidFloor(Transform parent)
        {
            float length = Sec2End - Sec1Start;
            float midZ   = (Sec1Start + Sec2End) * 0.5f;

            var go  = new GameObject("PhysicsFloor");
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(0, FloorY, midZ);

            var col  = go.AddComponent<BoxCollider>();
            col.size = new Vector3(TrackWidth, 1f, length);

            // Invisible mesh so it registers correctly in editor
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            var mr = go.AddComponent<MeshRenderer>();
            mr.enabled = false;   // physics only — visuals come from art prefabs
        }

        // ── Section 1: Neon sprint (Z 10–110) ───────────────────────────────────

        private static void BuildSection1_NeonSprint(Transform parent)
        {
            var root = new GameObject("Section1_NeonSprint").transform;
            root.SetParent(parent);

            // Floor tiles every 5 units
            for (float z = Sec1Start; z < Sec1End; z += 5f)
                Place(NeonTiles, new Vector3(0, 0, z), root, $"NeonTile_Z{(int)z}");

            // Pillars flanking the corridor every 15 units
            for (float z = Sec1Start + 5f; z < Sec1End - 5f; z += 15f)
            {
                Place(NeonPillar, new Vector3(-4.5f, 0, z), root, $"PillarL_Z{(int)z}");
                Place(NeonPillar, new Vector3( 4.5f, 0, z), root, $"PillarR_Z{(int)z}");
            }

            // Fences between pillars (alternating sides)
            for (float z = Sec1Start + 10f; z < Sec1End - 10f; z += 15f)
            {
                bool left = ((int)(z / 15f) % 2) == 0;
                Place(NeonFence, new Vector3(left ? -4.5f : 4.5f, 0, z), root, $"Fence_Z{(int)z}");
            }

            // Obstacles in the lane — alternating left-centre-right
            var obs = new (float x, float z, string prefab)[]
            {
                (-2f,  25f, NeonObs1),
                ( 2f,  38f, NeonObs2),
                ( 0f,  50f, NeonObs3),
                (-2f,  63f, NeonObs4),
                ( 2f,  76f, NeonObs5),
                ( 0f,  88f, NeonObs6),
                (-2f, 100f, NeonObs7),
            };
            for (int i = 0; i < obs.Length; i++)
                Place(obs[i].prefab, new Vector3(obs[i].x, 0, obs[i].z), root, $"NeonObs_{i + 1}");

            // Decorative neon circles beside the track
            Place(NeonCircle, new Vector3(-6f, 3f, 35f), root, "Circle_L1");
            Place(NeonCircle, new Vector3( 6f, 3f, 65f), root, "Circle_R1");
            Place(NeonCircle, new Vector3(-6f, 3f, 95f), root, "Circle_L2");

            Debug.Log("[RaceSceneBuilder] Section 1 built.");
        }

        // ── Section 2: Deathrun gauntlet (Z 110–210) ────────────────────────────

        private static void BuildSection2_Deathrun(Transform parent)
        {
            var root = new GameObject("Section2_Deathrun").transform;
            root.SetParent(parent);

            // Deathrun land tiles every 10 units
            string[] lands = { DrLand1, DrLand2, DrLand3, DrLand4, DrLand5,
                               DrLand1, DrLand2, DrLand3, DrLand4, DrLand5 };
            for (int i = 0; i < lands.Length; i++)
                Place(lands[i], new Vector3(0, 0, Sec2Start + i * 10f), root, $"DrLand_{i + 1}");

            // Barriers on sides every 20 units
            for (float z = Sec2Start + 5f; z < Sec2End; z += 20f)
            {
                Place(DrBarrier, new Vector3(-5f, 0, z), root, $"DrBarrierL_Z{(int)z}");
                Place(DrBarrier, new Vector3( 5f, 0, z), root, $"DrBarrierR_Z{(int)z}");
            }

            // Obstacles — progressively harder clusters
            var obs = new (float x, float z, string prefab)[]
            {
                // Cluster 1 – entry
                ( 0f, 120f, DrObs001),
                (-2f, 130f, DrObs2),
                ( 2f, 130f, DrObs3),

                // Cluster 2 – mid
                ( 0f, 145f, DrObs4),
                (-3f, 155f, DrObs5),
                ( 3f, 155f, DrObs6),

                // Cluster 3 – exit sprint
                ( 0f, 165f, DrObs7),
                (-2f, 175f, DrObs8),
                ( 2f, 175f, DrObs9),
                ( 0f, 185f, DrObs10),
                (-3f, 195f, DrObs2),
                ( 3f, 195f, DrObs3),
            };
            for (int i = 0; i < obs.Length; i++)
                Place(obs[i].prefab, new Vector3(obs[i].x, 0, obs[i].z), root, $"DrObs_{i + 1}");

            // Recovery trampolines between clusters
            Place(DrTramp1, new Vector3(-3f, 0, 138f), root, "DrTramp_1");
            Place(DrTramp2, new Vector3( 3f, 0, 160f), root, "DrTramp_2");
            Place(DrTramp1, new Vector3( 0f, 0, 202f), root, "DrTramp_3");

            Debug.Log("[RaceSceneBuilder] Section 2 built.");
        }

        // ── Section 3: Jump challenge (Z 210–290) ────────────────────────────────

        private static void BuildSection3_JumpChallenge(Transform parent)
        {
            var root = new GameObject("Section3_JumpChallenge").transform;
            root.SetParent(parent);

            // Platforms — each one is a stepping stone over the void.
            // Gap between each is ~4–5 units (jumpable but requires commitment).
            var platforms = new (float z, float x, float y, string prefab)[]
            {
                (215f,  0f, 0f, NeonPlat01),   // wide — easy start
                (222f, -2f, 0f, NeonPlat02),
                (229f,  2f, 0f, NeonPlat03),
                (236f,  0f, 1f, NeonPlat04),   // rising
                (243f, -2f, 2f, NeonPlat05),
                (250f,  2f, 1f, NeonPlat06),
                (257f,  0f, 0f, NeonPlat07),
                (264f, -2f, 0f, NeonPlat08),
                (271f,  2f, 0f, NeonPlat01),
                (278f,  0f, 0f, NeonPlat02),   // landing pad before finish
                (286f,  0f, 0f, NeonPlat03),   // final big pad
            };

            for (int i = 0; i < platforms.Length; i++)
            {
                var (z, x, y, prefab) = platforms[i];
                Place(prefab, new Vector3(x, y, z), root, $"JumpPlat_{i + 1}");
            }

            // Obstacle on a mid platform to keep it interesting
            Place(NeonObs1, new Vector3( 0f, 2f, 236f), root, "JumpObs_1");
            Place(NeonObs2, new Vector3( 2f, 1f, 257f), root, "JumpObs_2");

            // Trampolines on two platforms as aid
            Place(NeonTramp1, new Vector3(-2f, 0f, 222f), root, "JumpTramp_1");
            Place(NeonTramp2, new Vector3( 2f, 0f, 264f), root, "JumpTramp_2");

            // Decorative towers flanking the jump section
            for (float z = Sec3Start; z < Sec3End; z += 25f)
            {
                Place(NeonTower, new Vector3(-8f, 0, z), root, $"TowerL_Z{(int)z}");
                Place(NeonTower, new Vector3( 8f, 0, z), root, $"TowerR_Z{(int)z}");
            }

            Debug.Log("[RaceSceneBuilder] Section 3 built.");
        }

        // ── Solid walls (Sections 1 + 2 only) ───────────────────────────────────

        private static void BuildWalls(Transform parent)
        {
            float length = Sec2End - Sec1Start;
            float midZ   = (Sec1Start + Sec2End) * 0.5f;

            CreateWallCollider(parent, new Vector3(-5.5f, WallHeight * 0.5f, midZ),
                               new Vector3(1f, WallHeight, length), "WallLeft");
            CreateWallCollider(parent, new Vector3( 5.5f, WallHeight * 0.5f, midZ),
                               new Vector3(1f, WallHeight, length), "WallRight");
        }

        private static void CreateWallCollider(Transform parent, Vector3 pos, Vector3 size, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.AddComponent<BoxCollider>().size = size;
        }

        // ── Kill plane ───────────────────────────────────────────────────────────

        private static void BuildKillPlane(Transform parent)
        {
            float midZ = (Sec3Start + Sec3End) * 0.5f;

            var go = new GameObject("KillPlane");
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(0, -15f, midZ);
            go.tag = "KillZone";

            var col  = go.AddComponent<BoxCollider>();
            col.size      = new Vector3(60f, 1f, Sec3End - Sec3Start + 40f);
            col.isTrigger = true;
        }

        // ── Start area ───────────────────────────────────────────────────────────

        private static void BuildStartArea()
        {
            var root = new GameObject("StartArea").transform;

            // Solid floor for the spawn area before Sec1
            var floor = new GameObject("SpawnFloor");
            floor.transform.SetParent(root);
            floor.transform.position = new Vector3(0, FloorY, Sec1Start * 0.5f);
            floor.AddComponent<BoxCollider>().size = new Vector3(TrackWidth, 1f, Sec1Start + 2f);

            // Visual: coloured start quad on floor
            AttachGroundQuad(floor, new Vector3(TrackWidth, Sec1Start, 1f),
                             new Color(0.2f, 0.8f, 0.3f, 0.9f), "StartLine_Visual");

            // Neon tiles on spawn floor
            Place(NeonTiles, new Vector3(0, 0, 2f), root, "SpawnTile_1");
            Place(NeonTiles, new Vector3(0, 0, 7f), root, "SpawnTile_2");
        }

        // ── Finish area ──────────────────────────────────────────────────────────

        private static void BuildFinishArea()
        {
            var root = new GameObject("FinishArea").transform;

            // Try to place the Deathrun finish arch prop
            Place(DrFinish, new Vector3(0, 0, FinishZ), root, "FinishArch");

            // Solid landing floor past the last jump platform
            var floor = new GameObject("FinishFloor");
            floor.transform.SetParent(root);
            floor.transform.position = new Vector3(0, FloorY, FinishZ + 5f);
            floor.AddComponent<BoxCollider>().size = new Vector3(TrackWidth, 1f, 20f);

            // Finish quad
            AttachGroundQuad(floor, new Vector3(TrackWidth, 5f, 1f),
                             new Color(1f, 0.85f, 0.1f, 0.9f), "FinishLine_Visual");

            // Tiles on finish floor
            Place(NeonGround, new Vector3(0, 0, FinishZ + 5f), root, "FinishGround");

            Debug.Log($"[RaceSceneBuilder] Finish area at Z={FinishZ}.");
        }

        // ── Checkpoints ──────────────────────────────────────────────────────────

        private static void BuildCheckpoints()
        {
            var root = new GameObject("Checkpoints").transform;

            var defs = new (int index, bool finish, Vector3 pos)[]
            {
                (0, false, new Vector3(0, 3f, Sec1End - 5f)),   // end of Neon sprint
                (1, false, new Vector3(0, 3f, 155f)),            // mid Deathrun
                (2, false, new Vector3(0, 3f, Sec2End - 5f)),   // end of Deathrun
                (3, false, new Vector3(0, 3f, 260f)),            // mid jump section
                (4, true,  new Vector3(0, 3f, FinishZ)),         // finish
            };

            foreach (var (index, finish, pos) in defs)
                CreateCheckpoint(root, index, finish, pos);

            Debug.Log("[RaceSceneBuilder] Checkpoints created.");
        }

        private static void CreateCheckpoint(Transform parent, int index, bool isFinish, Vector3 pos)
        {
            var go = new GameObject(isFinish ? "FinishTrigger" : $"Checkpoint_{index}");
            go.transform.SetParent(parent);
            go.transform.position = pos;

            var col  = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size      = new Vector3(TrackWidth + 4f, 8f, 1f);

            var cp = go.AddComponent<RaceCheckpoint>();
            cp.index        = index;
            cp.isFinishLine = isFinish;

            // Visible marker strip on the floor
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Marker";
            quad.transform.SetParent(go.transform);
            quad.transform.localPosition = new Vector3(0, -2.9f, 0);
            quad.transform.localRotation = Quaternion.Euler(90, 0, 0);
            quad.transform.localScale    = new Vector3(TrackWidth, 1f, 1f);
            quad.GetComponent<Renderer>().sharedMaterial =
                CreateMaterial(isFinish ? new Color(1f, 0.8f, 0f) : new Color(0f, 1f, 0.5f));
            Object.DestroyImmediate(quad.GetComponent<Collider>());
        }

        // ── Spawn points ─────────────────────────────────────────────────────────

        private static void BuildSpawnPoints()
        {
            var root   = new GameObject("SpawnPoints").transform;
            float[] xs = { -3f, -1f, 1f, 3f };

            for (int i = 0; i < xs.Length; i++)
            {
                var sp = new GameObject($"Spawn_{i + 1}");
                sp.transform.SetParent(root);
                sp.transform.position = new Vector3(xs[i], 0.1f, 3f);

                // Small disc so you can see the spawn point in editor
                var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disc.transform.SetParent(sp.transform);
                disc.transform.localPosition = Vector3.zero;
                disc.transform.localScale    = new Vector3(0.6f, 0.05f, 0.6f);
                disc.GetComponent<Renderer>().sharedMaterial = CreateMaterial(Color.cyan);
                Object.DestroyImmediate(disc.GetComponent<Collider>());
            }

            Debug.Log("[RaceSceneBuilder] 4 spawn points at Z=3.");
        }

        // ── Game Manager ─────────────────────────────────────────────────────────

        private static void BuildGameManager()
        {
            new GameObject("GameManager").AddComponent<RaceManager>();
            Debug.Log("[RaceSceneBuilder] GameManager + RaceManager created.");
        }

        // ── Camera ───────────────────────────────────────────────────────────────

        private static void BuildCamera()
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags  = CameraClearFlags.Skybox;
            cam.fieldOfView = 65f;
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0, 12f, -15f);
            camGo.transform.rotation = Quaternion.Euler(25f, 0, 0);
        }

        // ── Utilities ─────────────────────────────────────────────────────────────

        private static GameObject Place(string prefabPath, Vector3 pos, Transform parent, string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[RaceSceneBuilder] Missing prefab: {prefabPath}");
                // Fallback: coloured cube so missing assets are visible immediately
                var fb = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fb.name = name + "_MISSING";
                fb.transform.SetParent(parent);
                fb.transform.position = pos;
                fb.transform.localScale = new Vector3(1f, 0.2f, 1f);
                fb.GetComponent<Renderer>().sharedMaterial = CreateMaterial(Color.magenta);
                return fb;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = pos;
            return go;
        }

        private static void AttachGroundQuad(GameObject parent, Vector3 scale, Color color, string name)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            quad.transform.SetParent(parent.transform);
            quad.transform.localPosition = new Vector3(0, 0.51f, 0);
            quad.transform.localRotation = Quaternion.Euler(90, 0, 0);
            quad.transform.localScale    = scale;
            quad.GetComponent<Renderer>().sharedMaterial = CreateMaterial(color);
            Object.DestroyImmediate(quad.GetComponent<Collider>());
        }

        private static Material CreateMaterial(Color color)
        {
            if (s_litShader == null)
                s_litShader = Shader.Find("Universal Render Pipeline/Lit");
            return new Material(s_litShader) { color = color };
        }
    }
}
