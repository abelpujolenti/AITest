using UnityEngine;

namespace AI
{
    [CreateAssetMenu(fileName = "NavMesh Agent", menuName = "ScriptableObjects/AI/NavMesh Agent", order = 1)]
    public class NavMeshAgentSpecs : ScriptableObject
    {
        [Min(0.05f)]
        public float radius;
        
        [Min(0.001f)]
        public float height;
        
        [Min(0.001f)]
        public float stepHeight;
        
        [Range(0, 45)]
        public uint maxSlope;

        [Min(1f)] 
        public float rotationSpeed;
    }
}
