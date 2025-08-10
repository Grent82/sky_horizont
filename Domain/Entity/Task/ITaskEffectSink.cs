namespace SkyHorizont.Domain.Entity.Task
{
    /// <summary>
    /// Route effects to the appropriate services/handlers.
    /// </summary>
    public interface ITaskEffectSink
    {
        void Publish(TaskEffect effect);
    }
}
