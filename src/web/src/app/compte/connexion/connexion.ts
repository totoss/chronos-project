import { Component, inject, input, signal } from '@angular/core';
import { email, form, FormField, FormRoot, required } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Router } from '@angular/router';
import { ErreurApi } from '../../commun/erreur-api';
import { Session } from '../session';

@Component({
  imports: [FormField, FormRoot, MatButtonModule, MatFormFieldModule, MatInputModule],
  selector: 'app-connexion',
  styleUrl: './connexion.scss',
  templateUrl: './connexion.html',
})
export class Connexion {
  /** L'adresse à rejoindre après la connexion, reçue du paramètre `?retour=` de l'URL. */
  readonly retour = input('/', { transform: (valeur: string | undefined) => valeur ?? '/' });

  private readonly session = inject(Session);
  private readonly router = inject(Router);

  protected readonly echec = signal<string | null>(null);
  protected readonly modele = signal({ email: '', password: '' });

  protected readonly formulaire = form(
    this.modele,
    (champs) => {
      required(champs.email, { message: "L'adresse électronique est obligatoire." });
      email(champs.email, { message: "L'adresse électronique est invalide." });
      required(champs.password, { message: 'Le mot de passe est obligatoire.' });
    },
    {
      submission: {
        action: async (champs) => {
          this.echec.set(null);
          const { email, password } = champs().value();
          try {
            await this.session.connecter(email, password);
          } catch (erreur) {
            // API injoignable : le dire, plutôt que de laisser croire à un mot de passe erroné.
            const injoignable = erreur instanceof ErreurApi && erreur.statut === 0;
            this.echec.set(
              injoignable ? erreur.message : 'Adresse électronique ou mot de passe incorrect.',
            );
            return;
          }
          await this.router.navigateByUrl(this.destination());
        },
      },
    },
  );

  /** Le paramètre vient de l'URL, donc de n'importe qui : on ne suit que les chemins internes. */
  private destination(): string {
    const retour = this.retour();
    return retour.startsWith('/') && !retour.startsWith('//') ? retour : '/';
  }
}
