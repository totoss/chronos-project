using System.ComponentModel.DataAnnotations;

namespace Chronos.Api.Affectations;

// Le taux n'a pas de [Range] : sa borne est RG-21, qui répond 422 et non 400.
public record CreerAffectationRequete(
    [Required(ErrorMessage = "Le collaborateur est obligatoire.")] int? CollaborateurId,
    [Required(ErrorMessage = "Le projet est obligatoire.")] int? ProjetId,
    [Required(ErrorMessage = "La date de début est obligatoire.")] DateOnly? DateDebut,
    [Required(ErrorMessage = "La date de fin est obligatoire.")] DateOnly? DateFin,
    [Required(ErrorMessage = "Le taux d'allocation est obligatoire.")] decimal? TauxAllocation,
    bool SurchargeConfirmee = false);
