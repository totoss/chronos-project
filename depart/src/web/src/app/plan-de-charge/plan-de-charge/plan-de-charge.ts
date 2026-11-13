// TODO [module 15] Composant provisoire de la route /plan-de-charge ; son nom et son dossier sont
//    déjà définitifs.
//    — cours/15-routage.md, À vous, exercice 1 « Les autres écrans. »
// TODO [module 15] Test, plan-de-charge.spec.ts : créer à la main le fichier : test « se crée » du
//    composant provisoire.
//    — cours/15-routage.md, À vous, exercice 1 « Les autres écrans. »
// TODO [module 18] Écran qui lit l'API par un `httpResource` : quatre semaines à partir du lundi de
//    `SelectionSemaine` ; périodes en colonnes (`computed`).
//    — cours/18-ecrans-metier.md, Pas à pas, étape 9 « Le plan de charge »
// TODO [module 18] Confirmer une surcharge par le dialogue `ConfirmationDeSurcharge`
//    (`MatDialog.open`) et la retenir dans un signal.
//    — cours/18-ecrans-metier.md, Pas à pas, étape 10 « Confirmer une surcharge »
// TODO [module 18] Un bouton ouvre le formulaire d'affectation dans un dialogue ; une affectation
//    créée recharge le plan.
//    — cours/18-ecrans-metier.md, À vous, exercice 4 « Une confirmation qui compte. »
// TODO [module 18] Un choix « semaine / mois » (`pas=mois`) ; décider où il vit.
//    — cours/18-ecrans-metier.md, À vous, exercice 5 « Par mois. »
// TODO [module 18] Test, plan-de-charge.spec.ts : quatre semaines demandées, ligne par
//    collaborateur, libellé par niveau, surcharge confirmée dans un dialogue, annulation,
//    changement de semaine, passage au mois.
//    — cours/18-ecrans-metier.md, non détaillé dans le module : comparer avec la solution au tag `module-18`
// TODO [module 21] Une description de la période (« semaine du lun. 4 mai », « mai 2026 ») pour le
//    contexte des indicateurs et le dialogue de confirmation.
//    — cours/21-accessibilite-et-finitions.md, À vous, exercice 2 « Le plan de charge. »
// TODO [module 21] Test, plan-de-charge.spec.ts : le tableau est nommé et chaque bouton dit quelle
//    surcharge il confirme ; taux attendu au format français (« 133 % », espace insécable).
//    — cours/21-accessibilite-et-finitions.md, non détaillé dans le module : comparer avec la solution au tag `module-21`
import { Component } from '@angular/core';

@Component({
  selector: 'app-plan-de-charge',
  styleUrl: './plan-de-charge.scss',
  templateUrl: './plan-de-charge.html',
})
export class PlanDeCharge {}
