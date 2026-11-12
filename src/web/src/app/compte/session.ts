import { HttpClient } from '@angular/common/http';
import { computed, inject, Service, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { Role, Utilisateur } from './utilisateur';

/** Qui est connecté. L'interface ne garde aucun jeton : le navigateur porte le cookie de session. */
@Service()
export class Session {
  private readonly http = inject(HttpClient);
  private readonly utilisateurCourant = signal<Utilisateur | null>(null);

  readonly utilisateur = this.utilisateurCourant.asReadonly();
  readonly estConnecte = computed(() => this.utilisateurCourant() !== null);
  readonly roles = computed<readonly Role[]>(() => this.utilisateurCourant()?.roles ?? []);

  /** La fiche de l'utilisateur ; `null` pour un compte qui n'en a pas (ou quand personne n'est connecté). */
  readonly collaborateurId = computed(() => this.utilisateurCourant()?.collaborateurId ?? null);

  aLeRole(...roles: Role[]): boolean {
    return this.roles().some((role) => roles.includes(role));
  }

  /** Demande à l'API qui est connecté. Ne lève jamais : un 401 signifie « personne ». */
  async charger(): Promise<void> {
    try {
      const utilisateur = await firstValueFrom(this.http.get<Utilisateur>('/api/compte/moi'));
      this.utilisateurCourant.set(utilisateur);
    } catch {
      this.utilisateurCourant.set(null);
    }
  }

  /** Laisse passer l'erreur : l'écran de connexion doit savoir que la tentative a échoué. */
  async connecter(email: string, password: string): Promise<void> {
    await firstValueFrom(
      this.http.post('/api/compte/login', { email, password }, { params: { useCookies: true } }),
    );
    // La réponse du login est vide ; c'est /moi qui dit qui est connecté et qui dépose le cookie XSRF-TOKEN.
    await this.charger();
  }

  async deconnecter(): Promise<void> {
    try {
      await firstValueFrom(this.http.post('/api/compte/logout', {}));
    } catch {
      // Session déjà expirée (401) ou API injoignable : l'interface se déconnecte quand même.
    }
    this.utilisateurCourant.set(null);
  }

  /** L'API vient de répondre 401 : l'interface s'aligne. */
  oublier(): void {
    this.utilisateurCourant.set(null);
  }
}
