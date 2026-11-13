// TODO [module 08] Décrire ce que le client envoie pour créer un collaborateur : matricule (20 caractères au plus), nom et prénom
//    (100), adresse électronique (valide, 254), catégorie, poste (100), date d'entrée, type de contrat, quotité, et manager
//    (facultatif). Tous obligatoires sauf le manager ; les types qui doivent pouvoir manquer sont nullables pour que l'absence
//    soit détectée par la validation ; chaque annotation porte un message en français (exercice 4). RG-02 et RG-03 ne se
//    contrôlent pas ici : le domaine les refuse (422).
//    — cours/08-une-api-robuste.md, Pas à pas, étape 1 « Décrire ce que le client envoie » ; messages : À vous, exercice 4 « Des messages pour l'utilisateur. »

namespace Chronos.Api.Collaborateurs;

public record CreerCollaborateurRequete;
