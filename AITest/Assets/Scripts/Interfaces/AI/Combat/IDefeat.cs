using ECS.Components.AI.Combat;

namespace Interfaces.AI.Combat
{
    public interface IDefeat
    {
        public void Defeat(DefeatComponent defeatComponent);
    }
}
