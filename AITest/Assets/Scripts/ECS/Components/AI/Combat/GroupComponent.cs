namespace ECS.Components.AI.Combat
{
    public abstract class GroupComponent
    {
        private uint groupTarget;

        public float groupWeight;

        public void SetGroupTarget(uint groupTarget)
        {
            this.groupTarget = groupTarget;
        }

        public uint GetGroupTarget()
        {
            return groupTarget;
        }
    }
}