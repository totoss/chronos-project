using System.Globalization;

namespace Chronos.Api.Commun;

public static class DatesIso
{
    /// <summary>Lit une date au seul format ISO « aaaa-mm-jj » : « 04/05/2026 » n'est pas lue comme le 5 avril.</summary>
    public static bool TryLire(string? texte, out DateOnly date) =>
        DateOnly.TryParseExact(texte, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
}
