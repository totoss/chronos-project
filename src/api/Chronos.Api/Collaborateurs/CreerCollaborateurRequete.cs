using System.ComponentModel.DataAnnotations;
using Chronos.Domain;

namespace Chronos.Api.Collaborateurs;

public record CreerCollaborateurRequete(
    [Required(ErrorMessage = "Le matricule est obligatoire."),
     StringLength(20, ErrorMessage = "Le matricule ne dépasse pas {1} caractères.")] string Matricule,
    [Required(ErrorMessage = "Le nom est obligatoire."),
     StringLength(100, ErrorMessage = "Le nom ne dépasse pas {1} caractères.")] string Nom,
    [Required(ErrorMessage = "Le prénom est obligatoire."),
     StringLength(100, ErrorMessage = "Le prénom ne dépasse pas {1} caractères.")] string Prenom,
    [Required(ErrorMessage = "L'adresse électronique est obligatoire."),
     EmailAddress(ErrorMessage = "L'adresse électronique n'est pas valide."),
     StringLength(254, ErrorMessage = "L'adresse électronique ne dépasse pas {1} caractères.")] string Email,
    [Required(ErrorMessage = "La catégorie est obligatoire.")] CategorieCollaborateur? Categorie,
    [Required(ErrorMessage = "Le poste est obligatoire."),
     StringLength(100, ErrorMessage = "Le poste ne dépasse pas {1} caractères.")] string Poste,
    [Required(ErrorMessage = "La date d'entrée est obligatoire.")] DateOnly? DateEntree,
    [Required(ErrorMessage = "Le type de contrat est obligatoire.")] TypeContrat? TypeContrat,
    [Required(ErrorMessage = "La quotité est obligatoire.")] decimal? Quotite,
    int? ManagerId);
