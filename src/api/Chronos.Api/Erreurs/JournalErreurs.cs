namespace Chronos.Api.Erreurs;

/// <summary>Les refus que l'API explique au client (422, 409) : une anomalie gérée, donc un avertissement. Série 2000.</summary>
internal static partial class JournalErreurs
{
    [LoggerMessage(EventId = 2001, Level = LogLevel.Warning,
        Message = "Règle de gestion {CodeRegle} non respectée")]
    public static partial void RegleMetierRefusee(ILogger logger, string codeRegle);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Warning,
        Message = "Conflit signalé au client (concurrence : {EstConcurrence})")]
    public static partial void ConflitSignale(ILogger logger, bool estConcurrence);
}
