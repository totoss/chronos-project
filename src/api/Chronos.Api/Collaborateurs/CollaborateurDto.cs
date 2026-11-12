using Chronos.Domain;

namespace Chronos.Api.Collaborateurs;

// La version réduite : tout ce qu'un manager ou un collègue a besoin de savoir, sans l'information contractuelle (RG-52).
// EstSorti remplace les dates, qui n'y figurent pas : vrai si la date de sortie est passée.
// Version est le jeton de concurrence du module 08 : l'interface en a besoin pour toute modification.
public record CollaborateurDto(
    int Id,
    string Matricule,
    string Nom,
    string Prenom,
    string Email,
    CategorieCollaborateur Categorie,
    string Poste,
    int? ManagerId,
    string? ManagerNom,
    bool EstSorti,
    Guid Version);
