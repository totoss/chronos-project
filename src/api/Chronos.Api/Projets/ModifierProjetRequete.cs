using System.ComponentModel.DataAnnotations;
using Chronos.Domain;

namespace Chronos.Api.Projets;

public record ModifierProjetRequete(
    [Required(ErrorMessage = "Le libellé est obligatoire."),
     StringLength(200, ErrorMessage = "Le libellé ne dépasse pas {1} caractères.")] string Libelle,
    [Required(ErrorMessage = "La nature est obligatoire.")] NatureProjet? Nature,
    int? ClientId,
    [Required(ErrorMessage = "La date de début est obligatoire.")] DateOnly? DateDebut,
    DateOnly? DateFin,
    [Required(ErrorMessage = "Le budget en jours est obligatoire."),
     Range(0, int.MaxValue, ErrorMessage = "Le budget en jours ne peut pas être négatif.")] int? BudgetJours,
    [Required(ErrorMessage = "Le responsable est obligatoire.")] int? ResponsableId,
    [Required(ErrorMessage = "La version est obligatoire.")] Guid? Version);
