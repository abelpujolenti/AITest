using System.Collections;
using AI.Combat;
using ECS.Components.AI.Combat;
using Managers;

namespace ECS.Systems.AI.Combat
{
    public class AttackSystem
    {
        public IEnumerator StartCastTime(AttackComponent attackComponent, AIAttackCollider attackCollider)
        {
            CombatManager.Instance.PutAttackOnCooldown(attackComponent);
            while (attackComponent.IsCasting())
            {
                attackComponent.DecreaseCurrentCastTime();
                yield return null;
            }
            
            attackCollider.Deactivate();
        }

        public IEnumerator StartCooldown(AttackComponent attackComponent)
        {
            attackComponent.StartCooldown();
            while (attackComponent.IsOnCooldown())
            {
                attackComponent.DecreaseCooldown();
                yield return null;
            }
        }
    }
}