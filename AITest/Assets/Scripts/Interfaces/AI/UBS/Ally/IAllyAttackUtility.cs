using Interfaces.AI.UBS.BaseInterfaces;

namespace Interfaces.AI.UBS.Ally
{
    public interface IAllyAttackUtility : IHasATarget, IBasicAttackMaxRange, IDistanceToEnemy, IAgentTransform, IRivalTransform, 
        IIsInAttackState, IBasicAttackDamage, IRivalHealth, IBasicStressDamage, IRivalStressRemainingToStun
    {}
}