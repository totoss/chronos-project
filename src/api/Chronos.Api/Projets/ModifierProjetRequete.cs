using System.ComponentModel.DataAnnotations;
using Chronos.Domain;

namespace Chronos.Api.Projets;

public record ModifierProjetRequete(
    [Required, StringLength(200)] string Libelle,
    [Required] NatureProjet? Nature,
    int? ClientId,
    [Required] DateOnly? DateDebut,
    DateOnly? DateFin,
    [Required, Range(0, int.MaxValue)] int? BudgetJours,
    [Required] int? ResponsableId,
    [Required] Guid? Version);
