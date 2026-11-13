namespace Chronos.Infrastructure.Tracabilite;

/// <summary>L'utilisateur qui fait la requête en cours. Hors requête (données initiales), l'identifiant vaut « systeme ».</summary>
public interface IUtilisateurCourant
{
    string Identifiant { get; }
    int? CollaborateurId { get; }
    bool APourRole(string role);
}
