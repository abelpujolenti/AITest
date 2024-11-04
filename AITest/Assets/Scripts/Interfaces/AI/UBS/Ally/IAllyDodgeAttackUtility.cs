using Interfaces.AI.UBS.BaseInterfaces;

namespace Interfaces.AI.UBS.Ally
{
    public interface IAllyDodgeAttackUtility : IUnderAttack, IHealth, ITotalHealth, IBasicAttackDamage, 
        IOncomingAttackDamage, IRivalHealth
    {}
}