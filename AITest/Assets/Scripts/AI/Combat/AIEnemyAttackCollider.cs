using System;
using System.Collections.Generic;
using ECS.Entities.AI.Combat;
using Unity.AI.Navigation;
using UnityEngine;

namespace AI.Combat
{
    public abstract class AIEnemyAttackCollider : AIAttackCollider
    {
        protected bool _isWarning;

        [SerializeField] protected List<NavMeshModifierVolume> _navMeshModifierVolumes = 
            new List<NavMeshModifierVolume>();
        
        protected List<AIAlly> _combatAgentsTriggering = new List<AIAlly>();

        public abstract Vector2[] GetCornerPoints();

        private uint _areaType = 0;

        protected bool _isSubscribed;

        public bool HasCombatAgentsTriggering()
        {
            return _combatAgentsTriggering.Count != 0;
        }

        protected override void OnDisable()
        {
            SetWalkable();
            _combatAgentsTriggering.Clear();
            _isSubscribed = false;
        }

        private void SetWalkable()
        {
            foreach (NavMeshModifierVolume navMeshModifierVolume in _navMeshModifierVolumes)
            {
                navMeshModifierVolume.area = 0;
            }

            _areaType = 0;
        }

        public bool IsWalkable()
        {
            return !Convert.ToBoolean(_areaType);
        }

        public void SetNotWalkable()
        {
            
            foreach (NavMeshModifierVolume navMeshModifierVolume in _navMeshModifierVolumes)
            {
                navMeshModifierVolume.area = 1;
            }

            _areaType = 1;
        }
    }
}