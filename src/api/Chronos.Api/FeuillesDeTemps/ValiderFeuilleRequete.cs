using System.ComponentModel.DataAnnotations;

namespace Chronos.Api.FeuillesDeTemps;

// Avant le module 12, la requête porte l'identifiant du valideur : l'API ne sait pas encore qui l'appelle.
public record ValiderFeuilleRequete(
    [Required(ErrorMessage = "Le valideur est obligatoire.")] int? ValideurId);
