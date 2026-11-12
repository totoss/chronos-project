import { Component, computed, signal } from '@angular/core';
import { contientSansAccents } from '../../commun/contient-sans-accents';
import { Collaborateur } from '../collaborateur';

@Component({
  imports: [],
  selector: 'app-liste-collaborateurs',
  styleUrl: './liste-collaborateurs.scss',
  templateUrl: './liste-collaborateurs.html',
})
export class ListeCollaborateurs {
  protected readonly collaborateurs = signal<Collaborateur[]>([
    { id: 1, matricule: 'C0001', nom: 'Martin', prenom: 'Camille', email: 'camille.martin@chronos.example', categorie: 'Consultant', poste: 'Consultante confirmée', dateEntree: '2021-09-01', dateSortie: null },
    { id: 2, matricule: 'C0002', nom: 'Bernard', prenom: 'Louis', email: 'louis.bernard@chronos.example', categorie: 'Consultant', poste: 'Consultant junior', dateEntree: '2025-02-03', dateSortie: null },
    { id: 3, matricule: 'C0003', nom: 'Dubois', prenom: 'Inès', email: 'ines.dubois@chronos.example', categorie: 'Consultant', poste: 'Manager', dateEntree: '2018-04-16', dateSortie: null },
    { id: 4, matricule: 'I0001', nom: 'Marchand', prenom: 'Hugo', email: 'hugo.marchand@chronos.example', categorie: 'Interne', poste: 'Gestionnaire RH', dateEntree: '2019-01-07', dateSortie: null },
    { id: 5, matricule: 'C0004', nom: 'Petit', prenom: 'Sarah', email: 'sarah.petit@chronos.example', categorie: 'Consultant', poste: 'Consultante senior', dateEntree: '2017-11-02', dateSortie: '2026-03-31' },
  ]);

  protected readonly filtre = signal('');

  protected readonly afficherSortis = signal(false);

  protected readonly tri = signal<'nom' | 'dateEntree'>('nom');

  protected readonly collaborateursFiltres = computed(() => {
    const texte = this.filtre().trim();
    const avecSortis = this.afficherSortis();
    const critere = this.tri();

    // `filter` renvoie un nouveau tableau : `sort` ne modifie pas celui du signal.
    return this.collaborateurs()
      .filter(
        (c) =>
          (avecSortis || c.dateSortie === null) &&
          (contientSansAccents(c.nom, texte) ||
            contientSansAccents(c.prenom, texte) ||
            contientSansAccents(c.matricule, texte)),
      )
      .sort((a, b) =>
        critere === 'nom'
          ? a.nom.localeCompare(b.nom, 'fr')
          : a.dateEntree.localeCompare(b.dateEntree),
      );
  });

  protected readonly repartition = computed(() => {
    const liste = this.collaborateursFiltres();
    const consultants = liste.filter((c) => c.categorie === 'Consultant').length;
    const internes = liste.length - consultants;

    return `${consultants} ${consultants > 1 ? 'consultants' : 'consultant'}, ${internes} ${internes > 1 ? 'internes' : 'interne'}`;
  });

  protected effacerFiltre(): void {
    this.filtre.set('');
  }

  protected basculerSortis(): void {
    this.afficherSortis.update((valeur) => !valeur);
  }

  protected basculerTri(): void {
    this.tri.update((valeur) => (valeur === 'nom' ? 'dateEntree' : 'nom'));
  }
}
