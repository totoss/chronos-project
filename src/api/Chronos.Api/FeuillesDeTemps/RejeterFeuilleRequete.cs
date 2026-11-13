using System.ComponentModel.DataAnnotations;

namespace Chronos.Api.FeuillesDeTemps;

// Le valideur est l'utilisateur connecté : la requête ne porte plus que le motif.
public record RejeterFeuilleRequete(
    [Required(ErrorMessage = "Le motif du rejet est obligatoire."),
     StringLength(500, ErrorMessage = "Le motif ne dépasse pas {1} caractères.")] string Motif);
