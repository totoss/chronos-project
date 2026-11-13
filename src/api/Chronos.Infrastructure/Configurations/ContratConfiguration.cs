using Chronos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chronos.Infrastructure.Configurations;

public class ContratConfiguration : IEntityTypeConfiguration<Contrat>
{
    public void Configure(EntityTypeBuilder<Contrat> builder)
    {
        builder.Property(c => c.Quotite).HasPrecision(3, 2);
    }
}
