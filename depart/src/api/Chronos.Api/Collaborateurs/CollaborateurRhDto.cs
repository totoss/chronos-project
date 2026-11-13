// TODO [module 12] La version complète d'une fiche, réservée au gestionnaire RH et à l'intéressé (RG-52) : tous les champs de
//    CollaborateurDto, plus la date d'entrée, la date de sortie (facultative) et la quotité du contrat en cours. Les champs
//    absents de la version réduite ne sont pas masqués par l'interface : ils ne quittent pas le serveur (minimisation des données).
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 10 « Ne montrer que le nécessaire »
// TODO [module 16] Ajouter l'indicateur booléen « EstSorti », juste avant la version, calculé avec la même règle que le filtre
//    « statut=sorti ».
//    — cours/16-dialoguer-avec-l-api.md, Pas à pas, étape 3 « Écrire les types miroirs » (passage « Cet indicateur n'existe pas encore dans votre API », qui tient lieu d'encadré « Compléter le contrat de l'API »)

namespace Chronos.Api.Collaborateurs;

public record CollaborateurRhDto;
