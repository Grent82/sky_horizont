using SkyHorizont.Domain.Services;

namespace SkyHorizont.Infrastructure.Persistence.Interfaces
{
    public interface IIntelDbContext : IBaseDbContext
    {
        IList<IntelReport> Reports { get; }
    }
}
