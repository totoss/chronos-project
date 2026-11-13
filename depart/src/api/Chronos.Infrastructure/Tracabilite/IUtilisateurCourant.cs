// TODO [module 12] L'utilisateur qui fait la requête en cours, déclaré ici parce que l'intercepteur de traçabilité en a besoin : trois membres, un identifiant (texte ; « systeme » hors requête, pour les données
//    initiales), l'identifiant de collaborateur (entier facultatif) et un test d'appartenance à un rôle. Son implémentation, qui isole le seul accès à la requête HTTP, vit dans Chronos.Api
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 7 « Autoriser par périmètre (RG-50, RG-51) »

namespace Chronos.Infrastructure.Tracabilite;

public interface IUtilisateurCourant
{
}
