// TODO [module 25] Créer au démarrage le premier administrateur d'une installation neuve (sans lui, personne ne peut se connecter ni créer de
//    compte). Une méthode statique asynchrone qui reçoit le fournisseur de services et la configuration, appelée depuis Program.cs.
//    Règles : rien n'est créé tant que « Chronos:Admin:Email » et « Chronos:Admin:MotDePasse » ne sont pas tous deux configurés (un
//    seul des deux : avertissement, rien de créé) ; aucune valeur par défaut ; les quatre rôles sont créés s'ils manquent ; le compte
//    n'est créé que si aucun compte n'existe encore (un redémarrage ne recrée ni ne réinitialise rien) ; il reçoit le rôle
//    Administrateur, et un compte qui n'obtient pas son rôle est supprimé ; un refus d'Identity arrête le démarrage avec un message qui cite
//    les codes d'erreur (jamais l'adresse). Les messages de journal sont générés à la compilation (série 5000) et ne portent que des
//    identifiants. Classe publique, statique et partielle dans la solution finale.
//    — cours/25-exploiter-l-application.md, Pas à pas, étape 12 « Le premier administrateur » (manque exposé par l'encadré « Compléter le déploiement »)
// (Le test correspondant est Chronos.Api.Tests/AdministrateurInitialTests.cs.)

namespace Chronos.Api.Securite;

public static class AdministrateurInitial
{
}
