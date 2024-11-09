namespace ECS.Components.AI.Combat
{
    public class AllyDamageComponent : DamageComponent
    {
        private uint _damage;
        private float _stressDamage;

        public AllyDamageComponent(uint damage, float stressDamage) : base(damage)
        {
            _stressDamage = stressDamage;
        }

        public float GetStressDamage()
        {
            return _stressDamage;
        }
    }
}