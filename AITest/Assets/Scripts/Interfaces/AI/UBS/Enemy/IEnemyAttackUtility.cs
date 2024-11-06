using Interfaces.AI.UBS.BaseInterfaces.Get;
using Interfaces.AI.UBS.BaseInterfaces.Property;

namespace Interfaces.AI.UBS.Enemy
{
    public interface IEnemyAttackUtility : ITarget, IGetAgentTransform, IRivalTransform, IVectorToRival, 
        IGetDistanceToRival, IGetMinimumRangeToAttack, IGetMaximumRangeToAttack
    {}
}