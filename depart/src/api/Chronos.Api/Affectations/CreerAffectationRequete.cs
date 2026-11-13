// TODO [module 09] La requête de création d'une affectation : collaborateur, projet, dates de début et de fin, taux d'allocation, tous
//    obligatoires (types nullables pour que l'absence soit détectée par la validation, messages en français), et l'indicateur
//    facultatif « surchargeConfirmee » (faux par défaut, RG-24). Le taux n'a pas de borne d'annotation : sa borne est RG-21, qui
//    répond 422 et non 400.
//    — cours/09-affectations-et-calcul-de-charge.md, Pas à pas, étape 9 « `GET /api/plan-de-charge` » ; confirmation de la surcharge : À vous, exercice 3 « Confirmer une surcharge (RG-24). »

namespace Chronos.Api.Affectations;

public record CreerAffectationRequete;
