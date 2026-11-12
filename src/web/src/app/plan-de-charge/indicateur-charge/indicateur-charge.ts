import { PercentPipe } from '@angular/common';
import {
  afterRenderEffect,
  Component,
  computed,
  ElementRef,
  input,
  output,
  viewChild,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { Charge, LIBELLES_NIVEAU, niveauDeCharge } from '../charge';

/** Composant de présentation : le taux de charge d'une période, et ce qu'il signifie. */
@Component({
  host: { '[class]': 'niveau()' },
  imports: [MatButtonModule, PercentPipe],
  selector: 'app-indicateur-charge',
  styleUrl: './indicateur-charge.scss',
  template: `
    @if (charge().tauxDeCharge !== null) {
      <span class="taux">{{ charge().tauxDeCharge | percent }}</span>
    }
    <span class="niveau">{{ libelle() }}</span>
    @if (niveau() === 'surcharge') {
      @if (confirmee()) {
        <span class="niveau" tabindex="-1" #marque>confirmée</span>
      } @else {
        <button
          matButton
          type="button"
          [attr.aria-label]="nomAccessible()"
          (click)="confirmer.emit()"
        >
          Confirmer
        </button>
      }
    }
  `,
})
export class IndicateurCharge {
  readonly charge = input.required<Charge>();
  readonly confirmee = input(false);
  /** Qui et quand (« Camille Durand, semaine du lun. 4 mai ») : sans cela, le bouton ne dit pas quelle surcharge il confirme. */
  readonly contexte = input('');
  readonly confirmer = output<void>();

  protected readonly niveau = computed(() => niveauDeCharge(this.charge()));
  protected readonly libelle = computed(() => LIBELLES_NIVEAU[this.niveau()]);

  /** Commence par le texte visible du bouton, pour qu'on puisse l'appeler par ce qu'on lit. */
  protected readonly nomAccessible = computed(() =>
    this.contexte() === '' ? null : `Confirmer la surcharge de ${this.contexte()}`,
  );

  private readonly marque = viewChild<ElementRef<HTMLElement>>('marque');

  constructor() {
    // Le bouton « Confirmer » disparaît quand la surcharge est confirmée : si le focus s'est perdu avec lui,
    // il passe à la mention « confirmée » plutôt que de retomber en haut de la page.
    afterRenderEffect(() => {
      const marque = this.marque();
      if (marque && document.activeElement === document.body) {
        marque.nativeElement.focus();
      }
    });
  }
}
