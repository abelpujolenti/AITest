namespace ECS.Components.AI.Combat
{
    public class MoralGroupComponent : GroupComponent
    {
        public bool isRequestingHelp;

        public float helpPriority;
        
        public MoralGroupComponent(float moralGroupWeight)
        {
            this.groupWeight = moralGroupWeight;
        }
    }
}