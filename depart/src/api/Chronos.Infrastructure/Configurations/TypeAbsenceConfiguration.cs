// TODO [module 10] Configuration de TypeAbsence (implémente IEntityTypeConfiguration de TypeAbsence) : code limité à 10 caractères avec un index UNIQUE, libellé limité à 100
//    — cours/10-temps-et-absences.md, Pas à pas, étape 6 « L'absence »
// TODO [module 25] Données de référence : déclarer les trois types (identifiant 1 « CP » Congés payés, 2 « RTT » Réduction du temps de travail, 3 « MAL » Maladie) avec la méthode de données initiales du modèle, qui
//    voyage avec les migrations (script et bundle compris), à la différence de UseSeeding. Les identifiants sont explicites ; des objets anonymes, parce que TypeAbsence n'a pas de constructeur qui prend
//    l'identifiant. Commentaire de raison attendu. Puis une migration TypesAbsenceReference par jeu, et un test qui migre une base neuve sans données de démonstration
//    — cours/25-exploiter-l-application.md, Pas à pas, étape 10 « Données de référence : les types d'absence dans les migrations »

namespace Chronos.Infrastructure.Configurations;

public class TypeAbsenceConfiguration
{
}
