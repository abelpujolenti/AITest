using Interfaces.AI.UBS.BaseInterfaces.Get;
using Interfaces.AI.UBS.BaseInterfaces.Property;

namespace Interfaces.AI.UBS.Enemy
{
    public interface IEnemyGetCloserToRivalUtility : ITarget, IAttacking, IGetDistanceToRival, IGetMinimumRangeToAttack, 
        IGetMaximumRangeToAttack
    {}
}