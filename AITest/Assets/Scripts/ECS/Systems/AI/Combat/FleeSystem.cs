using System.Collections.Generic;
using ECS.Components.AI.Navigation;
using ECS.Entities.AI.Combat;
using Managers;
using UnityEngine;

namespace ECS.Systems.AI.Combat
{
    public class FleeSystem
    {
        //ERASE!!!!
        public List<Vector3> GetTerrainPositions(List<GameObject> FLEE_POINTS)
        {
            List<Vector3> points = new List<Vector3>();
            
            RaycastHit hit;

            foreach (GameObject gameObject in FLEE_POINTS)
            {
                Ray ray = new Ray(gameObject.transform.position, Vector3.down);
                if (Physics.Raycast(ray, out hit))
                {
                    points.Add(hit.point);
                }
            }

            return points;

        }
        
        public void EvaluateClosesPoint(ref Dictionary<AICombatAgentEntity, int> FLEE_POINTS_RECORD, 
            List<Vector3> FLEE_POINTS, AICombatAgentEntity combatAgentNeedsToFlee)
        {
            float closestDistance = 300000;
            
            Vector3 agentPosition = combatAgentNeedsToFlee.transform.position;
            Vector3 destination = new Vector3();

            foreach (Vector3 position in FLEE_POINTS)
            {
                float currentDistance = (agentPosition- position).magnitude;
                if (currentDistance > closestDistance)
                {
                    continue;
                }

                closestDistance = currentDistance;
                destination = position;
            }
            
            ECSNavigationManager.Instance.UpdateNavMeshAgentVectorDestination(combatAgentNeedsToFlee.GetNavMeshAgentComponent(), 
                new VectorComponent(destination));
        }

        public void UpdateFleeMovement(ref Dictionary<AIAlly, int> FLEE_POINTS_RECORD, 
            List<Vector3> FLEE_POINTS)
        {
            foreach (var combatAgentFleeing in FLEE_POINTS_RECORD)
            {
                AIAlly combatAgent = combatAgentFleeing.Key;
                int index = combatAgentFleeing.Value;

                if ((combatAgent.transform.position - FLEE_POINTS[index]).magnitude < 8)
                {
                    int newIndex = (combatAgentFleeing.Value + 1) % FLEE_POINTS.Count;

                    FLEE_POINTS_RECORD[combatAgent] = newIndex;
                    
                    ECSNavigationManager.Instance.UpdateNavMeshAgentVectorDestination(combatAgent.GetNavMeshAgentComponent(), 
                        new VectorComponent(FLEE_POINTS[newIndex]));
                }
            }
        }
        //
    }
}