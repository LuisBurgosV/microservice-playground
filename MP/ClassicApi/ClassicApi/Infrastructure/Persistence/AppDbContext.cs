using ClassicApi.Domain.Entities;
using ClassicApi.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace ClassicApi.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Client> Clients => Set<Client>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new ClientConfiguration());
        }
    }
}
