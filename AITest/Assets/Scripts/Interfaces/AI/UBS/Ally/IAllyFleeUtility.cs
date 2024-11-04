using Interfaces.AI.UBS.BaseInterfaces;

namespace Interfaces.AI.UBS.Ally
{
    public interface IAllyFleeUtility : IIsSeeingARival, IIsInFleeState, IHealth, IBasicAttackDamage, IMoralWeight, 
        IThreatWeightOfTarget, IUnderThreat
    {}
}