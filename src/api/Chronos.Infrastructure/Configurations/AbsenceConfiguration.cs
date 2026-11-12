using Chronos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chronos.Infrastructure.Configurations;

public class AbsenceConfiguration : IEntityTypeConfiguration<Absence>
{
    public void Configure(EntityTypeBuilder<Absence> builder)
    {
        builder.Property(a => a.Version).IsConcurrencyToken();
        builder.HasIndex(a => new { a.CollaborateurId, a.DateDebut });

        builder.HasOne<Collaborateur>()
            .WithMany()
            .HasForeignKey(a => a.CollaborateurId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TypeAbsence>()
            .WithMany()
            .HasForeignKey(a => a.TypeAbsenceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
