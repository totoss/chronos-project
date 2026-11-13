// TODO [module 12] L'implémentation de IUtilisateurCourant (interface du projet Infrastructure, que l'intercepteur de traçabilité utilise
//    aussi) à partir de la requête en cours : identifiant technique du compte (« systeme » hors requête), identifiant de la fiche
//    du collaborateur (lu dans le claim ClaimsChronos.CollaborateurId, absent s'il n'y en a pas) et appartenance à un rôle. C'est la
//    seule classe qui touche à IHttpContextAccessor : le reste du code dépend de l'interface. Classe publique et scellée, enregistrée
//    par requête dans Program.cs.
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 7 « Autoriser par périmètre (RG-50, RG-51) »

namespace Chronos.Api.Securite;

public sealed class UtilisateurCourant
{
}
