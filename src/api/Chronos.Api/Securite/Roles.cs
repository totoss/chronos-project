namespace Chronos.Api.Securite;

/// <summary>Les quatre rôles du cahier des charges (§ 2). Un nom de rôle ne s'écrit jamais deux fois à la main.</summary>
public static class Roles
{
    public const string Collaborateur = "Collaborateur";
    public const string Manager = "Manager";
    public const string GestionnaireRh = "GestionnaireRh";
    public const string Administrateur = "Administrateur";

    public static readonly string[] Tous = [Collaborateur, Manager, GestionnaireRh, Administrateur];
}
