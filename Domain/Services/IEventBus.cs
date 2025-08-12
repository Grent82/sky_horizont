namespace SkyHorizont.Domain.Services
{
    public interface IEventBus
    {
        void Publish<T>(T @event);
    }
}
