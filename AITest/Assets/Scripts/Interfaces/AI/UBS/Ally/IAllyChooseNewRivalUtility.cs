using Interfaces.AI.UBS.BaseInterfaces.Get;

namespace Interfaces.AI.UBS.Ally
{
    public interface IAllyChooseNewRivalUtility : IHasATarget, IIsSeeingARival, IGetMoralWeight, IIsFighting, 
        IGetThreatWeightOfTarget 
    {}
}