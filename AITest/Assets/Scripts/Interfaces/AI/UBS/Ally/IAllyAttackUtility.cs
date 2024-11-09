using Interfaces.AI.UBS.BaseInterfaces.Get;

namespace Interfaces.AI.UBS.Ally
{
    public interface IAllyAttackUtility : IHasATarget, IGetDistanceToRival, IGetMinimumRangeToAttack, 
        IGetMaximumRangeToAttack, IGetVectorToRival, IGetAgentTransform, IGetRivalTransform, IIsInAttackState, 
        IGetCanDefeatEnemy, IGetCanStunEnemy
    {}
}