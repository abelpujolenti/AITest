using Interfaces.AI.UBS.BaseInterfaces.Get;

namespace Interfaces.AI.UBS.Ally
{
    public interface IAllyFleeUtility : IGetDistancesToThreatGroupsThatThreatMe, IGetRadiusOfAlert, IIsInFleeState, 
        IGetHealth, IGetBasicAttackDamage, IGetMoralWeight, IGetThreatWeightOfTarget, IIsUnderThreat
    {}
}