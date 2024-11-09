using Interfaces.AI.UBS.BaseInterfaces.Get;

namespace Interfaces.AI.UBS.Enemy
{
    public interface IEnemyGetCloserToRivalUtility : IIsStunned, IHasATarget, IIsAttacking, IGetDistanceToRival, IGetMaximumRangeToAttack
    {}
}