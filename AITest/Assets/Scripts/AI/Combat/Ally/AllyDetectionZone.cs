using ECS.Entities.AI.Combat;
using Managers;
using UnityEngine;

namespace AI.Combat.Ally
{
    public class AllyDetectionZone : MonoBehaviour
    {
        [SerializeField] private AIAlly _aiAlly;

        private void OnTriggerEnter(Collider other)
        {
            CombatManager.Instance.OnAllyJoinAlly(ref _aiAlly.GetMoralComponent());
        }

        private void OnTriggerExit(Collider other)
        {
            CombatManager.Instance.OnAllySeparateFromAlly(ref _aiAlly.GetMoralComponent());
        }
    }
}
