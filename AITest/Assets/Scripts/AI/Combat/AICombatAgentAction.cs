namespace AI.Combat
{
    public class AICombatAgentAction<TAction>
    {
        private TAction _aiCombatAction;
        public float utilityScore = 0;

        public AICombatAgentAction(TAction aiCombatAction)
        {
            _aiCombatAction = aiCombatAction;
        }

        public TAction GetAIAction()
        {
            return _aiCombatAction;
        }
    }
}