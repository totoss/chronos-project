// TODO [module 12] RG-50 et RG-51 : calculer, pour l'utilisateur courant, les collaborateurs dont il peut voir les données. Le périmètre
//    vient de la hiérarchie enregistrée en base (collaborateurs archivés compris, maillons de la chaîne), non du rôle : il
//    contient la fiche de l'utilisateur et toute son équipe, directe et indirecte ; une chaîne circulaire ne doit pas faire boucler
//    le calcul ; qui n'a pas de fiche ne voit personne. Le service reçoit l'utilisateur courant et le contexte. Il offre : voir
//    tout (qui y a droit : voir la décision de l'exercice 4), reconnaître sa propre fiche, la liste des identifiants visibles,
//    « puis-je voir ce collaborateur ? » (404 sinon dans les endpoints). Classe publique et scellée, enregistrée par requête.
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 7 « Autoriser par périmètre (RG-50, RG-51) »
// TODO [module 12] RG-36 : ajouter « puis-je valider pour ce collaborateur ? » : vrai pour son manager direct ou un gestionnaire RH,
//    jamais pour soi-même, ni sans fiche.
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 8 « RG-36 : valider la feuille d'un autre »
// TODO [module 12] Trancher si l'administrateur voit toutes les fiches au regard de son rôle et de la minimisation, puis fixer la décision par un test.
//    — cours/12-securite-et-tracabilite.md, À vous, exercice 4 « L'administrateur. »

namespace Chronos.Api.Securite;

public sealed class PerimetreUtilisateur
{
}
