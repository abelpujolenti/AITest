using System.Collections.Generic;
using ECS.Components.AI.Combat;
using UnityEngine;

namespace ECS.Systems.AI.Combat
{
    public class SpawnAttackWarnSystem
    {
        public void UpdateAttackAoEColliders(ref Dictionary<AttackComponent, Collider> attacksColliders)
        {
            foreach (KeyValuePair<AttackComponent, Collider> attackCollider in attacksColliders)
            {
                AttackComponent attackComponent = attackCollider.Key;
                
                if (!attackComponent.IsOnCooldown())
                {
                    continue;
                }

                if (!attackComponent.IsCasting())
                {
                    attackComponent.DecreaseCurrentCastTime();
                    continue;
                }
            }
        }
    }
}