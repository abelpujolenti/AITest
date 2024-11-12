using AI.Combat.ScriptableObjects;

namespace ECS.Components.AI.Combat
{
    public class AllyAttackComponent : AttackComponent
    {
        private uint _allyID; 
            
        private float _stressDamage;

        protected AllyAttackComponent(uint allyID, AIAllyAttack aiAllyAttack) : base(aiAllyAttack)
        {
            _allyID = allyID;
            _stressDamage = aiAllyAttack.stressDamage;
        }

        public float GetStressDamage()
        {
            return _stressDamage;
        }

        public uint GetAllyID()
        {
            return _allyID;
        }
    }
}