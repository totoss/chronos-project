namespace Chronos.Api.FeuillesDeTemps;

/// <summary>
/// Les messages de journal des feuilles de temps, réunis pour être relus d'un coup : aucun ne porte de donnée
/// personnelle, seulement des identifiants. Les EventId de cette ressource sont dans la série 3000.
/// </summary>
public static partial class JournalFeuillesDeTemps
{
    [LoggerMessage(EventId = 3001, Level = LogLevel.Information,
        Message = "Feuille {FeuilleId} validée par {UtilisateurId}")]
    public static partial void FeuilleValidee(ILogger logger, int feuilleId, string utilisateurId);

    [LoggerMessage(EventId = 3002, Level = LogLevel.Warning,
        Message = "Validation refusée pour la feuille {FeuilleId} : règle {CodeRegle}")]
    public static partial void ValidationRefusee(ILogger logger, int feuilleId, string codeRegle);

    [LoggerMessage(EventId = 3003, Level = LogLevel.Information,
        Message = "Feuille {FeuilleId} rejetée par {UtilisateurId}")]
    public static partial void FeuilleRejetee(ILogger logger, int feuilleId, string utilisateurId);
}
