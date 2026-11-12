using System.ComponentModel.DataAnnotations;

namespace Chronos.Api.Absences;

public record CreerAbsenceRequete(
    [Required(ErrorMessage = "Le collaborateur est obligatoire.")] int? CollaborateurId,
    [Required(ErrorMessage = "Le type d'absence est obligatoire.")] int? TypeAbsenceId,
    [Required(ErrorMessage = "La date de début est obligatoire.")] DateOnly? DateDebut,
    [Required(ErrorMessage = "La date de fin est obligatoire.")] DateOnly? DateFin,
    bool DebuteApresMidi = false,
    bool FinitLeMatin = false);
