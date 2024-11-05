using Interfaces.AI.UBS.BaseInterfaces.Get;
using Interfaces.AI.UBS.BaseInterfaces.Property;

namespace Interfaces.AI.UBS.Ally
{
    public interface IAllyDodgeAttackUtility : IUnderAttack, IHealth, IGetTotalHealth, IGetBasicAttackDamage, 
        IOncomingAttackDamage, IRivalHealth
    {}
}