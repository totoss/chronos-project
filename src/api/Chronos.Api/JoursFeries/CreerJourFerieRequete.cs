using System.ComponentModel.DataAnnotations;

namespace Chronos.Api.JoursFeries;

public record CreerJourFerieRequete(
    [Required(ErrorMessage = "La date est obligatoire.")] DateOnly? Date,
    [Required(ErrorMessage = "Le libellé est obligatoire."),
     StringLength(100, ErrorMessage = "Le libellé ne dépasse pas {1} caractères.")] string Libelle);
