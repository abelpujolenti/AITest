using Interfaces.AI.UBS.BaseInterfaces.Get;

namespace Interfaces.AI.UBS.Ally
{
    public interface IAllyHelpAnotherMoralGroupUtility : IGetHealth, IHasATarget, IGetDistanceToRival, 
        IGetMinimumRangeToAttack, IGetMaximumRangeToAttack, IGetAgentTransform, IGetVectorToRival, 
        IIsAnotherMoralGroupUnderThreat, IIsFighting
    {}
}