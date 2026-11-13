// TODO [module 08] La requête de modification d'un projet (PUT) : libellé, nature, dates, budget en jours, responsable, et
//    la version lue par le client (jeton de concurrence optimiste, obligatoire). Le code d'un projet ne se modifie pas.
//    Mêmes contraintes que la création.
//    — cours/08-une-api-robuste.md, À vous, exercice 1 « Les projets. »
// TODO [module 17] Donner à chaque annotation un message en français.
//    — cours/17-formulaires.md, Pas à pas, étape 6 « Les refus de l'API » (encadré « Compléter le contrat de l'API. »)

namespace Chronos.Api.Projets;

public record ModifierProjetRequete;
