using System.ComponentModel.DataAnnotations;

namespace Chronos.Api.Clients;

public record ClientDto(int Id, string Nom, int ProjetsEnCours);

public record CreerClientRequete(
    [Required(ErrorMessage = "Le nom est obligatoire."),
     StringLength(200, ErrorMessage = "Le nom ne dépasse pas {1} caractères.")] string Nom);

public record ModifierClientRequete(
    [Required(ErrorMessage = "Le nom est obligatoire."),
     StringLength(200, ErrorMessage = "Le nom ne dépasse pas {1} caractères.")] string Nom);
