using Interfaces.AI.UBS.BaseInterfaces.Get;

namespace Interfaces.AI.UBS.Ally
{
    public interface IAllyHelpAllyUtility : IGetHealth, IIsAnotherAllyUnderThreat, IIsFighting
    {}
}