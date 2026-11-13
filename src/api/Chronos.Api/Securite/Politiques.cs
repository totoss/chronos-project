namespace Chronos.Api.Securite;

/// <summary>Les politiques d'autorisation : le rôle ouvre la fonction, le périmètre dit sur qui (voir <see cref="PerimetreUtilisateur"/>).</summary>
public static class Politiques
{
    /// <summary>Fiches des collaborateurs et calendrier (RG-52, cahier des charges § 2).</summary>
    public const string GestionRh = "GestionRh";

    /// <summary>Valider ou rejeter une feuille de temps, approuver ou refuser une absence (RG-36).</summary>
    public const string Validation = "Validation";

    /// <summary>Piloter l'activité : clients, projets et affectations de son équipe.</summary>
    public const string Pilotage = "Pilotage";

    /// <summary>Gérer les comptes et les rôles.</summary>
    public const string Administration = "Administration";
}
