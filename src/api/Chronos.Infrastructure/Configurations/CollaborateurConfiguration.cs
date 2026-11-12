using Chronos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chronos.Infrastructure.Configurations;

public class CollaborateurConfiguration : IEntityTypeConfiguration<Collaborateur>
{
    public const string FiltreNonArchives = "NonArchives";

    public void Configure(EntityTypeBuilder<Collaborateur> builder)
    {
        builder.HasQueryFilter(FiltreNonArchives, c => !c.EstArchive);

        builder.Property(c => c.Matricule).HasMaxLength(20);
        builder.HasIndex(c => c.Matricule).IsUnique();

        builder.Property(c => c.Nom).HasMaxLength(100);
        builder.Property(c => c.Prenom).HasMaxLength(100);

        builder.Property(c => c.Email).HasMaxLength(254);
        builder.HasIndex(c => c.Email).IsUnique();

        builder.Property(c => c.Poste).HasMaxLength(100);

        builder.HasOne(c => c.Manager)
            .WithMany()
            .HasForeignKey(c => c.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Contrats)
            .WithOne()
            .HasForeignKey(c => c.CollaborateurId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
