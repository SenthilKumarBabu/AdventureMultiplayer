using System.Collections.Generic;
using UnityEngine;

namespace AdventureMultiplayer
{
    public enum ObstacleType { Triangle, RingStack, Spinner, Puncher, Dumbbell, SpikeRoller, RotatingBeam, Barrel, WreckingBall, Axe, Hammer, RotatingLog, Boulder, RotatingPillar, Laser, HorizontalPusher }

    [CreateAssetMenu(menuName = "Adventure Multiplayer/Obstacle Knockback Database", fileName = "ObstacleKnockbackDatabase")]
    public class ObstacleKnockbackDatabase : ScriptableObject
    {
        [System.Serializable]
        public class TypeConfig
        {
            public ObstacleType type;
            [Min(0f)] public float lateralForce      = 20f;
            [Min(0f)] public float upwardForce       = 12f;
            [Min(0)]  public int   damage            = 0;
                      public bool  pureKnockback     = true;
            [Min(0f)] public float knockbackCooldown = 1f;
        }

        [System.Serializable]
        public class MeshMapping
        {
            public string      meshName;
            public ObstacleType type;
        }

        [SerializeField] private TypeConfig[]  configs;
        [SerializeField] private MeshMapping[] meshMappings;

        public TypeConfig GetConfig(ObstacleType type)
        {
            if (configs == null) return null;
            foreach (var c in configs)
                if (c.type == type) return c;
            return null;
        }

        // Returns the ObstacleType for a given mesh name, or null if not mapped.
        public ObstacleType? GetTypeFromMesh(string meshName)
        {
            if (meshMappings == null) return null;
            foreach (var m in meshMappings)
                if (m.meshName == meshName) return m.type;
            return null;
        }

        private void OnValidate()
        {
            if (configs == null) return;
            var seen = new HashSet<ObstacleType>();
            foreach (var c in configs)
            {
                if (!seen.Add(c.type))
                    Debug.LogWarning($"[ObstacleKnockbackDatabase] Duplicate entry for type '{c.type}' — only the first will be used.", this);
            }
        }
    }
}
