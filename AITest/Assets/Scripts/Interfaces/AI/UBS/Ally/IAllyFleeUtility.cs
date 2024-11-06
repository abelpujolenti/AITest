using Interfaces.AI.UBS.BaseInterfaces.Get;
using Interfaces.AI.UBS.BaseInterfaces.Property;

namespace Interfaces.AI.UBS.Ally
{
    public interface IAllyFleeUtility : IGetDistancesToThreatGroupsThatThreatMe, IGetRadiusOfAlert, IIsInFleeState, 
        IGetHealth, IGetBasicAttackDamage, IGetMoralWeight, IGetThreatWeightOfTarget, IIsUnderThreat
    {}
}