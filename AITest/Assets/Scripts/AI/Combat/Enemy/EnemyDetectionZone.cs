using ECS.Entities.AI.Combat;
using Managers;
using UnityEngine;

namespace AI.Combat.Enemy
{
    public class EnemyDetectionZone : MonoBehaviour
    {
        [SerializeField] private AIEnemy _aiEnemy;

        private void OnTriggerEnter(Collider other)
        {
            AIEnemy otherEnemy = other.GetComponent<EnemyDetectionZone>().GetAIEnemy();
            
            ECSCombatManager.Instance.OnEnemyJoinEnemy(_aiEnemy, otherEnemy);
        }

        private void OnTriggerExit(Collider other)
        {
            AIEnemy otherEnemy = other.GetComponent<EnemyDetectionZone>().GetAIEnemy();
            
            ECSCombatManager.Instance.OnEnemySeparateFromEnemy(_aiEnemy, otherEnemy);
        }

        public AIEnemy GetAIEnemy()
        {
            return _aiEnemy;
        }
    }
}
