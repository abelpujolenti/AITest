using ECS.Components.AI.Combat;
using Interfaces.AI.Combat;

namespace ECS.Systems.AI.Combat
{
    public class MoralSystem
    {
        public void IncreaseMoralLevel(ref MoralComponent firstMoralComponent, ref MoralComponent secondMoralComponent)
        {
            firstMoralComponent.AddMinMoralWeight();
            secondMoralComponent.AddMinMoralWeight();
        }

        public void DecreaseMoralLevel(ref MoralComponent firstMoralComponent, ref MoralComponent secondMoralComponent)
        {
            firstMoralComponent.SubtractMinMoralWeight();
            secondMoralComponent.SubtractMinMoralWeight();
        }

        public bool EvaluateConfrontation(IStatWeight moralComponent, IStatWeight threatComponent)
        {
            return moralComponent.GetWeight() > threatComponent.GetWeight();
        }
    }
}