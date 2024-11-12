using Interfaces.AI.Combat;

namespace ECS.Components.AI.Combat
{
    public class MoralComponent : IStatWeight, IGroup
    {
        private static uint _globalMoralIndex = 1;
        
        private const float MIN_MORAL_WEIGHT = 0;
        private const float MAX_MORAL_WEIGHT = 100;
        
        private float _moralWeight;

        private uint _originalGroup;
        public uint currentGroup;

        public MoralComponent(float moralWeight)
        {
            _moralWeight = moralWeight;
            _originalGroup = _globalMoralIndex;
            currentGroup = _originalGroup;
            _globalMoralIndex++;
        }

        public void SetMoralWeight(float moralWeight)
        {
            _moralWeight = moralWeight;
        }

        public float GetWeight()
        {
            return _moralWeight;
        }

        public uint GetOriginalGroup()
        {
            return _originalGroup;
        }

        public uint GetCurrentGroup()
        {
            return currentGroup;
        }
    }
}