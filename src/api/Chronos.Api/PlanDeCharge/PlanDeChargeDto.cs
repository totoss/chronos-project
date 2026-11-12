namespace Chronos.Api.PlanDeCharge;

public record PlanDeChargeDto(DateOnly Du, DateOnly Au, string Pas, IReadOnlyList<LignePlanDeChargeDto> Lignes);

public record LignePlanDeChargeDto(int CollaborateurId, string Nom, string Prenom, IReadOnlyList<ChargeDto> Periodes);

public record ChargeDto(
    DateOnly Debut, DateOnly Fin, decimal Capacite, decimal ChargePlanifiee,
    decimal? TauxDeCharge, bool Surcharge, bool Intercontrat);
