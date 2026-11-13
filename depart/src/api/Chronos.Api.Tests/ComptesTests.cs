// TODO [module 25] Tester le contrat de la gestion des comptes : la liste donne pour chaque compte l'adresse, les rôles, la fiche et le verrouillage, sans aucun
//    secret ; seul l'administrateur liste et modifie (403 sinon) ; une modification sans jeton anti-CSRF est refusée ; un gestionnaire RH sans fiche
//    en obtient une par l'administrateur et peut alors saisir ; les rôles sont remplacés et la fiche peut être détachée ; une fiche
//    n'appartient qu'à un seul compte (409) ; les demandes invalides sont refusées avec les clés habituelles ; un compte verrouillé ne se
//    connecte plus (401) et se déverrouille ; un administrateur ne se verrouille pas et ne se retire pas son rôle (409) ; la session ouverte
//    d'un compte verrouillé, ou ouverte avant un changement de rôles, est refusée après l'intervalle de validation du tampon de sécurité.
//    — cours/25-exploiter-l-application.md, Pas à pas, étape 13 « Gérer les comptes »

namespace Chronos.Api.Tests;

public class ComptesTests
{
}
