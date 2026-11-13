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

        // Données de référence : l'application ne fonctionne pas sans elles, en production comme en développement.
        // Elles voyagent donc avec les migrations (script SQL et bundle compris), à la différence de UseSeeding,
        // qui ne s'exécute ni dans un script ni en production. Les identifiants sont explicites : HasData l'exige.
        builder.HasData(
            new { Id = 1, Code = "CP", Libelle = "Congés payés" },
            new { Id = 2, Code = "RTT", Libelle = "Réduction du temps de travail" },
            new { Id = 3, Code = "MAL", Libelle = "Maladie" });
    }
}
