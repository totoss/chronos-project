using Chronos.Domain;

namespace Chronos.Api.Collaborateurs;

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
    DateOnly DateEntree,
    DateOnly? DateSortie,
    decimal Quotite,
    Guid Version);
