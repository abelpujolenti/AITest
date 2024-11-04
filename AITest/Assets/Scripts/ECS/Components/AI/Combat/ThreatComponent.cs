using System;
using Interfaces.AI.Combat;

namespace ECS.Components.AI.Combat
{
    [Serializable]
    public class ThreatComponent : IStatWeight, IGroup
    {
        private static uint _globalThreatIndex = 1;
        
        private uint _originalThreatGroup;
        public uint currentThreatGroup;
        
        private float _threatWeight;

        public ThreatComponent(float threatWeight)
        {
            _originalThreatGroup = _globalThreatIndex;
            currentThreatGroup = _originalThreatGroup;
            _threatWeight = threatWeight;
            _globalThreatIndex++;
        }

        public uint GetOriginalThreatGroup()
        {
            return _originalThreatGroup;
        }

        public float GetWeight()
        {
            return _threatWeight;
        }

        public uint GetOriginalGroup()
        {
            return _originalThreatGroup;
        }

        public uint GetCurrentGroup()
        {
            return currentThreatGroup;
        }
    }
}