using AI.Combat.ScriptableObjects;

namespace Interfaces.AI.UBS.BaseInterfaces.Get
{
    public interface IGetContext<TContext> where TContext : AICombatAgentContext
    {
        public TContext GetContext();
    }
}