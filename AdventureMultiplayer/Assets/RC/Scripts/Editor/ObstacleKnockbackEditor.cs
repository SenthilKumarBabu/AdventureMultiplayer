using UnityEditor;
using UnityEngine;

namespace AdventureMultiplayer.Editor
{
    [CustomEditor(typeof(ObstacleKnockback))]
    public class ObstacleKnockbackEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("database"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("obstacleType"));

            EditorGUILayout.Space();

            // Direction override — editable per-instance (for HorizontalPusher, Laser, etc.)
            var overrideProp = serializedObject.FindProperty("overrideDirection");
            EditorGUILayout.PropertyField(overrideProp, new GUIContent("Override Direction"));
            if (overrideProp.boolValue)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("customDirection"), new GUIContent("Custom Direction"));

            serializedObject.ApplyModifiedProperties();

            // Auto-Detect button — reads mesh name and looks it up in the database mapping.
            var db2 = serializedObject.FindProperty("database").objectReferenceValue as ObstacleKnockbackDatabase;
            if (db2 != null)
            {
                var target_ = (ObstacleKnockback)target;
                var mf = target_.GetComponent<UnityEngine.MeshFilter>()
                      ?? target_.GetComponentInParent<UnityEngine.MeshFilter>()
                      ?? target_.GetComponentInChildren<UnityEngine.MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    var detected = db2.GetTypeFromMesh(mf.sharedMesh.name);
                    if (detected.HasValue)
                    {
                        var currentIdx = serializedObject.FindProperty("obstacleType").enumValueIndex;
                        if (currentIdx != (int)detected.Value)
                        {
                            EditorGUILayout.HelpBox($"Mesh '{mf.sharedMesh.name}' maps to '{detected.Value}' — click to apply.", MessageType.Info);
                            if (GUILayout.Button($"Auto-Detect: Set to '{detected.Value}'"))
                            {
                                serializedObject.Update();
                                serializedObject.FindProperty("obstacleType").enumValueIndex = (int)detected.Value;
                                serializedObject.ApplyModifiedProperties();
                            }
                        }
                        else
                        {
                            EditorGUILayout.HelpBox($"✓ Type matches mesh mapping ('{mf.sharedMesh.name}').", MessageType.None);
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox($"Mesh '{mf.sharedMesh.name}' not in mapping — set type manually.", MessageType.Warning);
                    }
                }
            }

            EditorGUILayout.Space();

            // Read-only preview of the current type's values from the database.
            var db = serializedObject.FindProperty("database").objectReferenceValue as ObstacleKnockbackDatabase;
            if (db == null)
            {
                EditorGUILayout.HelpBox("Assign an ObstacleKnockbackDatabase to configure this obstacle.", MessageType.Info);
                return;
            }

            var type   = (ObstacleType)serializedObject.FindProperty("obstacleType").enumValueIndex;
            var config = db.GetConfig(type);

            EditorGUILayout.LabelField($"Values for '{type}' (read-only — edit in Database asset)", EditorStyles.boldLabel);

            if (config == null)
            {
                EditorGUILayout.HelpBox($"No entry for '{type}' in the database. Open the ScriptableObject and add it.", MessageType.Warning);
                return;
            }

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.FloatField("Lateral Force",      config.lateralForce);
            EditorGUILayout.FloatField("Upward Force",       config.upwardForce);
            EditorGUILayout.IntField("Damage",               config.damage);
            EditorGUILayout.Toggle("Pure Knockback",         config.pureKnockback);
            EditorGUILayout.FloatField("Knockback Cooldown", config.knockbackCooldown);
            EditorGUI.EndDisabledGroup();
        }
    }
}
