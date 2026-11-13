// Fichier d'assemblage : chaque module qui touche au modèle vient ici. La déclaration ci-dessous n'a ni classe de base ni constructeur principal : ils exigent le paquet EF Core.

// TODO [module 06] Le contexte : il hérite de DbContext (paquet du fournisseur ajouté à l'étape 1) et reçoit en constructeur principal ses options (DbContextOptions du contexte), car c'est
//    l'application qui fournit le SGBD et la chaîne de connexion. Un DbSet par entité : Collaborateurs, Contrats, Clients, Projets. OnModelCreating applique toutes les classes
//    IEntityTypeConfiguration de l'assembly, pour ne pas les déclarer une à une
//    — cours/06-persister-avec-ef-core.md, Pas à pas, étape 2 « Écrire le contexte »
// TODO [module 06] Une règle pour toutes les énumérations : redéfinir ConfigureConventions pour enregistrer chaque énumération sous son nom (chaîne) avec une longueur maximale de 20, plutôt que de
//    répéter la conversion propriété par propriété ; prouver par la commande « has-pending-model-changes » que le modèle n'a pas changé
//    — cours/06-persister-avec-ef-core.md, À vous, exercice 2 « Une règle pour toutes les énumérations. »
// TODO [module 06] Pour chaque changement du modèle : une migration (« dotnet ef migrations add », projet cible Chronos.Infrastructure puis, dès le module 07, les deux projets de migrations)
//    — cours/06-persister-avec-ef-core.md, Pas à pas, étape 6 « Créer la première migration »
// TODO [module 09] Ajouter les DbSet Affectations et JoursFeries, puis créer la migration Affectations dans chacun des deux jeux (routine du module 07)
//    — cours/09-affectations-et-calcul-de-charge.md, Pas à pas, étape 1 « `Affectation` et `JourFerie` »
// TODO [module 10] Ajouter UN SEUL DbSet pour l'agrégat : FeuillesDeTemps (pas de DbSet de SaisieDeTemps : on n'y accède que par la feuille, ce qu'un commentaire dit) ; migration FeuillesDeTemps
//    dans chaque jeu
//    — cours/10-temps-et-absences.md, Pas à pas, étape 3 « Enregistrer l'agrégat »
// TODO [module 10] Ajouter les DbSet Absences et TypesAbsence ; migration Absences dans chaque jeu
//    — cours/10-temps-et-absences.md, Pas à pas, étape 6 « L'absence »
// TODO [module 10] Ajouter la méthode « EnTransactionSerialisableAsync » : elle exécute une lecture suivie d'une écriture dans une transaction de niveau « sérialisable », confiée à la stratégie
//    d'exécution (en cas d'échec passager, toute la fonction est rejouée : oublier les entités suivies avant chaque tentative, et ne rien supposer d'un passage précédent). Elle sert à
//    RG-41 (demande d'absence) et RG-43 (approbation)
//    — cours/10-temps-et-absences.md, Pas à pas, étape 8 « RG-43 : approuver une absence »
// TODO [module 12] Le contexte hérite d'IdentityDbContext (utilisateur du projet, rôle à clé entière, clé entière) au lieu de DbContext ; OnModelCreating appelle la base EN PREMIER (elle déclare les
//    tables d'Identity) puis déclare la relation du compte vers son collaborateur (suppression restreinte, index unique sur l'identifiant du collaborateur) ; migration AjoutIdentiteEtTracabilite
//    dans chaque jeu (avec les colonnes de traçabilité de l'étape 9)
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 1 « Le compte utilisateur et ses tables »
// TODO [module 22] Réécrire chaque DbSet en lecture seule, sur le modèle recommandé par la documentation d'EF Core, plutôt que d'accepter la version « nullable » que dotnet format propose ;
//    le modèle ne change pas. Garder le nom « builder » pour le paramètre d'OnModelCreating (règle CA1725)
//    — cours/22-qualite-du-code.md, Pas à pas, étape 4 « Élever le niveau d'analyse »

namespace Chronos.Infrastructure;

public class ChronosDbContext
{
}
