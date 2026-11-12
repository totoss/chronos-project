using System.Data;
using Chronos.Domain;
using Chronos.Infrastructure.Identite;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Chronos.Infrastructure;

public class ChronosDbContext(DbContextOptions<ChronosDbContext> options)
    : IdentityDbContext<Utilisateur, IdentityRole<int>, int>(options)
{
    public DbSet<Collaborateur> Collaborateurs => Set<Collaborateur>();
    public DbSet<Contrat> Contrats => Set<Contrat>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Projet> Projets => Set<Projet>();
    public DbSet<Affectation> Affectations => Set<Affectation>();
    public DbSet<JourFerie> JoursFeries => Set<JourFerie>();

    // Les saisies de temps n'ont pas de DbSet : on n'y accède que par leur feuille (agrégat).
    public DbSet<FeuilleDeTemps> FeuillesDeTemps => Set<FeuilleDeTemps>();
    public DbSet<Absence> Absences => Set<Absence>();
    public DbSet<TypeAbsence> TypesAbsence => Set<TypeAbsence>();

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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // En premier : c'est lui qui déclare les tables d'Identity.
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ChronosDbContext).Assembly);

        builder.Entity<Utilisateur>(u =>
        {
            u.HasOne(x => x.Collaborateur).WithMany().HasForeignKey(x => x.CollaborateurId).OnDelete(DeleteBehavior.Restrict);
            u.HasIndex(x => x.CollaborateurId).IsUnique();
        });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(20);
    }
}
