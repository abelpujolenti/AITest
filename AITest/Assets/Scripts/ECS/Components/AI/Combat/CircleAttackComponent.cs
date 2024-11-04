using AI.Combat.ScriptableObjects;
using Interfaces.AI.Combat;

namespace ECS.Components.AI.Combat
{
    public class CircleAttackComponent : AttackComponent
    {
        private float _radius;

        public CircleAttackComponent(AIAttack aiAttack, ICircleAttack aiAttackAoE) : base(aiAttack)
        {
            _radius = aiAttackAoE.GetRadius();
        }

        public float GetRadius()
        {
            return _radius;
        }
    }
}