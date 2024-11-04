namespace ECS.Components.AI.Combat
{
    public class DamageComponent
    {
        private uint _damage;

        public DamageComponent(uint damage)
        {
            _damage = damage;
        }

        public uint GetDamage()
        {
            return _damage;
        }
    }
}