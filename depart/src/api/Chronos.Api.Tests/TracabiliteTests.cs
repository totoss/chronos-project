// TODO [module 12] Tester la traçabilité (RG-53) : un gestionnaire RH crée un collaborateur ; relu par le contexte, celui-ci porte l'heure de création égale
//    à celle de l'horloge pilotée et l'identifiant du compte comme auteur ; la trace ne recopie ni nom ni adresse ; une modification ajoute l'heure
//    et l'auteur sans toucher à la création ; la trace distingue les auteurs ; chaque ressource créée par l'API est tracée ; hors requête,
//    l'auteur est « systeme ».
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 9 « Tracer (RG-53) »
// TODO [module 12] Tester que l'approbation d'une absence (et la validation d'une feuille) est tracée au nom du valideur.
//    — cours/12-securite-et-tracabilite.md, À vous, exercice 3 « RG-36 jusqu'au bout. »

namespace Chronos.Api.Tests;

public class TracabiliteTests
{
}
