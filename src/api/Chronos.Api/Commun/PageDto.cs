namespace Chronos.Api.Commun;

public record PageDto<T>(IReadOnlyList<T> Elements, int Page, int Taille, int Total);
