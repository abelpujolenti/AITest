using Interfaces.AI.UBS.BaseInterfaces.Get;

namespace Interfaces.AI.UBS.Ally
{
    public interface IAllyGetCloserToRivalUtility : IHasATarget, IIsAttacking, IGetMoralWeight, IGetThreatWeightOfTarget, 
        IGetDistanceToRival, IGetMinimumRangeToAttack, IGetMaximumRangeToAttack, IIsUnderAttack
    {}
}