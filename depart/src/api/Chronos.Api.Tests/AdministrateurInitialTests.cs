// TODO [module 25] Tester le premier administrateur : réglage absent ou incomplet, rien n'est créé ; création des rôles et de l'administrateur sans fiche, qui se connecte ;
//    les rôles existent quand l'administrateur crée ensuite les comptes des autres ; un redémarrage ne crée rien de plus et ne
//    réinitialise pas le mot de passe ; quand un compte existe, les rôles manquants sont recréés mais aucun administrateur n'est ajouté ; un mot
//    de passe refusé par Identity arrête le démarrage avec un message clair.
//    — cours/25-exploiter-l-application.md, Pas à pas, étape 12 « Le premier administrateur »

namespace Chronos.Api.Tests;

public class AdministrateurInitialTests
{
}
