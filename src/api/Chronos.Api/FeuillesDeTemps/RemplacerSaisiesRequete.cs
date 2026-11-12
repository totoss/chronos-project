using System.ComponentModel.DataAnnotations;

namespace Chronos.Api.FeuillesDeTemps;

// La durée n'a pas de [Range] : ses valeurs permises sont celles de RG-30, qui répond 422 et non 400.
public record SaisieRequete(
    [Required(ErrorMessage = "Le projet est obligatoire.")] int? ProjetId,
    [Required(ErrorMessage = "Le jour est obligatoire.")] DateOnly? Jour,
    [Required(ErrorMessage = "La durée est obligatoire.")] decimal? Duree);

public record RemplacerSaisiesRequete(
    [Required(ErrorMessage = "La liste des saisies est obligatoire.")] IReadOnlyList<SaisieRequete> Saisies);
