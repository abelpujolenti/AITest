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
            CombatManager.Instance.OnEnemyJoinEnemy(_aiEnemy, other.GetComponent<EnemyDetectionZone>().GetAIEnemy().GetCombatAgentInstance());
        }

        private void OnTriggerExit(Collider other)
        {
            CombatManager.Instance.OnEnemySeparateFromEnemy(_aiEnemy, other.GetComponent<EnemyDetectionZone>().GetAIEnemy().GetCombatAgentInstance());
        }

        private AIEnemy GetAIEnemy()
        {
            return _aiEnemy;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            
            Gizmos.DrawWireSphere(transform.position, 8);
        }
    }
}
