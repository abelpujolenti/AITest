using Interfaces.AI.UBS.BaseInterfaces.Get;
using Interfaces.AI.UBS.BaseInterfaces.Property;

namespace Interfaces.AI.UBS.Ally
{
    public interface IAllyFleeUtility : ITarget, IInFleeState, IHealth, IGetBasicAttackDamage, IMoralWeight, 
        IThreatWeightOfTarget, IUnderThreat
    {}
}