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
            ECSCombatManager.Instance.OnAllyJoinAlly(ref _aiAlly.GetMoralComponent());
        }

        private void OnTriggerExit(Collider other)
        {
            ECSCombatManager.Instance.OnAllySeparateFromAlly(ref _aiAlly.GetMoralComponent());
        }
    }
}
