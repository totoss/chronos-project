using System.Data;
using Chronos.Domain;
using Microsoft.EntityFrameworkCore;

namespace Chronos.Infrastructure;

public class ChronosDbContext(DbContextOptions<ChronosDbContext> options) : DbContext(options)
{
    public DbSet<Collaborateur> Collaborateurs { get; set; }
    public DbSet<Contrat> Contrats { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Projet> Projets { get; set; }
    public DbSet<Affectation> Affectations { get; set; }
    public DbSet<JourFerie> JoursFeries { get; set; }

    // Les saisies de temps n'ont pas de DbSet : on n'y accède que par leur feuille (agrégat).
    public DbSet<FeuilleDeTemps> FeuillesDeTemps { get; set; }
    public DbSet<Absence> Absences { get; set; }
    public DbSet<TypeAbsence> TypesAbsence { get; set; }

    /// <summary>
    /// Exécute une lecture suivie d'une écriture dans une transaction sérialisable, confiée à la stratégie
    /// d'exécution : en cas d'échec passager (erreur de sérialisation, interblocage), toute la fonction est rejouée.
    /// Elle ne doit donc rien supposer d'un passage précédent.
    /// </summary>
    public Task<T> EnTransactionSerialisableAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        var strategie = Database.CreateExecutionStrategy();

        return strategie.ExecuteAsync(async () =>
        {
            ChangeTracker.Clear();
            await using var transaction =
                await Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            var resultat = await operation();

            await transaction.CommitAsync(cancellationToken);
            return resultat;
        });
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChronosDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(20);
    }
}
