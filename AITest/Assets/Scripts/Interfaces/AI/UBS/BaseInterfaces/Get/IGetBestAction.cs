namespace Interfaces.AI.UBS.BaseInterfaces.Get
{
    public interface IGetBestAction<TAction, TContext>
    {
        public TAction GetBestAction(TContext context);
    }
}