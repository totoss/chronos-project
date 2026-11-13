using System.ComponentModel.DataAnnotations;

namespace Chronos.Api.Affectations;

// Le collaborateur et le projet d'une affectation ne changent pas : on supprime et on recrée.
public record ModifierAffectationRequete(
    [Required(ErrorMessage = "La date de début est obligatoire.")] DateOnly? DateDebut,
    [Required(ErrorMessage = "La date de fin est obligatoire.")] DateOnly? DateFin,
    [Required(ErrorMessage = "Le taux d'allocation est obligatoire.")] decimal? TauxAllocation,
    bool SurchargeConfirmee = false);
