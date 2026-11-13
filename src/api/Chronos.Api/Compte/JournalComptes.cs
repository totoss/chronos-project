namespace Chronos.Api.Compte;

/// <summary>Les messages de journal de la gestion des comptes : des identifiants, jamais d'adresse. Série 4000.</summary>
public static partial class JournalComptes
{
    [LoggerMessage(EventId = 4001, Level = LogLevel.Information,
        Message = "Compte {CompteId} créé par {UtilisateurId}")]
    public static partial void CompteCree(ILogger logger, int compteId, string utilisateurId);

    [LoggerMessage(EventId = 4002, Level = LogLevel.Information,
        Message = "Compte {CompteId} modifié par {UtilisateurId} (fiche {CollaborateurId}, verrouillé : {Verrouille})")]
    public static partial void CompteModifie(ILogger logger, int compteId, string utilisateurId, int? collaborateurId, bool verrouille);

    [LoggerMessage(EventId = 4003, Level = LogLevel.Warning,
        Message = "Modification du compte {CompteId} refusée à {UtilisateurId} : {Motif}")]
    public static partial void ModificationRefusee(ILogger logger, int compteId, string utilisateurId, string motif);
}
