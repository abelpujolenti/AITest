using Interfaces.AI.UBS.BaseInterfaces.Get;
using Interfaces.AI.UBS.BaseInterfaces.Property;

namespace Interfaces.AI.UBS.Ally
{
    public interface IAllyGetCloserToRivalUtility : ITarget, IAttacking, IMoralWeight, IThreatWeightOfTarget, 
        IGetBasicAttackMaximumRange, IDistanceToRival
    {}
}