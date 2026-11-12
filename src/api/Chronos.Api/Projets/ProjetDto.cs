namespace Chronos.Api.Projets;

public enum NatureProjet { Facturable, Interne }

public enum StatutProjet { EnPreparation, EnCours, Termine, Annule }

public record ProjetDto(
    int Id,
    string Code,
    string Libelle,
    NatureProjet Nature,
    DateOnly DateDebut,
    DateOnly? DateFin,
    int BudgetJours,
    StatutProjet Statut);

public record CreerProjetRequete(
    string Code,
    string Libelle,
    NatureProjet Nature,
    DateOnly DateDebut,
    DateOnly? DateFin,
    int BudgetJours);
