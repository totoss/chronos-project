using System.ComponentModel.DataAnnotations;

namespace Chronos.Api.Collaborateurs;

public record ModifierCollaborateurRequete(
    [Required, StringLength(100)] string Nom,
    [Required, StringLength(100)] string Prenom,
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, StringLength(100)] string Poste,
    DateOnly? DateSortie,
    int? ManagerId,
    [Required] Guid? Version);
