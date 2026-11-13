// TODO [module 09] La requête de modification d'une affectation (PUT) : dates de début et de fin, taux d'allocation (obligatoires, messages en
//    français) et l'indicateur facultatif « surchargeConfirmee ». Le collaborateur et le projet d'une affectation ne changent pas :
//    on supprime et on recrée.
//    — cours/09-affectations-et-calcul-de-charge.md, À vous, exercice 1 « Les affectations au complet. » (et exercice 3 « Confirmer une surcharge (RG-24). »)

namespace Chronos.Api.Affectations;

public record ModifierAffectationRequete;
