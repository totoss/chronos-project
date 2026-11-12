using Chronos.Domain;

namespace Chronos.Api.Projets;

public record ProjetDto(
    int Id,
    string Code,
    string Libelle,
    NatureProjet Nature,
    int? ClientId,
    string? ClientNom,
    DateOnly DateDebut,
    DateOnly? DateFin,
    int BudgetJours,
    int ResponsableId,
    string ResponsableNom,
    StatutProjet Statut,
    Guid Version);
