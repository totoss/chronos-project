using Chronos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chronos.Infrastructure.Configurations;

public class TypeAbsenceConfiguration : IEntityTypeConfiguration<TypeAbsence>
{
    public void Configure(EntityTypeBuilder<TypeAbsence> builder)
    {
        builder.Property(t => t.Code).HasMaxLength(10);
        builder.HasIndex(t => t.Code).IsUnique();
        builder.Property(t => t.Libelle).HasMaxLength(100);
    }
}
