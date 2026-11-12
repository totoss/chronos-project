import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { App } from './app';
import { routes } from './app.routes';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter(routes, withComponentInputBinding()),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('affiche le nom de l\'application', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Chronos');
  });

  it('marque le lien de l\'écran courant avec aria-current', async () => {
    const fixture = TestBed.createComponent(App);
    await TestBed.inject(Router).navigateByUrl('/projets');
    TestBed.tick();
    TestBed.inject(HttpTestingController)
      .expectOne((r) => r.url === '/api/projets')
      .flush({ elements: [], page: 1, taille: 20, total: 0 });
    await fixture.whenStable();

    const courants = (fixture.nativeElement as HTMLElement).querySelectorAll('a[aria-current="page"]');

    expect(courants.length).toBe(1);
    expect(courants[0].textContent).toContain('Projets');
  });

  it('propose un lien de menu par écran, sans la connexion', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const liens = Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll('mat-nav-list a'),
    ).map((a) => a.getAttribute('href'));

    expect(liens).toEqual([
      '/collaborateurs',
      '/projets',
      '/plan-de-charge',
      '/feuille-de-temps',
      '/feuilles-a-valider',
      '/absences',
    ]);
  });
});
