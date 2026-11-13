// TODO [module 04] Tester RG-02, limite comprise : une sortie le jour de l'entrée ou plus tard est acceptée, la veille est refusée (à la déclaration de sortie comme dans « Modifier »). Tester la
//    construction (fiche sans sortie, manager ni contrat), le refus d'un matricule vide, le rattachement à un manager ou à personne
//    — cours/04-modeliser-le-domaine.md, À vous, exercice 4 « `Collaborateur` et RG-02. »
// TODO [module 04] Tester l'historique des contrats : un contrat ajouté est visible, une quotité hors des bornes est refusée et n'est pas ajoutée, un passage de 100 % à 80 % ne change pas la quotité
//    d'avant, la quotité sans contrat à la date demandée est nulle, un nouveau contrat ne commence pas avant le contrat en cours
//    — cours/04-modeliser-le-domaine.md, À vous, exercice 5 « `Contrat` et RG-03. »
// TODO [module 06] Tester Archiver : le collaborateur est marqué archivé sans toucher à sa fiche, et deux archivages de suite le laissent archivé
//    — cours/06-persister-avec-ef-core.md, À vous, exercice 4 « Archiver. »
// TODO [module 07] Écrire d'abord les tests de normalisation : matricule enregistré en majuscules, adresse électronique en minuscules, à la construction et dans « Modifier »
//    — cours/07-deux-sgbd-un-seul-code.md, À vous, exercice 1 « Décider de la casse (RG-01, RG-10). »
// TODO [module 08] Tester le jeton de concurrence : chaque méthode qui modifie la fiche (Modifier, DeclarerSortie, RattacherA, Archiver, AjouterContrat) renouvelle la version, et une modification
//    refusée ne la renouvelle pas
//    — cours/08-une-api-robuste.md, Pas à pas, étape 7 « Modifier sans écraser le travail d'un autre »

namespace Chronos.Domain.Tests;

public class CollaborateurTests
{
}
