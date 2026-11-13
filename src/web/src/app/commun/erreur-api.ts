import { HttpErrorResponse } from '@angular/common/http';

/** Une erreur de l'API, traduite depuis sa réponse ProblemDetails. */
export class ErreurApi extends Error {
  constructor(
    /** Code HTTP ; 0 quand l'API n'a pas répondu. */
    readonly statut: number,
    message: string,
    /** Code de la règle de gestion violée (réponse 422), par exemple 'RG-11'. */
    readonly regle: string | null = null,
    /** Messages par champ (réponse 400). Les clés sont les noms de champs renvoyés par l'API. */
    readonly champs: Record<string, string[]> = {},
    /** Code de l'extension `code` de la réponse, par exemple 'concurrence' (409). */
    readonly code: string | null = null,
  ) {
    super(message);
    this.name = 'ErreurApi';
  }

  /** L'API a refusé les données envoyées : à l'utilisateur de les corriger. */
  get estDeSaisie(): boolean {
    return this.statut === 400 || this.statut === 409 || this.statut === 422;
  }
}

interface ProblemDetails {
  title?: string;
  detail?: string;
  regle?: string;
  code?: string;
  errors?: Record<string, string[]>;
}

/** Quand l'API n'écrit pas de `detail`, son `title` est en anglais : on le remplace. */
const LIBELLES: Record<number, string> = {
  400: 'Les données envoyées sont invalides.',
  401: 'Votre session a expiré. Reconnectez-vous.',
  403: "Vous n'avez pas le droit d'effectuer cette action.",
  404: "La ressource demandée n'existe pas, ou vous n'y avez pas accès.",
  500: 'Une erreur est survenue sur le serveur.',
};

export function versErreurApi(erreur: unknown): ErreurApi {
  if (!(erreur instanceof HttpErrorResponse)) {
    return new ErreurApi(0, 'Une erreur inattendue est survenue.');
  }
  if (erreur.status === 0) {
    return new ErreurApi(0, "L'API ne répond pas. Vérifiez votre connexion, puis réessayez.");
  }
  const probleme = (erreur.error ?? {}) as ProblemDetails;
  const message =
    probleme.detail ??
    LIBELLES[erreur.status] ??
    `L'API a répondu par une erreur ${erreur.status}.`;
  return new ErreurApi(
    erreur.status,
    message,
    probleme.regle ?? null,
    probleme.errors ?? {},
    probleme.code ?? null,
  );
}
