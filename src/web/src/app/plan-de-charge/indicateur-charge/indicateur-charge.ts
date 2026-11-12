import { PercentPipe } from '@angular/common';
import { Component, computed, input, output } from '@angular/core';
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
        <span class="niveau">confirmée</span>
      } @else {
        <button matButton type="button" (click)="confirmer.emit()">Confirmer</button>
      }
    }
  `,
})
export class IndicateurCharge {
  readonly charge = input.required<Charge>();
  readonly confirmee = input(false);
  readonly confirmer = output<void>();

  protected readonly niveau = computed(() => niveauDeCharge(this.charge()));
  protected readonly libelle = computed(() => LIBELLES_NIVEAU[this.niveau()]);
}
