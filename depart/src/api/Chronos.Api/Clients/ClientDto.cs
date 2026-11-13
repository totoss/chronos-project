// Ce fichier réunit trois types : ClientDto (ce que l'API renvoie), CreerClientRequete et ModifierClientRequete (ce qu'elle reçoit).
//
// TODO [module 03] ClientDto : identifiant et nom du client ; CreerClientRequete : le nom.
//    — cours/03-premiere-api.md, À vous, exercice 4 « Une deuxième ressource. »
// TODO [module 06] ClientDto gagne le nombre de projets en cours du client (calculé par la base, sans charger les projets).
//    — cours/06-persister-avec-ef-core.md, À vous, exercice 6 « Compter sans charger. »
// TODO [module 08] Ajouter ModifierClientRequete (le nom) et annoter les deux requêtes : nom obligatoire, 200 caractères au plus.
//    — cours/08-une-api-robuste.md, À vous, exercice 2 « Les clients. »
// TODO [module 17] Donner aux annotations des deux requêtes un message en français.
//    — cours/17-formulaires.md, Pas à pas, étape 6 « Les refus de l'API » (encadré « Compléter le contrat de l'API. »)

namespace Chronos.Api.Clients;

public record ClientDto;

public record CreerClientRequete;

public record ModifierClientRequete;
