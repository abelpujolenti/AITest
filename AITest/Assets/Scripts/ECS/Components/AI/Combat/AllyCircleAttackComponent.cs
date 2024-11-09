using AI.Combat.ScriptableObjects;

namespace ECS.Components.AI.Combat
{
    public class AllyCircleAttackComponent : AllyAttackComponent
    {
        private float _radius;

        public AllyCircleAttackComponent(AIAllyAttack aiAttack) : base(aiAttack)
        {
            _radius = aiAttack.attackAoE.GetRadius();
        }

        public float GetRadius()
        {
            return _radius;
        }
    }
}