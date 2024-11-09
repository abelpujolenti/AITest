using AI.Combat.ScriptableObjects;

namespace ECS.Components.AI.Combat
{
    public class AllyAttackComponent : AttackComponent
    {
        private float _stressDamage;

        protected AllyAttackComponent(AIAllyAttack aiAllyAttack) : base(aiAllyAttack)
        {
            _stressDamage = aiAllyAttack.stressDamage;
        }

        public float GetStressDamage()
        {
            return _stressDamage;
        }
    }
}