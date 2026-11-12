using Chronos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chronos.Infrastructure.Configurations;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.Property(c => c.Nom).HasMaxLength(200);
        builder.Property(c => c.CreePar).HasMaxLength(64);
        builder.Property(c => c.ModifiePar).HasMaxLength(64);
    }
}
