using AI.Combat.ScriptableObjects;

namespace ECS.Components.AI.Combat
{
    public class CircleAttackComponent : AttackComponent
    {
        private float _radius;

        public CircleAttackComponent(AIAttack aiAttack) : base(aiAttack)
        {
            _radius = aiAttack.attackAoE.GetRadius();
        }

        public float GetRadius()
        {
            return _radius;
        }
    }
}