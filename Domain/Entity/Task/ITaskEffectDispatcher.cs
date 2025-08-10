namespace SkyHorizont.Domain.Entity.Task
{
    /// <summary>
    /// Application-layer hook that receives domain task effects and routes them
    /// to appropriate systems (tech tree, intel, economy, etc.).
    /// </summary>
    public interface ITaskEffectDispatcher
    {
        void Dispatch(TaskEffect effect);
    }
}
