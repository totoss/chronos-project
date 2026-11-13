// TODO [module 06] Le collaborateur tel que la liste le renvoie : identifiant, matricule, nom, prénom, adresse électronique, catégorie
//    (énumération du domaine), poste, identifiant et nom complet du manager (nul sans manager), dates d'entrée et de sortie,
//    quotité du contrat en cours (0 s'il n'y en a pas).
//    — cours/06-persister-avec-ef-core.md, À vous, exercice 3 « Lister les collaborateurs. »
// TODO [module 08] Terminer le DTO par la version (jeton de concurrence, un Guid).
//    — cours/08-une-api-robuste.md, Pas à pas, étape 7 « Modifier sans écraser le travail d'un autre »
// TODO [module 12] Le DTO devient la version réduite, sans information contractuelle (RG-52) : retirer les dates et la quotité,
//    qui passent dans CollaborateurRhDto. La version reste.
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 10 « Ne montrer que le nécessaire »
// TODO [module 16] Ajouter l'indicateur booléen « EstSorti » (vrai si la date de sortie est passée), juste avant la version :
//    il remplace les dates, absentes de la version réduite.
//    — cours/16-dialoguer-avec-l-api.md, Pas à pas, étape 3 « Écrire les types miroirs » (passage « Cet indicateur n'existe pas encore dans votre API », qui tient lieu d'encadré « Compléter le contrat de l'API »)

namespace Chronos.Api.Collaborateurs;

public record CollaborateurDto;
