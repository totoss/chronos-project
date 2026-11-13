// TODO [module 06] Archiver sans supprimer (RG-05) : une constante publique qui nomme un filtre de requête, et ce filtre nommé qui écarte les collaborateurs archivés de toute requête sur
//    « Collaborateurs » (un filtre ne change pas le schéma : pas de migration). Il fait disparaître les projets dont le responsable est archivé : les requêtes sur les projets doivent donc
//    ignorer ce filtre, et lui seul, par son nom
//    — cours/06-persister-avec-ef-core.md, Pas à pas, étape 11 « Archiver sans supprimer (RG-05) »
// TODO [module 06] Compléter la configuration (classe qui implémente IEntityTypeConfiguration de Collaborateur) : longueurs, index UNIQUES sur le matricule et sur l'adresse électronique (RG-01),
//    catégorie enregistrée en chaîne, relation vers le manager (d'une table vers elle-même, sans suppression en cascade), relation vers les contrats. Migration ConfigurationCollaborateurs :
//    la lire, l'appliquer, puis constater ce que devient la colonne « Categorie » des lignes existantes
//    — cours/06-persister-avec-ef-core.md, À vous, exercice 1 « Configurer `Collaborateur` et `Contrat`. »
// TODO [module 07] Ajouter un index (non unique) sur le nom, puis créer la migration IndexNomCollaborateurs dans les DEUX jeux, les comparer et les appliquer sur les deux bases
//    — cours/07-deux-sgbd-un-seul-code.md, À vous, exercice 3 « Un changement, deux migrations. »
// TODO [module 08] Déclarer la propriété « Version » comme jeton de concurrence, puis créer la migration VersionCollaborateur dans chaque jeu
//    — cours/08-une-api-robuste.md, Pas à pas, étape 7 « Modifier sans écraser le travail d'un autre »
// TODO [module 12] Limiter à 64 caractères les deux colonnes d'auteur de la trace
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 9 « Tracer (RG-53) »

namespace Chronos.Infrastructure.Configurations;

public class CollaborateurConfiguration
{
}
