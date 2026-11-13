// TODO [module 08] La requête de modification (PUT) : nom, prénom, adresse électronique, poste, date de sortie (facultative), manager
//    (facultatif), et la version lue par le client (obligatoire : c'est le jeton de concurrence optimiste). Le matricule, la
//    catégorie et le contrat ne se modifient pas ici.
//    — cours/08-une-api-robuste.md, Pas à pas, étape 7 « Modifier sans écraser le travail d'un autre »
// TODO [module 17] Donner à chaque annotation, y compris celle de la version, un message en français : le formulaire les affiche tels quels.
//    — cours/17-formulaires.md, Pas à pas, étape 6 « Les refus de l'API » (encadré « Compléter le contrat de l'API. »)

namespace Chronos.Api.Collaborateurs;

public record ModifierCollaborateurRequete;
