namespace Interfaces.AI.Combat
{
    public interface IGroupTarget
    {
        public void SetGroupTarget(uint groupTarget);
        
        public uint GetGroupTarget();
    }
}