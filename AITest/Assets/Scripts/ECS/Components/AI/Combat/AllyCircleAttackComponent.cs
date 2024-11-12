using AI.Combat.ScriptableObjects;

namespace ECS.Components.AI.Combat
{
    public class AllyCircleAttackComponent : AllyAttackComponent
    {
        private float _radius;

        public AllyCircleAttackComponent(uint allyID, AIAllyAttack aiAttack) : base(allyID, aiAttack)
        {
            _radius = aiAttack.attackAoE.GetRadius();
        }

        public float GetRadius()
        {
            return _radius;
        }
    }
}