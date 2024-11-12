namespace ECS.Components.AI.Combat
{
    public class ThreatGroupComponent : GroupComponent
    {
        public float groupRadius;

        public ThreatGroupComponent(float threatGroupWeight)
        {
            groupWeight = threatGroupWeight;
        }
    }
}