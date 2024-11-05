namespace ECS.Systems.AI.Combat
{
    public class CombatSystem
    {
        /*public void UpdateCombatState(Dictionary<AICombatAgentEntity, AICombatAgentEntity> aiCombatAgentEntities)
        {
            foreach (KeyValuePair<AICombatAgentEntity, AICombatAgentEntity> attackerAndTarget in aiCombatAgentEntities)
            {
                AICombatAgentEntity target = attackerAndTarget.Value;
                
                if (target == null)
                {
                    continue;
                }
                
                AICombatAgentEntity attacker = attackerAndTarget.Key;

                Transform attackerTransform = attacker.transform;

                Vector3 relativePosition = target.transform.position - attackerTransform.position;

                if (Vector3.Angle(attackerTransform.forward, relativePosition) > 15f)
                {
                    continue;
                }
                
                foreach (AttackComponent attackComponent in attacker.GetAttackComponents())
                {
                    if (attackComponent.IsOnCooldown() || attackComponent.IsCasting())
                    {
                        continue;
                    }
                    
                    float minimumRange = attackComponent.GetMinimumRangeCast();
                    float maximumRange = attackComponent.GetMaximumRangeCast();

                    float distance = relativePosition.magnitude - target.GetRadius(); 

                    if (distance > maximumRange || distance < minimumRange)
                    {
                        continue;
                    }
                    
                    attackComponent.SetRelativePositionIfPossible(relativePosition);
                    
                    attackComponent.StartCastTime();
                    
                    CombatManager.Instance.StartCastingAnAttack(attackerTransform, attackComponent);
                    
                    //ERASE IT WHEN WE'LL USE COLLIDERS
                    ApplyDamage(ref attackerAndTarget.Value.GetHealthComponent(), new DamageComponent(attackComponent.GetDamage()));
                    
                    Debug.Log(attackerAndTarget.Key.name + " attacked");

                    if (attackerAndTarget.Value.GetHealthComponent().health <= 0)
                    {
                        Debug.Log(attackerAndTarget.Value.name + " dead");
                        ECSCombatManager.Instance.RequestRival(attackerAndTarget.Key);
                    }
                    
                }
            }
        }*/
    }
}