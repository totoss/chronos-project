// TODO [module 12] Interface de traçabilité (RG-53) : quatre propriétés en lecture seule, la date de création, l'auteur de la création, la date de dernière
//    modification (absente à la création) et l'auteur de la dernière modification (absent à la création). Les dates sont des DateTimeOffset. Ces valeurs sont
//    renseignées par l'intercepteur de l'infrastructure, jamais à la main. À faire implémenter, avec des accesseurs d'écriture privés, par Collaborateur,
//    FeuilleDeTemps, Absence, Affectation, Projet, Client et JourFerie (pas par Contrat ni SaisieDeTemps)
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 9 « Tracer (RG-53) »

namespace Chronos.Domain;

public interface ITracable
{
}
