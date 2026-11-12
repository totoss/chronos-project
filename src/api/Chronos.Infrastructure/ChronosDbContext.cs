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
        // En premier : c'est lui qui déclare les tables d'Identity.
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChronosDbContext).Assembly);

        modelBuilder.Entity<Utilisateur>(u =>
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
