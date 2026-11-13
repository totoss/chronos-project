// TODO [module 04] Entité collaborateur. Propriétés : Id, Matricule, Nom, Prenom, Email, Categorie (CategorieCollaborateur), Poste, DateEntree, DateSortie (facultative),
//    ManagerId (facultatif), EstArchive ; accesseurs d'écriture privés. Le constructeur reçoit tout sauf la sortie, le manager et l'archivage, refuse par une
//    exception d'argument les textes vides et retire les espaces superflus. La date de sortie n'est pas connue à l'embauche : une méthode la pose (son nom, et comment un gestionnaire RH l'appellerait : c'est la question de l'exercice). RG-02 : la date de sortie n'est pas antérieure à la date d'entrée (limite comprise) ; violation = RegleMetierException avec le code de la règle ; tester
//    la limite. Prévoir aussi une méthode qui rattache à un manager (ou à personne) et une qui modifie la fiche (nom, prénom, adresse, poste, date de sortie,
//    manager) : le module 08 les appellera
//    — cours/04-modeliser-le-domaine.md, À vous, exercice 4 « `Collaborateur` et RG-02. »
// TODO [module 04] Historique des contrats : un champ privé « _contrats » (une List de Contrat) exposé en lecture seule sous le nom « Contrats » (IReadOnlyCollection), une
//    méthode « AjouterContrat » (type, quotité, date de début) et une méthode « QuotiteAu(jour) » (0 sans contrat à cette date). Les règles de clôture du contrat en cours et de refus d'un
//    contrat trop précoce ne sont pas dans l'exercice : choix de la solution de référence, à comparer au tag module-25. Tester un passage de 100 % à 80 % :
//    la quotité d'avant ne bouge pas
//    — cours/04-modeliser-le-domaine.md, À vous, exercice 5 « `Contrat` et RG-03. »
// TODO [module 06] Ajouter une propriété de navigation « Collaborateur? Manager » (accesseur d'écriture privé, comme Projet.Client)
//    — cours/06-persister-avec-ef-core.md, À vous, exercice 1 « Configurer `Collaborateur` et `Contrat`. »
// TODO [module 06] Ajouter la méthode « Archiver », avec son test : elle marque le collaborateur archivé (RG-05) sans toucher au reste de la fiche
//    — cours/06-persister-avec-ef-core.md, À vous, exercice 4 « Archiver. »
// TODO [module 07] Normaliser dans le domaine, au constructeur : le matricule en majuscules, l'adresse électronique en minuscules (RG-01, comparaison de casse différente
//    selon le SGBD). « Modifier » normalise l'adresse de la même façon. Tests d'abord
//    — cours/07-deux-sgbd-un-seul-code.md, À vous, exercice 1 « Décider de la casse (RG-01, RG-10). »
// TODO [module 08] Jeton de concurrence optimiste : une propriété « Version » (un Guid, nouvelle valeur à la création), renouvelée dans CHAQUE méthode qui modifie la fiche
//    (Modifier, DeclarerSortie, RattacherA, Archiver, AjouterContrat) ; un refus ne la renouvelle pas
//    — cours/08-une-api-robuste.md, Pas à pas, étape 7 « Modifier sans écraser le travail d'un autre »
// TODO [module 12] Implémenter ITracable (RG-53) : les quatre propriétés de trace, à accesseurs d'écriture privés
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 9 « Tracer (RG-53) »
// TODO [module 22] Non détaillé dans le module : le niveau d'analyse « all » signale aussi la mise en minuscules de l'adresse (règle CA1308). Décision à prendre avec la grille de l'étape 5 ;
//    la solution de référence est à comparer au tag module-25
//    — cours/22-qualite-du-code.md, Pas à pas, étape 5 « Décider du sort de chaque avertissement »

namespace Chronos.Domain;

public class Collaborateur
{
}
