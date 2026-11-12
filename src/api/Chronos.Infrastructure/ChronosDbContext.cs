using Chronos.Domain;
using Microsoft.EntityFrameworkCore;

namespace Chronos.Infrastructure;

public class ChronosDbContext(DbContextOptions<ChronosDbContext> options) : DbContext(options)
{
    public DbSet<Collaborateur> Collaborateurs { get; set; }
    public DbSet<Contrat> Contrats { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Projet> Projets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChronosDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(20);
    }
}
