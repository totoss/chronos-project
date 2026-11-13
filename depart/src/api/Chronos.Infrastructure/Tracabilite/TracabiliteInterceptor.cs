// TODO [module 12] RG-53 : intercepteur d'enregistrement (classe scellée, construite avec TimeProvider et IUtilisateurCourant ; elle hérite de SaveChangesInterceptor, non déclaré ici : paquet EF Core).
//    Il redéfinit LES DEUX méthodes, synchrone et asynchrone, pour que la trace ne dépende pas de la façon dont l'enregistrement est appelé. Pour toute entité ITracable ajoutée, il renseigne la date et
//    l'auteur de création ; pour toute entité modifiée, la date et l'auteur de modification. L'heure vient de TimeProvider (UTC), l'auteur est l'IDENTIFIANT du compte, pas un nom ni une adresse. Le
//    brancher dans l'enregistrement du contexte (services de la requête, après le choix du fournisseur) ; limiter les colonnes d'auteur à 64 caractères dans les configurations ; créer la migration
//    AjoutIdentiteEtTracabilite dans chaque jeu. Tester : un collaborateur créé par un gestionnaire RH porte la date de l'horloge et l'identifiant du compte
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 9 « Tracer (RG-53) »

namespace Chronos.Infrastructure.Tracabilite;

public sealed class TracabiliteInterceptor
{
}
