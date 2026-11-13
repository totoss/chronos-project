// TODO [module 09] Entité affectation : Id, CollaborateurId, ProjetId, navigation « Projet? Projet », DateDebut, DateFin, TauxAllocation (decimal) ; accesseurs d'écriture privés.
//    Le constructeur est PRIVÉ (ses paramètres portent le nom des propriétés : EF Core s'en sert pour relire une affectation) ; la seule porte d'entrée est la fabrique
//    statique « Creer(collaborateur, projet, dateDebut, dateFin, tauxAllocation) », qui vérifie : RG-13 (appeler Projet.VerifierOuvert), RG-21 (taux entre 5 % et 100 %,
//    bornes comprises), RG-20 (la fin ne précède pas le début ; l'affectation tient dans la période du projet, dont la fin peut être absente ; elle tient dans la présence
//    du collaborateur : date d'entrée, date de sortie éventuelle) ; violation = RegleMetierException avec le code de la règle. Écrire les tests de RG-20, RG-21 et RG-13
//    — cours/09-affectations-et-calcul-de-charge.md, Pas à pas, étape 1 « `Affectation` et `JourFerie` »
// TODO [module 09] Ajouter une méthode « Modifier » qui revérifie RG-13, RG-20 et RG-21 sans dupliquer « Creer » (partager la vérification), refuse un autre collaborateur ou un autre
//    projet que ceux de l'affectation, et laisse l'affectation intacte en cas de refus
//    — cours/09-affectations-et-calcul-de-charge.md, À vous, exercice 1 « Les affectations au complet. »
// TODO [module 12] Implémenter ITracable (RG-53) : les quatre propriétés de trace, à accesseurs d'écriture privés
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 9 « Tracer (RG-53) »

namespace Chronos.Domain;

public class Affectation
{
}
