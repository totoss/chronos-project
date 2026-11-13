using Chronos.Domain;

namespace Chronos.Api.Collaborateurs;

// La version complète, réservée au gestionnaire RH et à l'intéressé : les champs en plus ne quittent pas le serveur
// pour les autres, ils ne sont pas masqués par l'interface.
public record CollaborateurRhDto(
    int Id,
    string Matricule,
    string Nom,
    string Prenom,
    string Email,
    CategorieCollaborateur Categorie,
    string Poste,
    int? ManagerId,
    string? ManagerNom,
    DateOnly DateEntree,
    DateOnly? DateSortie,
    decimal Quotite,
    bool EstSorti,
    Guid Version);
