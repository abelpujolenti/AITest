namespace Interfaces.AI.UBS.BaseInterfaces
{
    public interface IGetBestAction<TAction, TContext>
    {
        public TAction GetBestAction(TContext context);
    }
}