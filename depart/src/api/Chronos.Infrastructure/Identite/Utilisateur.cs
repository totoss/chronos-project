// TODO [module 12] Le compte de connexion : il hérite d'IdentityUser à clé entière (par défaut la clé est une chaîne ; ce choix suit le reste du modèle), et RÉFÉRENCE un collaborateur sans se confondre avec lui :
//    un identifiant de collaborateur facultatif (un administrateur peut n'être rattaché à aucune fiche) et la navigation vers le collaborateur. Le domaine ne connaît rien d'Identity. La classe de base
//    n'est pas déclarée ici : elle exige le paquet Identity
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 1 « Le compte utilisateur et ses tables »

namespace Chronos.Infrastructure.Identite;

public class Utilisateur
{
}
