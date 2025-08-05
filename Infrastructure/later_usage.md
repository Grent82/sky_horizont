# Later Usage Examples

## Real DbContext 

```
namespace SkyHorizont.Infrastructure.Persistence
{
    public class RealFundsDbContext : DbContext, IFundsDbContext
    {
        public DbSet<FactionAccount> FactionFunds { get; set; }
        public void SaveChanges() => base.SaveChanges();
        protected override void OnModelCreating(ModelBuilder builder) { /* mapping code */ }
    }
}
```
