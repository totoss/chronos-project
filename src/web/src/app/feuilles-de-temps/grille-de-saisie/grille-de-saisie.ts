import { DecimalPipe } from '@angular/common';
import { Component, computed, input, model } from '@angular/core';
import { libelleJour } from '../../commun/dates';
import {
  cle,
  estAffecte,
  DUREES,
  DUREES_D_UNE_DEMI_JOURNEE,
  Grille,
  LigneDeSaisie,
  totalDeLaLigne,
  totalDuJour,
} from '../feuille-de-temps';

/** Composant de présentation : il affiche et modifie une grille, il ne connaît pas l'API. */
@Component({
  imports: [DecimalPipe],
  selector: 'app-grille-de-saisie',
  styleUrl: './grille-de-saisie.scss',
  templateUrl: './grille-de-saisie.html',
})
export class GrilleDeSaisie {
  readonly jours = input.required<string[]>();
  readonly lignes = input.required<LigneDeSaisie[]>();
  /** Jours où rien ne se saisit : week-ends, jours fériés, absences d'une journée (RG-32). */
  readonly joursFermes = input(new Set<string>());
  /** Jours où une demi-journée d'absence ne laisse à saisir que 0,5 au plus (RG-32). */
  readonly joursEnDemiAbsence = input(new Set<string>());
  readonly lectureSeule = input(false);
  readonly grille = model.required<Grille>();

  protected readonly libelleJour = libelleJour;

  protected readonly totauxParJour = computed(() =>
    this.jours().map((jour) => totalDuJour(this.grille(), this.lignes(), jour)),
  );
  protected readonly totauxParLigne = computed(() =>
    this.lignes().map((ligne) => totalDeLaLigne(this.grille(), ligne, this.jours())),
  );

  protected duree(ligne: LigneDeSaisie, jour: string): number {
    return this.grille()[cle(ligne.projetId, jour)] ?? 0;
  }

  /** RG-30 : des quarts de journée ; pas plus d'une demi-journée un jour d'absence partielle. */
  protected dureesProposees(jour: string): number[] {
    return this.joursEnDemiAbsence().has(jour) ? DUREES_D_UNE_DEMI_JOURNEE : DUREES;
  }

  /** Ce que le total d'un jour ne doit pas dépasser : une journée, ou une demi-journée d'absence en moins. */
  protected limite(jour: string): number {
    return this.joursEnDemiAbsence().has(jour) ? 0.5 : 1;
  }

  protected estSaisissable(ligne: LigneDeSaisie, jour: string): boolean {
    return !this.lectureSeule() && !this.joursFermes().has(jour) && estAffecte(ligne, jour);
  }

  protected saisir(ligne: LigneDeSaisie, jour: string, valeur: string): void {
    this.grille.update((grille) => ({ ...grille, [cle(ligne.projetId, jour)]: Number(valeur) }));
  }
}
