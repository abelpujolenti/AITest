using System;
using Interfaces.AI.Combat;
using UnityEngine;

namespace AI.Combat
{
    public enum AIAttackAoEType
    {
        RECTANGLE_AREA,
        CIRCLE_AREA,
        CONE_AREA
    }
    
    [Serializable]
    public class AIAttackAoE : IRectangleAttack, ICircleAttack, IConeAttack 
    {
        public AIAttackAoEType aiAttackAoEType;
        
        public Vector3 direction;

        public float length;
        public float width;
        
        public float radius;

        public float degrees;

        public float GetLength()
        {
            return length;
        }
        
        public float GetWideness()
        {
            return width;
        }

        public float GetRadius()
        {
            return radius;
        }

        public Vector3 GetDirection()
        {
            return direction;
        }

        public float GetDegrees()
        {
            return degrees;
        }
    }
}