using System.ComponentModel.DataAnnotations;

namespace Chronos.Api.Absences;

// Le demandeur est l'utilisateur connecté : la requête ne porte plus d'identifiant de collaborateur.
public record CreerAbsenceRequete(
    [Required(ErrorMessage = "Le type d'absence est obligatoire.")] int? TypeAbsenceId,
    [Required(ErrorMessage = "La date de début est obligatoire.")] DateOnly? DateDebut,
    [Required(ErrorMessage = "La date de fin est obligatoire.")] DateOnly? DateFin,
    bool DebuteApresMidi = false,
    bool FinitLeMatin = false);
