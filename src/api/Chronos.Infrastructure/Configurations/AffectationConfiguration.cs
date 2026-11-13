using Chronos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chronos.Infrastructure.Configurations;

public class AffectationConfiguration : IEntityTypeConfiguration<Affectation>
{
    public void Configure(EntityTypeBuilder<Affectation> builder)
    {
        builder.Property(a => a.TauxAllocation).HasPrecision(3, 2);
        builder.Property(a => a.CreePar).HasMaxLength(64);
        builder.Property(a => a.ModifiePar).HasMaxLength(64);

        builder.HasOne<Collaborateur>()
            .WithMany()
            .HasForeignKey(a => a.CollaborateurId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Projet)
            .WithMany()
            .HasForeignKey(a => a.ProjetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.CollaborateurId, a.DateDebut });
    }
}
