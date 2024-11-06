namespace ECS.Components.AI.Combat
{
    public class ThreatGroupComponent
    {
        public uint groupTarget;
        
        public float threatGroupWeight;
        public float groupRadius;

        public ThreatGroupComponent(float threatGroupWeight)
        {
            this.threatGroupWeight = threatGroupWeight;
        }
    }
}