import { FieldTree, isFieldTree, ValidationError } from '@angular/forms/signals';
import { ErreurApi } from './erreur-api';

/**
 * Traduit une erreur de l'API en erreurs de formulaire.
 * - 400 : chaque message est rattaché à son champ ;
 * - 409, 422, ou 400 dont aucun champ n'est reconnu : une erreur sur le formulaire entier, avec
 *   le message général ; son `kind` est la règle (`RG-xx`) ou le code (`concurrence`) quand l'API
 *   les donne, sinon `http-400`, `http-409` ou `http-422` ;
 * - autre chose (panne, 500) : aucune erreur de formulaire, l'intercepteur a déjà notifié.
 */
export function erreursDeFormulaire(
  erreur: unknown,
  formulaire: FieldTree<unknown>,
): ValidationError.WithOptionalFieldTree[] {
  if (!(erreur instanceof ErreurApi) || !erreur.estDeSaisie) {
    return [];
  }

  const champs = formulaire as unknown as Record<string, unknown>;
  const erreurs: ValidationError.WithOptionalFieldTree[] = [];

  for (const [nom, messages] of Object.entries(erreur.champs)) {
    // L'API écrit « Nom » ou « nom » selon sa configuration : on accepte les deux.
    const champ = champs[nom.charAt(0).toLowerCase() + nom.slice(1)];
    if (isFieldTree(champ)) {
      for (const message of messages) {
        erreurs.push({ kind: 'serveur', message, fieldTree: champ });
      }
    }
  }

  if (erreurs.length === 0) {
    erreurs.push({
      kind: erreur.regle ?? erreur.code ?? `http-${erreur.statut}`,
      message: erreur.message,
    });
  }
  return erreurs;
}
