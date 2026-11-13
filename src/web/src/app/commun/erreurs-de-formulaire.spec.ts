import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { form } from '@angular/forms/signals';
import { ErreurApi } from './erreur-api';
import { erreursDeFormulaire } from './erreurs-de-formulaire';

describe('erreursDeFormulaire', () => {
  const formulaire = TestBed.runInInjectionContext(() => form(signal({ email: '', quotite: 0 })));

  it('rattache chaque message d’un 400 au champ qui porte le même nom', () => {
    const erreur = new ErreurApi(400, 'Invalide.', null, {
      Email: ['Adresse invalide.'],
      Quotite: ['Trop haute.', 'Trop précise.'],
    });

    const erreurs = erreursDeFormulaire(erreur, formulaire);

    expect(erreurs.map((e) => e.message)).toEqual([
      'Adresse invalide.',
      'Trop haute.',
      'Trop précise.',
    ]);
    expect(erreurs[0].fieldTree).toBe(formulaire.email);
    expect(erreurs[1].fieldTree).toBe(formulaire.quotite);
    expect(erreurs[2].fieldTree).toBe(formulaire.quotite);
  });

  it('n’affiche pas une clé inconnue telle quelle : seul le message général apparaît', () => {
    const erreur = new ErreurApi(400, 'Les données envoyées sont invalides.', null, {
      '$.version': ['The JSON value could not be converted.'],
    });

    const erreurs = erreursDeFormulaire(erreur, formulaire);

    expect(erreurs).toEqual([
      { kind: 'http-400', message: 'Les données envoyées sont invalides.' },
    ]);
  });

  it('rattache un 409 au formulaire, sous le code http-409', () => {
    const [erreur] = erreursDeFormulaire(new ErreurApi(409, 'Déjà pris.'), formulaire);

    expect(erreur).toEqual({ kind: 'http-409', message: 'Déjà pris.' });
  });

  it('prend pour kind le code d’un 409 de concurrence, et la règle d’un doublon', () => {
    const concurrence = new ErreurApi(409, 'Périmée.', null, {}, 'concurrence');
    const doublon = new ErreurApi(409, 'Déjà pris.', 'RG-01');

    expect(erreursDeFormulaire(concurrence, formulaire)[0].kind).toBe('concurrence');
    expect(erreursDeFormulaire(doublon, formulaire)[0].kind).toBe('RG-01');
  });

  it('prend pour kind la règle d’un 422', () => {
    const [erreur] = erreursDeFormulaire(new ErreurApi(422, 'Quotité.', 'RG-03'), formulaire);

    expect(erreur).toEqual({ kind: 'RG-03', message: 'Quotité.' });
  });

  it('ne renvoie rien pour une panne, une erreur 500 ou une exception inconnue', () => {
    expect(erreursDeFormulaire(new ErreurApi(0, 'Injoignable.'), formulaire)).toEqual([]);
    expect(erreursDeFormulaire(new ErreurApi(500, 'Serveur.'), formulaire)).toEqual([]);
    expect(erreursDeFormulaire(new Error('autre'), formulaire)).toEqual([]);
  });
});
