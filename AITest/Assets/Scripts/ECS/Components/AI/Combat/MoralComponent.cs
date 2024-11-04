using Interfaces.AI.Combat;

namespace ECS.Components.AI.Combat
{
    public class MoralComponent : IStatWeight, IGroup
    {
        private static uint _globalMoralIndex = 0;
        
        private const float MIN_MORAL_WEIGHT = 0;
        private const float MAX_MORAL_WEIGHT = 1;
        
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

        public float GetWeight()
        {
            return _moralWeight;
        }

        public void AddMinMoralWeight()
        {
            AddToMoralWeight(MIN_MORAL_WEIGHT);
        }

        public void SubtractMinMoralWeight()
        {
            AddToMoralWeight(-MIN_MORAL_WEIGHT);
        }

        private void AddToMoralWeight(float moralToAdd)
        {
            float temporaryMoral = _moralWeight + moralToAdd;
            if (temporaryMoral < MIN_MORAL_WEIGHT || temporaryMoral > MAX_MORAL_WEIGHT)
            {
                return;
            }

            _moralWeight = temporaryMoral;
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