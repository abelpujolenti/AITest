using ECS.Components.AI.Combat;

namespace ECS.Systems.AI.Combat
{
    public class ReceiveDamageSystem
    {
        public HealthComponent ReceiveDamage(HealthComponent healthComponent, DamageComponent damageComponent)
        {
            //healthComponent.SetHealth(healthComponent.GetHealth() - damageComponent.GetDamage());

            return healthComponent;
        }
    }
}
