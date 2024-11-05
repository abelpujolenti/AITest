using Interfaces.AI.UBS.BaseInterfaces.Get;
using Interfaces.AI.UBS.BaseInterfaces.Property;

namespace Interfaces.AI.UBS.Ally
{
    public interface IAllyAttackUtility : ITarget, IGetBasicAttackMaximumRange, IDistanceToRival, IVectorToRival, 
        IGetAgentTransform, IRivalTransform, IInAttackState, IGetBasicAttackDamage, IRivalHealth, IGetBasicStressDamage, 
        IRivalMaximumStress, IRivalCurrentStress
    {}
}