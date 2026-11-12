using System.ComponentModel.DataAnnotations;

namespace Chronos.Api.Collaborateurs;

public record FiltreCollaborateurs(
    [property: Range(1, 1_000_000)] int Page = 1,
    [property: Range(1, 100)] int Taille = 20,
    [property: StringLength(100)] string? Recherche = null,
    string? Statut = null,
    string? Tri = null);
