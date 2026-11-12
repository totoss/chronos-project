using Chronos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chronos.Infrastructure.Configurations;

public class ProjetConfiguration : IEntityTypeConfiguration<Projet>
{
    public void Configure(EntityTypeBuilder<Projet> builder)
    {
        builder.Property(p => p.Code).HasMaxLength(20);
        builder.HasIndex(p => p.Code).IsUnique();

        builder.Property(p => p.Libelle).HasMaxLength(200);

        builder.Property(p => p.Version).IsConcurrencyToken();

        builder.HasOne(p => p.Client)
            .WithMany()
            .HasForeignKey(p => p.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Responsable)
            .WithMany()
            .HasForeignKey(p => p.ResponsableId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
