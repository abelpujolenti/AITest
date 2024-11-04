using System.Collections.Generic;
using ECS.Components.AI.Combat;
using ECS.Components.AI.Navigation;
using UnityEngine;

namespace ECS.Systems.AI.Combat
{
    public class ThreatSystem
    {
        public void UpdateThreatGroupsBarycenter(
            ref Dictionary<uint, KeyValuePair<List<ThreatComponent>, List<TransformComponent>>> groupThreatsComponents, 
            ref Dictionary<uint, KeyValuePair<GroupThreatWeightComponent, VectorComponent>> groupThreatWeightAndOrigin)
        {
            for (uint i = 1; i < groupThreatsComponents.Count; i++)
            {
                if (groupThreatsComponents[i].Value.Count == 0)
                {
                    continue;
                }
                VectorComponent vectorComponent = ReturnThreatBarycenter(groupThreatsComponents[i].Value);
                groupThreatWeightAndOrigin[i].Value.SetDestination(vectorComponent.GetPosition());
            }
        }

        private VectorComponent ReturnThreatBarycenter(List<TransformComponent> transformComponents)
        {
            Vector3 XZposition = new Vector3();

            foreach (TransformComponent transformComponent in transformComponents)
            {
                XZposition += transformComponent.GetTransform().position;
            }

            XZposition /= transformComponents.Count;

            return new VectorComponent(XZposition);
        }

        public void AddThreat(
            ref Dictionary<uint, KeyValuePair<List<ThreatComponent>, List<TransformComponent>>> groupThreatsComponents, 
            ref Dictionary<uint, KeyValuePair<GroupThreatWeightComponent, VectorComponent>> groupThreatWeightAndOrigin, 
            ThreatComponent threatComponent, TransformComponent transformComponent)
        {
            uint threatGroup = threatComponent.GetOriginalThreatGroup();

            List<ThreatComponent> threatComponents = new List<ThreatComponent>();
            List<TransformComponent> transformComponents = new List<TransformComponent>();
            
            groupThreatsComponents.Add(threatGroup, 
                new KeyValuePair<List<ThreatComponent>, List<TransformComponent>>(threatComponents, transformComponents));
            
            groupThreatsComponents[threatGroup].Key.Add(threatComponent);
            groupThreatsComponents[threatGroup].Value.Add(transformComponent);

            GroupThreatWeightComponent groupThreatWeightComponent = new GroupThreatWeightComponent(0);
            VectorComponent vectorComponent = new VectorComponent(new Vector3());
            
            groupThreatWeightAndOrigin.Add(threatGroup, 
                new KeyValuePair<GroupThreatWeightComponent, VectorComponent>(groupThreatWeightComponent, vectorComponent));
            groupThreatWeightAndOrigin[threatGroup].Key.groupThreatWeight = threatComponent.GetWeight();
        }

        public void EraseThreat(
            ref Dictionary<uint, KeyValuePair<List<ThreatComponent>, List<TransformComponent>>> groupThreatsComponents, 
            ref Dictionary<uint, KeyValuePair<GroupThreatWeightComponent, VectorComponent>> groupThreatWeightAndOrigin, 
            ThreatComponent threatComponent)
        {
            uint threatGroup = threatComponent.GetOriginalThreatGroup();

            List<ThreatComponent> threatComponents = groupThreatsComponents[threatGroup].Key;

            if (threatComponents.Count != 0)
            {
                uint threatGroupToMove = FindLowestGroupIndex(threatGroup, threatComponents);
                
                MoveWholeThreatGroupToAnotherThreatGroup(ref groupThreatsComponents, ref groupThreatWeightAndOrigin, 
                    threatGroup, threatGroupToMove);
            }

            groupThreatsComponents.Remove(threatGroup);
        }

        private uint FindLowestGroupIndex(uint threatGroupFromWhichTheyCome, List<ThreatComponent> threatComponents)
        {
            uint lowestGroupIndex = threatGroupFromWhichTheyCome;

            foreach (ThreatComponent threatComponent in threatComponents)
            {
                uint currentThreatGroup = threatComponent.GetOriginalThreatGroup();
                if (currentThreatGroup - 1 == lowestGroupIndex)
                {
                    return currentThreatGroup;
                }

                if (currentThreatGroup > lowestGroupIndex)
                {
                    continue;
                }

                lowestGroupIndex = currentThreatGroup;
            }

            return lowestGroupIndex;
        }

