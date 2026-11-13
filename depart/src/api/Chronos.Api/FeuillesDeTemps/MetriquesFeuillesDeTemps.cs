// TODO [module 25] Une métrique métier : compter les feuilles de temps validées et rejetées. Le service reçoit la fabrique de mesures
//    (IMeterFactory) pour créer son compteur ; le nom du Meter est une constante publique, déclarée aussi dans la télémétrie de
//    Program.cs. Deux méthodes publiques, validée et rejetée, incrémentent le même compteur avec une seule étiquette « decision » à deux
//    valeurs (choix d'étiquette à justifier : voir la question de l'exercice).
//    Classe publique et scellée, enregistrée en singleton.
//    — cours/25-exploiter-l-application.md, À vous, exercice 3 « Une métrique métier. »

namespace Chronos.Api.FeuillesDeTemps;

public sealed class MetriquesFeuillesDeTemps
{
}
