using System.ComponentModel.DataAnnotations;

namespace Chronos.Api.FeuillesDeTemps;

public record RejeterFeuilleRequete(
    [Required(ErrorMessage = "Le valideur est obligatoire.")] int? ValideurId,
    [Required(ErrorMessage = "Le motif du rejet est obligatoire."),
     StringLength(500, ErrorMessage = "Le motif ne dépasse pas {1} caractères.")] string Motif);
