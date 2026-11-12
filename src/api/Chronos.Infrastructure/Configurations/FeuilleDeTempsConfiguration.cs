using Chronos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chronos.Infrastructure.Configurations;

public class FeuilleDeTempsConfiguration : IEntityTypeConfiguration<FeuilleDeTemps>
{
    public void Configure(EntityTypeBuilder<FeuilleDeTemps> builder)
    {
        builder.ToTable("FeuillesDeTemps");
        builder.HasIndex(f => new { f.CollaborateurId, f.Semaine }).IsUnique();
        builder.Property(f => f.MotifRejet).HasMaxLength(500);
        builder.Property(f => f.Version).IsConcurrencyToken();

        builder.HasOne<Collaborateur>()
            .WithMany()
            .HasForeignKey(f => f.CollaborateurId)
            .OnDelete(DeleteBehavior.Restrict);

        // La cascade est voulue : une saisie n'existe pas sans sa feuille.
        builder.HasMany(f => f.Saisies)
            .WithOne()
            .HasForeignKey(s => s.FeuilleDeTempsId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
