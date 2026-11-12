using System.ComponentModel.DataAnnotations;

namespace Chronos.Api.Collaborateurs;

public record FiltreCollaborateurs(
    [property: Range(1, 1_000_000, ErrorMessage = "La page est un entier compris entre {1} et {2}.")] int Page = 1,
    [property: Range(1, 100, ErrorMessage = "La taille de page est un entier compris entre {1} et {2}.")] int Taille = 20,
    [property: StringLength(100, ErrorMessage = "La recherche ne dépasse pas {1} caractères.")] string? Recherche = null,
    string? Statut = null,
    string? Tri = null);
