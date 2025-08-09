using SkyHorizont.Domain.Intrigue;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence
{
    public class SecretsRepository : ISecretsRepository
    {
        private readonly ISecretsDbContext _context;

        public SecretsRepository(ISecretsDbContext context)
        {
            _context = context;
        }

        public Secret Add(Secret secret)
        {
            _context.Secrets[secret.Id] = secret;
            _context.SaveChanges();
            return secret;
        }

        public Secret? GetById(Guid id)
        {
            _context.Secrets.TryGetValue(id, out var secret);
            return secret;
        }
    }
}
