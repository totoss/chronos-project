using Chronos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chronos.Infrastructure.Configurations;

public class SaisieDeTempsConfiguration : IEntityTypeConfiguration<SaisieDeTemps>
{
    public void Configure(EntityTypeBuilder<SaisieDeTemps> builder)
    {
        builder.ToTable("SaisiesDeTemps");
        builder.Property(s => s.Duree).HasPrecision(3, 2);
        builder.HasIndex(s => new { s.FeuilleDeTempsId, s.ProjetId, s.Jour }).IsUnique();

        builder.HasOne<Projet>()
            .WithMany()
            .HasForeignKey(s => s.ProjetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
