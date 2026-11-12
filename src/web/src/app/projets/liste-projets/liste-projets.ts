import { Component, computed, signal } from '@angular/core';
import { BarreDeRecherche } from '../../commun/barre-de-recherche/barre-de-recherche';
import { CompteurDeResultats } from '../../commun/compteur-de-resultats/compteur-de-resultats';
import { contientSansAccents } from '../../commun/contient-sans-accents';
import { CarteProjet } from '../carte-projet/carte-projet';
import { Projet } from '../projet';

@Component({
  imports: [BarreDeRecherche, CarteProjet, CompteurDeResultats],
  selector: 'app-liste-projets',
  styleUrl: './liste-projets.scss',
  templateUrl: './liste-projets.html',
})
export class ListeProjets {
  protected readonly projets = signal<Projet[]>([
    { code: 'BDL-REFONTE', libelle: 'Refonte du portail client', nature: 'Facturable', statut: 'EnCours', client: 'Banque du Lac' },
    { code: 'ASM-MIGRATION', libelle: 'Migration vers le cloud', nature: 'Facturable', statut: 'EnPreparation', client: 'Assurances du Midi' },
    { code: 'BDL-AUDIT', libelle: 'Audit de sécurité', nature: 'Facturable', statut: 'Termine', client: 'Banque du Lac' },
    { code: 'INT-FORMATION', libelle: 'Plan de formation interne', nature: 'Interne', statut: 'EnCours', client: null },
    { code: 'INT-OUTILS', libelle: 'Outillage de la société', nature: 'Interne', statut: 'Annule', client: null },
  ]);

  protected readonly recherche = signal('');

  protected readonly statutFiltre = signal('');

  protected readonly projetsFiltres = computed(() => {
    const texte = this.recherche().trim();
    const statut = this.statutFiltre();

    return this.projets().filter(
      (p) =>
        (statut === '' || p.statut === statut) &&
        (contientSansAccents(p.code, texte) ||
          contientSansAccents(p.libelle, texte) ||
          contientSansAccents(p.client ?? '', texte)),
    );
  });
}
