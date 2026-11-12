using Chronos.Domain;

namespace Chronos.Api.Absences;

public record AbsenceDto(
    int Id,
    int CollaborateurId,
    int TypeAbsenceId,
    DateOnly DateDebut,
    DateOnly DateFin,
    bool DebuteApresMidi,
    bool FinitLeMatin,
    StatutAbsence Statut)
{
    public static AbsenceDto Depuis(Absence absence) => new(
        absence.Id, absence.CollaborateurId, absence.TypeAbsenceId, absence.DateDebut, absence.DateFin,
        absence.DebuteApresMidi, absence.FinitLeMatin, absence.Statut);
}
