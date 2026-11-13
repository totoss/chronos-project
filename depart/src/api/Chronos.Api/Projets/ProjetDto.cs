// TODO [module 03] Décrire un projet tel que l'API le renvoie : identifiant, code, libellé, nature, date de début, date de
//    fin (facultative), budget en jours et statut. Un « record » positionnel, propriétés en français. Au module 03, les
//    énumérations NatureProjet (Facturable, Interne) et StatutProjet (EnPreparation, EnCours, Termine, Annule) sont
//    déclarées dans ce même fichier, ainsi que la requête de création CreerProjetRequete (code, libellé, nature, dates, budget).
//    — cours/03-premiere-api.md, Pas à pas, étape 6 « Décrire un projet : le DTO »
// TODO [module 04] Les deux énumérations n'appartiennent plus à l'API : elles viennent du domaine (Chronos.Domain). Le DTO
//    les importe et ne les déclare plus.
//    — cours/04-modeliser-le-domaine.md, Pas à pas, étape 10 « Brancher l'API sur le domaine »
// TODO [module 06] Ajouter le client (identifiant facultatif et nom facultatif) après la nature, et le responsable
//    (identifiant et nom) après le budget, tous deux avant le statut.
//    — cours/06-persister-avec-ef-core.md, Pas à pas, étape 9 « Servir `/api/projets` depuis la base »
// TODO [module 08] Terminer le DTO par la version (jeton de concurrence, un Guid) : le client la renvoie à la modification.
//    La requête de création quitte ce fichier pour CreerProjetRequete.cs.
//    — cours/08-une-api-robuste.md, À vous, exercice 1 « Les projets. »
// TODO [module 10] Terminer le DTO par le total des jours validés du projet, un décimal calculé par la base.
//    — cours/10-temps-et-absences.md, À vous, exercice 4 « Le temps consommé. »

namespace Chronos.Api.Projets;

public record ProjetDto;
