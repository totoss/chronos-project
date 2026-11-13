using Chronos.Domain;

namespace Chronos.Api.FeuillesDeTemps;

public record SaisieDto(int ProjetId, DateOnly Jour, decimal Duree);

public record FeuilleDeTempsDto(
    int Id,
    int CollaborateurId,
    DateOnly Semaine,
    StatutFeuilleDeTemps Statut,
    string? MotifRejet,
    IReadOnlyList<SaisieDto> Saisies)
{
    public static FeuilleDeTempsDto Depuis(FeuilleDeTemps feuille) => new(
        feuille.Id, feuille.CollaborateurId, feuille.Semaine, feuille.Statut, feuille.MotifRejet,
        [.. feuille.Saisies
            .OrderBy(s => s.Jour).ThenBy(s => s.ProjetId)
            .Select(s => new SaisieDto(s.ProjetId, s.Jour, s.Duree))]);
}
