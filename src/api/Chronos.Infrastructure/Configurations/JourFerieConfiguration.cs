using Chronos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chronos.Infrastructure.Configurations;

public class JourFerieConfiguration : IEntityTypeConfiguration<JourFerie>
{
    public void Configure(EntityTypeBuilder<JourFerie> builder)
    {
        builder.HasKey(j => j.Date);
        builder.Property(j => j.Date).ValueGeneratedNever();
        builder.Property(j => j.Libelle).HasMaxLength(100);
    }
}
