// TODO [module 06] Configuration de Projet (la classe implémente IEntityTypeConfiguration de Projet ; interface non déclarée ici, elle exige le paquet EF Core) : code limité à 20 caractères avec un
//    index UNIQUE (RG-10 : c'est la base qui refusera un doublon, même sur deux requêtes simultanées), libellé limité à 200 ; relation vers le client et relation vers le responsable
//    (navigations « Client » et « Responsable », clés étrangères existantes, plusieurs projets par client ou responsable, sans collection inverse), toutes deux en suppression RESTREINTE (la
//    relation du responsable est obligatoire et la cascade par convention supprimerait ses projets). Nature et statut sont enregistrés par leur nom : à la main d'abord, puis par la
//    convention du contexte (exercice 2)
//    — cours/06-persister-avec-ef-core.md, Pas à pas, étape 3 « Configurer `Projet` »
// TODO [module 08] Déclarer la propriété « Version » comme jeton de concurrence, puis créer la migration VersionProjet dans chaque jeu (routine du module 07)
//    — cours/08-une-api-robuste.md, À vous, exercice 1 « Les projets. »
// TODO [module 12] Limiter à 64 caractères les deux colonnes d'auteur de la trace (création et modification) ; elles sont créées avec la migration AjoutIdentiteEtTracabilite
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 9 « Tracer (RG-53) »

namespace Chronos.Infrastructure.Configurations;

public class ProjetConfiguration
{
}
