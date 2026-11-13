// TODO [module 12] Les comptes du développement : une méthode statique asynchrone qui reçoit le fournisseur de services et un mot de passe
//    (jamais écrit dans un fichier versionné : il vient des secrets utilisateur, clé « Chronos:Demo:MotDePasse »). Elle crée les rôles
//    manquants, puis un compte par rôle, relié à la fiche des données initiales par l'adresse électronique : Camille (Collaborateur),
//    Sophie (Collaborateur et Manager), Hugo (Collaborateur et GestionnaireRh) et un administrateur sans fiche. Idempotente :
//    elle ne recrée pas ce qui existe, et arrête le démarrage avec un message clair si une fiche attendue manque.
//    Appelée depuis Program.cs, en développement seulement. Classe publique statique.
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 11 « Des comptes pour le développement »

namespace Chronos.Api.Securite;

public static class ComptesDeDemonstration
{
}
