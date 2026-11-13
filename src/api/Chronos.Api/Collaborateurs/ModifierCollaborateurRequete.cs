using System.ComponentModel.DataAnnotations;

namespace Chronos.Api.Collaborateurs;

public record ModifierCollaborateurRequete(
    [Required(ErrorMessage = "Le nom est obligatoire."),
     StringLength(100, ErrorMessage = "Le nom ne dépasse pas {1} caractères.")] string Nom,
    [Required(ErrorMessage = "Le prénom est obligatoire."),
     StringLength(100, ErrorMessage = "Le prénom ne dépasse pas {1} caractères.")] string Prenom,
    [Required(ErrorMessage = "L'adresse électronique est obligatoire."),
     EmailAddress(ErrorMessage = "L'adresse électronique n'est pas valide."),
     StringLength(254, ErrorMessage = "L'adresse électronique ne dépasse pas {1} caractères.")] string Email,
    [Required(ErrorMessage = "Le poste est obligatoire."),
     StringLength(100, ErrorMessage = "Le poste ne dépasse pas {1} caractères.")] string Poste,
    DateOnly? DateSortie,
    int? ManagerId,
    [Required(ErrorMessage = "La version est obligatoire.")] Guid? Version);
