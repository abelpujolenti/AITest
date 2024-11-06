using ECS.Entities.AI.Combat;
using UnityEngine;

namespace AI.Combat.Enemy
{
    public class EnemyDetectionZone : MonoBehaviour
    {
        [SerializeField] private AIEnemy _aiEnemy;

        private void OnTriggerEnter(Collider other)
        {
            _aiEnemy.AddOverlappingEnemyID(other.GetComponent<EnemyDetectionZone>().GetAIEnemy().GetCombatAgentInstance());
        }

        private void OnTriggerExit(Collider other)
        {
            _aiEnemy.RemoveOverlappingEnemy(other.GetComponent<EnemyDetectionZone>().GetAIEnemy().GetCombatAgentInstance());
        }

        private AIEnemy GetAIEnemy()
        {
            return _aiEnemy;
        }
    }
}
