using Interfaces.AI.UBS.BaseInterfaces.Get;

namespace Interfaces.AI.UBS.Enemy
{
    public interface IEnemyAttackUtility : IIsStunned, IHasATarget, IGetAgentTransform, IGetRivalTransform, IGetVectorToRival, 
        IGetDistanceToRival, IGetMinimumRangeToAttack, IGetMaximumRangeToAttack
    {}
}