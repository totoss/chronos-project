# Chronos — projet de départ

Ce dossier est le **corps du projet Chronos, vidé de son implémentation**. La structure et l'architecture sont en place : la solution .NET et ses sept projets, l'espace de travail Angular, un fichier pour chaque fichier de l'application finale. Le code reste à écrire, module après module, en suivant le [cours](../cours/README.md).

Chaque endroit où vous devez écrire quelque chose porte une **annotation `TODO [module NN]`** qui dit quoi faire et renvoie au passage du cours qui le demande.

La [solution de référence](../cours/README.md#solution-de-référence) reste à part, dans `src/` à la racine du dépôt du cours, avec ses tags `module-NN`. Ce dossier-ci est le point de départ ; la solution est le point d'arrivée, à ne consulter qu'après avoir fait le travail.

## Démarrer

Le contenu de ce dossier se copie **à la racine de votre propre dépôt**. Les chemins que cite le cours (`src/api/Chronos.Api/…`, `compose.yaml`) y sont alors valables tels quels.

Au [module 00](../cours/00-environnement-de-travail.md), étape 11 « Créer votre dépôt », après `git init` et avant le premier commit, copiez le squelette depuis votre clone du dépôt du cours :

```powershell
Copy-Item -Path ..\chronos-project\depart\* -Destination . -Recurse
```

Les fichiers et dossiers dont le nom commence par un point (`.gitignore`, `.github`…) sont copiés avec le reste. Renommez ensuite ce mode d'emploi, pour laisser `README.md` à votre projet :

```powershell
Rename-Item README.md DEPART.md
```

L'état de départ compile. Vérifiez-le une fois le module 00 terminé :

| Dossier | Commande | Résultat attendu |
| --- | --- | --- |
| `src/api` | `dotnet build` | 0 erreur, 0 avertissement |
| `src/web` | `npm ci`, puis `npx ng build` | aucune erreur |
| `src/web` | `npx ng test --watch=false` | 1 test, qui passe |

`dotnet test` n'a rien à exécuter avant le module 04 : les projets de tests existent, mais ne deviennent de vrais projets xUnit qu'à ce module.

## Ce que contient le squelette

| Sorte de fichier | Ce que vous y trouvez | Exemples |
| --- | --- | --- |
| **Fournis** | La plomberie, dans l'état du module qui l'introduit. Ce que les modules suivants y ajoutent est annoté. | `Chronos.slnx`, les `.csproj` et leurs références entre projets, `global.json`, `angular.json`, `package.json`, `.gitignore` |
| **D'assemblage** | Une version minimale qui compile, avec une annotation par ajout, **à l'endroit où le code ira**. | `Program.cs`, `ChronosDbContext.cs`, `app.config.ts`, `app.routes.ts`, `compose.yaml` |
| **À écrire** | L'en-tête d'annotations, puis le type déclaré sans aucun membre — ou rien d'autre quand une déclaration vide serait trompeuse (garde, intercepteur, endpoints). | `Collaborateur.cs`, `CollaborateursEndpoints.cs`, `session.ts`, `auth-guard.ts` |

Trois choses à savoir sur la plomberie .NET :

- Les **références entre projets** sont posées : c'est l'architecture (`Chronos.Api` → `Chronos.Infrastructure` → `Chronos.Domain`, un projet de migrations par SGBD, deux projets de tests).
- Les **paquets NuGet ne sont pas installés**. Chaque `dotnet package add` du cours reste à faire, et s'annote dans le `.csproj` concerné et dans `Directory.Packages.props`.
- Les deux projets de tests compilent à vide. Le module 04 (tests du domaine) et le module 11 (tests d'intégration) en font des projets xUnit ; leur `.csproj` dit exactement quoi y écrire.

## Lire une annotation

```csharp
// TODO [module 08] GET /{id:int} lit une fiche (200 ou 404).
//    — cours/08-une-api-robuste.md, Pas à pas, étape 3 « Le même format pour toutes les erreurs »
```

- `[module 08]` : le module où ce travail se fait.
- Le texte dit **quoi** obtenir — un comportement, une route, un code de réponse, une règle `RG-xx` du [cahier des charges](../docs/cahier-des-charges.md). Il ne dit pas comment : c'est le module qui l'explique.
- La dernière ligne, après le tiret, est la **référence** : le fichier du module, la rubrique, le numéro et le titre exact de l'étape ou de l'exercice. Les chemins `cours/…` sont relatifs à la racine du dépôt du cours.
- Une annotation qui renvoie à un exercice ne donne pas sa réponse. Quand l'exercice pose une question (« où placez-vous la vérification ? »), l'annotation la rappelle sans y répondre.
- La référence « non détaillé dans le module : comparer avec la solution au tag `module-NN` » signale un point que la solution contient sans que le module le décrive pas à pas : faites-le au mieux, puis comparez. C'est le cas de nombreux tests de l'interface avant le module 20 : la solution teste ses écrans au fil de l'eau, alors que le cours n'enseigne les tests d'interface qu'au module 20. Voyez avec votre mentor si vous les écrivez sur le moment ou à ce module-là.

Pour lister le travail d'un module, depuis la racine de votre dépôt :

```powershell
git grep -n "TODO \[module 08\]"
```

## Travailler un module

1. Lisez le module.
2. Listez ses annotations avec la commande ci-dessus : vous savez dans quels fichiers il vous emmène.
3. Faites le pas à pas, puis les exercices. **Supprimez chaque annotation une fois son travail fait.**
4. À la fin du module, la commande ne doit plus rien renvoyer pour ce numéro, et les critères de réussite du module doivent passer.
5. Comparez alors avec la solution, dans le dépôt du cours : `git diff module-07 module-08 -- src` montre ce que le module ajoute.

Les exercices qui ajoutent quelque chose à l'application sont annotés comme le pas à pas : les modules suivants les supposent faits.

## Les commandes du cours à ne pas relancer

Le cours part d'un dossier vide et fait créer projets et fichiers par des commandes. Ici, ils existent déjà : ces commandes **refusent** de s'exécuter, et les forcer écraserait les annotations.

| Commande du cours | Sur le squelette | Ce que vous faites à la place |
| --- | --- | --- |
| `dotnet new globaljson`, `dotnet new sln`, `dotnet new webapi`, `dotnet new classlib`, `dotnet new xunit3` | Le fichier ou le projet existe : la commande refuse de l'écraser et propose `--force`. Ne forcez pas. | Rien, ou ce que dit l'annotation du `.csproj` (modules 04 et 11 : écrire à la main ce que le modèle xUnit aurait produit). |
| `dotnet sln add`, `dotnet reference add`, `dotnet reference remove` | Déjà fait : les références entre projets sont celles de l'état final. | Rien. |
| `ng new` | L'espace de travail existe. | Rien. |
| `ng generate component`, `service`, `guard`, `interceptor` pour un fichier présent dans le squelette | `A merge conflicted on path "…"`, code de sortie 1, rien n'est écrit, même avec `--force`. | Complétez les fichiers existants et **créez le fichier `.spec.ts` à la main** (modèles ci-dessous). |

`ng generate` fonctionne normalement pour les fichiers que le squelette ne contient pas : les fichiers provisoires du cours, notamment.

Les commandes suivantes, elles, se lancent comme le cours le dit :

| Commande | Remarque |
| --- | --- |
| `dotnet package add`, `dotnet user-secrets init`, `dotnet new tool-manifest`, `dotnet ef …` | Aucune. |
| `ng add @angular/material` (module 14) | Modifie `styles.scss` et `index.html` ; leurs annotations survivent. |
| `ng add playwright-ng-schematics` (module 20) | Crée `playwright.config.ts` et `e2e/tsconfig.json` : ne les créez pas avant, la commande échouerait. N'ajoutez aucun commentaire à `angular.json`, elle le lit en JSON strict. Supprimez `e2e/LISEZMOI.md` une fois ses annotations traitées. |
| `ng add angular-eslint` (module 22) | **Remplace** `eslint.config.js` : lisez ses annotations avant de lancer la commande. |

### Modèles de fichiers de test Angular

Le squelette ne contient aucun `*.spec.ts`, sauf `src/app/app.spec.ts` : un fichier de test sans test ferait échouer `ng test`. Quand le cours dit de garder ou de corriger le test généré avec un composant, créez-le à la main. C'est ce que la CLI aurait écrit :

```ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ListeCollaborateurs } from './liste-collaborateurs';

describe('ListeCollaborateurs', () => {
  let component: ListeCollaborateurs;
  let fixture: ComponentFixture<ListeCollaborateurs>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [ListeCollaborateurs] }).compileComponents();
    fixture = TestBed.createComponent(ListeCollaborateurs);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
```

C'est ce test `should create` que les annotations appellent « se crée ». Les autres modèles de la CLI sont plus courts :

- **Service** : `TestBed.configureTestingModule({})`, `service = TestBed.inject(Session)`, et un test `should be created`. Il ne passe qu'une fois le décorateur `@Service()` écrit sur la classe : le squelette la fournit sans.
- **Garde ou intercepteur** : la fonction s'appelle dans un contexte d'injection, puis le même test `should be created`.

```ts
const executeGuard: CanActivateFn = (...guardParameters) =>
  TestBed.runInInjectionContext(() => authGuard(...guardParameters));
```

Chaque fichier qui a un test dans la solution porte une annotation « Test, `x.spec.ts` : … » par module qui y ajoute des tests.

## Étapes du cours déjà faites

| Module, étape | Ce que le squelette fournit | Ce qu'il vous reste |
| --- | --- | --- |
| 00, étape 11 « Créer votre dépôt » | `.gitattributes`, `.gitignore`, `.editorconfig` | `git init`, lire ces trois fichiers, le premier commit, `gh repo create` |
| 01, étape 7 « Sortir les mots de passe du dépôt » | `.env.example` | Créer `.env` ; écrire `compose.yaml` (étape 6), dont seul le nom de projet est posé |
| 03, étape 1 « Fixer la version du SDK » | `global.json` | Rien |
| 03, étape 2 « Créer la solution et le projet » | `Chronos.slnx`, `Chronos.Api.csproj` | Rien |
| 03, étape 3 « Lire ce que le modèle a généré » | `appsettings*.json`, `launchSettings.json` | Lire ; écrire dans `Program.cs` ce que le modèle y aurait mis (OpenAPI), d'après ses annotations |
| 03, étape 4 « Fixer le port et lancer l'API » | Le profil `http` et le port 5080 | Lancer l'API |
| 03, étape 5 « Centraliser les réglages et les versions » | `Directory.Build.props`, `Directory.Packages.props` (sans version de paquet) | Installer les paquets, inscrire leurs versions |
| 04, étape 1 « Créer le projet du domaine » | `Chronos.Domain.csproj`, dans la solution | Rien |
| 04, étapes 4 « Créer le projet de tests » et 5 « Un premier test » | `Chronos.Domain.Tests.csproj`, sa référence au domaine, `xunit.runner.json` complet | Le type de sortie, le `using` global, le paquet xUnit, la section `test` de `global.json` — à la main |
| 04, étape 10 « Brancher l'API sur le domaine » | La référence existe, par l'intermédiaire de `Chronos.Infrastructure` | Le code |
| 06, étape 1 « Créer le projet d'infrastructure » | Le projet et ses références | Les paquets EF Core |
| 07, étape 4 « Un projet par jeu de migrations » | Les deux projets et leurs références | Les migrations |
| 11, étape 1 « Créer le projet de tests » | `Chronos.Api.Tests.csproj`, sa référence à l'API, `xunit.runner.json` | Le SDK Web, le type de sortie, les paquets — à la main |
| 13, étape 2 « Créer l'application » | L'espace de travail Angular, un composant racine minimal | Rien |
| 13, étape 5 « Remplacer la page d'accueil » | `index.html` en français, titré « Chronos » | Le signal `titre`, le gabarit ; **ajouter** le test « affiche le nom de l'application » (le test généré que l'étape fait corriger n'existe pas ici) |
| 13, étape 10 « Fixer les versions de Node.js et de TypeScript » | Le bloc `devEngines` et la version de TypeScript dans `package.json` | Rien |
| 13 à 19, étapes qui lancent `ng generate` | Les fichiers `.ts`, `.html`, `.scss` | Le contenu, et le `.spec.ts` à la main |
| 16, étape 1 « Relayer `/api` vers l'API » | `src/proxy.conf.json`, avec son annotation et un objet vide | Y écrire le relais, le déclarer dans `angular.json` |

## Ce que les annotations ne peuvent pas porter

Trois fichiers sont en JSON strict : un commentaire les rendrait illisibles pour leurs outils. Voici ce que les modules y ajoutent.

**`src/api/Chronos.Api/appsettings.json` et `appsettings.Development.json`**

| Module | Ajout |
| --- | --- |
| 06, étape 10 « Lire le SQL exécuté » | Le niveau de journal de `Microsoft.EntityFrameworkCore.Database.Command`, dans les deux fichiers |
| 25, étape 11 « Données de démonstration : un réglage explicite » | `Chronos:Demo:Donnees`, dans `appsettings.Development.json` seulement |

**`src/web/angular.json`**

| Module | Ajout |
| --- | --- |
| 16, étape 1 | `proxyConfig` dans les options de la cible `serve` |
| 20, étape 9 | Fait par `ng add` : la cible `e2e` et la collection de schémas |
| 20, exercice 6 | Les seuils de couverture de la cible `test` |
| 21, étape 1 | `i18n.sourceLocale` |
| 21 | Les feuilles de style des polices, avant `src/styles.scss` ; les budgets de taille |
| 22, étape 8 | Fait par `ng add` : la cible `lint`. À la main : `e2e/**/*.ts` dans ses motifs de fichiers |

**`src/web/package.json`**

| Module | Ajout |
| --- | --- |
| 14 | `@angular/material`, `@angular/cdk` (par `ng add`) |
| 20 | Le script `e2e`, `@playwright/test`, `playwright-ng-schematics` (par `ng add`) ; `@vitest/coverage-v8` (exercice 6) |
| 21 | `@angular/aria`, les deux paquets `@fontsource`, `@axe-core/playwright` |
| 22 | Les scripts `lint`, `format`, `format:check` ; ESLint et `angular-eslint` (par `ng add`) |

## Ce que le squelette ne contient pas

- **Les fichiers produits par une commande du cours** : `packages.lock.json`, les dossiers `Migrations/`, `dotnet-tools.json`, `playwright.config.ts`, `e2e/tsconfig.json`. La commande est annotée à l'endroit le plus proche.
- **Les fichiers provisoires**, que le cours fait créer puis supprimer : `ProjetsEnMemoire.cs` et `ClientsEnMemoire.cs` (modules 03 à 06), `ValiderFeuilleRequete.cs` (10 à 12), le fichier de comparaison sans accents de l'exercice 2 du module 13 (13 à 16), `collaborateurs-en-dur.ts` (15 à 16), l'écran provisoire de feuille de temps (15 à 18) et celui de connexion (15 à 19), `collaborateur-courant.ts` (18 à 19). L'annotation du fichier voisin les mentionne, au module qui les crée.
- **Les workflows GitHub Actions** : un workflow vide ferait échouer GitHub dès le premier `push`. Leurs annotations sont dans [`.github/workflows/LISEZMOI.md`](.github/workflows/LISEZMOI.md), à supprimer quand les fichiers sont écrits. Même principe pour [`src/web/e2e/LISEZMOI.md`](src/web/e2e/LISEZMOI.md).
- **Les fichiers de test de l'interface**, voir plus haut.

## Ce qui a été vérifié

- L'état de départ : compilation de la solution sans avertissement, démarrage de l'API (aucune route : 404 partout), `npm ci`, `ng build`, `ng test`.
- La cohabitation avec chaque étape du parcours : le code de la solution aux tags `module-03` à `module-25`, posé sur le squelette, compile ; les coquilles des modules à venir ne gênent aucun état intermédiaire. Côté interface, les tests des tags `module-13`, `16`, `18`, `19` et `20` passent dans les mêmes conditions.
- La plomberie : complétée d'après ses seules annotations, puis chargée du code du tag `module-25`, la solution .NET compile sans avertissement, avertissements traités en erreurs.
- Chaque annotation cite un titre d'étape ou d'exercice qui existe mot pour mot dans le module.

Ce qui ne l'a pas été : personne n'a encore suivi le cours de bout en bout à partir de ce squelette, et tout a tourné dans des conteneurs Linux, pas directement sous Windows.
