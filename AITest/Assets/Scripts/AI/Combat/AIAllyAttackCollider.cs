using System.Collections.Generic;
using ECS.Entities.AI.Combat;

namespace AI.Combat
{
    public abstract class AIAllyAttackCollider : AIAttackCollider
    {
        protected uint _allyID;
        
        protected List<AIEnemy> _combatAgentsTriggering = new List<AIEnemy>();

        protected override void OnDisable()
        {
            _combatAgentsTriggering.Clear();
        }
    }
}