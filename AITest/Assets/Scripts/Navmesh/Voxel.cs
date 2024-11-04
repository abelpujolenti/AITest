using UnityEngine;

namespace AI
{
    public struct Voxel
    {
        public Vector3 position;
        public int isWalkable;
        public int isCeil;

        public Voxel(Vector3 position)
        {
            this.position = position;
            isWalkable = 0;
            isCeil = 0;
        }
    }
}