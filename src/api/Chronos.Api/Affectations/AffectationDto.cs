namespace Chronos.Api.Affectations;

public record AffectationDto(
    int Id,
    int CollaborateurId,
    int ProjetId,
    DateOnly DateDebut,
    DateOnly DateFin,
    decimal TauxAllocation);