        public void MoveWholeThreatGroupToAnotherThreatGroup(
            ref Dictionary<uint, KeyValuePair<List<ThreatComponent>, List<TransformComponent>>> groupThreatsComponents, 
            ref Dictionary<uint, KeyValuePair<GroupThreatWeightComponent, VectorComponent>> groupThreatWeightAndOrigin, 
            uint threatGroupFromWhichTheyCome, uint threatGroupToMove)
        {
            for (int i = 0; i < groupThreatsComponents[threatGroupFromWhichTheyCome].Key.Count; i++)
            {
                MoveSingleThreatToAnotherThreatGroup(ref groupThreatsComponents, ref groupThreatWeightAndOrigin, 
                    groupThreatsComponents[threatGroupFromWhichTheyCome].Key[i], threatGroupFromWhichTheyCome, threatGroupToMove);
            }
        }

        public void MoveGivenThreatsToAnotherThreatGroup(
            ref Dictionary<uint, KeyValuePair<List<ThreatComponent>, List<TransformComponent>>> groupThreatsComponents,
            ref Dictionary<uint, KeyValuePair<GroupThreatWeightComponent, VectorComponent>> groupThreatWeightAndOrigin,
            List<ThreatComponent> threatComponentsToMove, uint threatGroupFromWhichTheyCome, uint threatGroupToMove)
        {
            foreach (ThreatComponent threatComponent in threatComponentsToMove)
            {
                MoveSingleThreatToAnotherThreatGroup(ref groupThreatsComponents, ref groupThreatWeightAndOrigin, 
                    threatComponent, threatGroupFromWhichTheyCome, threatGroupToMove);
            }
        }

        private void MoveSingleThreatToAnotherThreatGroup(
            ref Dictionary<uint, KeyValuePair<List<ThreatComponent>, List<TransformComponent>>> groupThreatsComponents,
            ref Dictionary<uint, KeyValuePair<GroupThreatWeightComponent, VectorComponent>> groupThreatWeightAndOrigin,
            ThreatComponent threatComponent, uint threatGroupFromWhichComes, uint threatGroupToMove)
        {
            int threatComponentListIndex = groupThreatsComponents[threatGroupFromWhichComes].Key.IndexOf(threatComponent);

            if (threatComponentListIndex == -1)
            {
                return;
            }
            
            MoveThreatComponentToThreatGroup(ref groupThreatsComponents, ref groupThreatWeightAndOrigin,
                groupThreatsComponents[threatGroupFromWhichComes].Key[threatComponentListIndex],
                threatGroupFromWhichComes, threatGroupToMove);
                    
            MoveTransformComponentToThreatGroup(ref groupThreatsComponents, 
                groupThreatsComponents[threatGroupFromWhichComes].Value[threatComponentListIndex],
                threatGroupFromWhichComes, threatGroupToMove);
        }

        private void MoveThreatComponentToThreatGroup(
            ref Dictionary<uint, KeyValuePair<List<ThreatComponent>, List<TransformComponent>>> groupThreatsComponents, 
            ref Dictionary<uint, KeyValuePair<GroupThreatWeightComponent, VectorComponent>> groupThreatWeightAndOrigin, 
            ThreatComponent threatComponent, uint threatGroupFromWhichTheyCome, uint threatGroupToMove)
        {
            groupThreatsComponents[threatGroupFromWhichTheyCome].Key.Remove(threatComponent);
            groupThreatWeightAndOrigin[threatGroupFromWhichTheyCome].Key.groupThreatWeight -=
                threatComponent.GetWeight();
            
            threatComponent.currentThreatGroup = threatGroupToMove;
            groupThreatsComponents[threatGroupToMove].Key.Add(threatComponent);
            groupThreatWeightAndOrigin[threatGroupToMove].Key.groupThreatWeight +=
                threatComponent.GetWeight();
        }

        private void MoveTransformComponentToThreatGroup(
            ref Dictionary<uint, KeyValuePair<List<ThreatComponent>, List<TransformComponent>>> groupThreatsComponents, 
            TransformComponent transformComponent, uint threatGroupFromWhichTheyCome, uint threatGroupToMove)
        {
            groupThreatsComponents[threatGroupFromWhichTheyCome].Value.Remove(transformComponent);
            
            groupThreatsComponents[threatGroupToMove].Value.Add(transformComponent);
        }
    }
}