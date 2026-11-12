# Plan de cours — Chronos

Ce cours accompagne un mentorat. Le mentoré y construit, module après module, une application complète de gestion du temps RH : une API en .NET 10, une base SQL Server ou PostgreSQL, une interface en Angular 22.

## À qui s'adresse ce cours

À un développeur **junior qui sait déjà programmer** : il connaît un langage objet, a des notions de web (HTTP, HTML) et de SQL, et sait utiliser git au quotidien. Il découvre .NET et Angular.

Le cours n'enseigne donc pas la programmation. Il enseigne **comment on construit une application professionnelle** avec ces deux frameworks, tels qu'ils se pratiquent fin 2026.

## Comment le cours fonctionne

Chaque module suit le même déroulé :

1. **Le mentoré lit le module** et les pages de documentation officielle qu'il cite.
2. **Il réalise le travail demandé** dans son propre dépôt, sans regarder la solution.
3. **Il compare** son résultat à la solution de référence du module.
4. **Séance de mentorat** : revue du code, discussion des questions prévues par le module, puis passage au module suivant.

Trois principes guident l'ensemble :

- **La documentation officielle d'abord.** Chaque notion renvoie à sa page de référence. Savoir lire la documentation de la bonne version est une compétence que le cours entraîne explicitement.
- **Le métier avant la technique.** Chaque module met en œuvre des règles de gestion précises du [cahier des charges](../docs/cahier-des-charges.md). On ne code pas une fonctionnalité technique « pour voir ».
- **Rien d'obsolète.** Les pratiques enseignées sont celles des [référentiels](../docs/references/) vérifiés. Quand un tutoriel trouvé en ligne contredit le cours, on vérifie dans la documentation officielle — et on corrige le cours s'il a tort.

## Structure d'un module

Chaque module est un fichier `cours/NN-titre.md` construit sur le même plan :

| Rubrique | Contenu |
| --- | --- |
| **Objectifs** | Ce que le mentoré saura faire à la fin, formulé en actions vérifiables. |
| **Prérequis** | Modules précédents et notions supposées acquises. |
| **Règles de gestion** | Les `RG-xx` du cahier des charges mises en œuvre. |
| **Notions** | Les concepts, expliqués sur le cas Chronos, avec le lien vers la documentation officielle. |
| **Pas à pas** | La construction guidée de la partie commune. |
| **À vous** | Les exercices réalisés seul, du plus guidé au plus ouvert. |
| **Critères de réussite** | Ce qui doit fonctionner et les tests qui doivent passer. |
| **Pièges** | Les erreurs fréquentes et les pratiques obsolètes rencontrées sur ce sujet. |
| **Pour la séance** | Les questions à discuter avec le mentor ; pas de réponse écrite dans le dépôt. |
| **Pour aller plus loin** | Lectures et prolongements facultatifs. |

## Solution de référence

Le code de référence vivra dans ce dépôt et avancera avec les modules. **Il n'est pas encore construit** : aujourd'hui, les modules décrivent la solution, et chacun dit dans son en-tête ce qui a été exécuté pour le rédiger.

- `src/api/` — la solution .NET.
- `src/web/` — l'application Angular.
- À la fin de chaque module, un **tag git** `module-NN` marquera l'état attendu. `git diff module-03 module-04` montrera exactement ce qu'un module ajoute.

Ce que les modules font construire :

```
src/api/
  Chronos.slnx, global.json, Directory.Build.props, Directory.Packages.props
  Chronos.Api/                     Minimal API, un dossier par ressource (module 03)
  Chronos.Domain/                  entités et règles de gestion, sans dépendance (module 04)
  Chronos.Domain.Tests/            tests unitaires xUnit v3 (module 04)
  Chronos.Infrastructure/          EF Core : ChronosDbContext, configurations (module 06)
  Chronos.Migrations.PostgreSql/   un jeu de migrations par SGBD (module 07)
  Chronos.Migrations.SqlServer/
  Chronos.Api.Tests/               tests d'intégration, Testcontainers (module 11)
src/web/                           application Angular « chronos-web », un dossier par fonctionnalité (module 13)
compose.yaml                       les deux bases (module 01), puis l'application complète (module 23)
.github/workflows/                 intégration continue (module 05), publication (module 24)
```

Conventions de code :

- Le **vocabulaire métier est en français**, tel qu'il figure dans le cahier des charges : `Collaborateur`, `Affectation`, `FeuilleDeTemps`.
- Le **vocabulaire technique reste en anglais** : `Dto`, `Endpoints`, `Handler`, `Interceptor`.
- **Pas de couche de plus que nécessaire** : les endpoints utilisent directement le `DbContext` et les services du domaine ; pas de *repository* générique, pas de bibliothèque de médiation ni de mapping. Ce choix se discute en séance (module 06).

## Parcours

Le cours traite le back-end en premier, puis le front-end. Mener les deux de front obligerait un junior à apprendre deux frameworks à la fois ; ici, quand il aborde Angular, l'API existe déjà et il peut se concentrer sur l'interface.

### Partie 0 — Démarrer

| N° | Module | Le mentoré saura… | Règles |
| --- | --- | --- | --- |
| 00 | [Environnement de travail](00-environnement-de-travail.md) | Installer et vérifier le SDK .NET 10, Node.js, Docker et son éditeur ; fixer les versions d'outils. | — |
| 01 | [Docker : les bases](01-docker-les-bases.md) | Expliquer ce que sont une image, un conteneur et un volume ; démarrer, inspecter et arrêter un conteneur ; décrire les deux bases du projet dans un fichier Compose. | — |
| 02 | [Comprendre le besoin](02-comprendre-le-besoin.md) | Lire un cahier des charges, en tirer un modèle de domaine et un découpage en lots. | toutes |

### Partie 1 — L'API (.NET 10)

| N° | Module | Le mentoré saura… | Règles |
| --- | --- | --- | --- |
| 03 | [Première API](03-premiere-api.md) | Créer une solution et un projet d'API, écrire des endpoints en Minimal API, les grouper, typer leurs réponses, explorer l'API avec OpenAPI et Scalar. | — |
| 04 | [Modéliser le domaine en C#](04-modeliser-le-domaine.md) | Écrire des entités qui protègent leurs invariants ; utiliser les types adaptés (`DateOnly`, records, énumérations, références non nulles) ; écrire ses premiers tests unitaires avec xUnit v3. | RG-01 à 05, RG-10 à 13 |
| 05 | [GitHub Actions : l'intégration continue](05-github-actions-integration-continue.md) | Écrire un workflow qui compile et teste l'API à chaque *push* et à chaque *pull request* ; lire un échec ; sécuriser un workflow ; rendre la vérification obligatoire avant fusion. | — |
| 06 | [Persister avec EF Core](06-persister-avec-ef-core.md) | Configurer un `DbContext`, créer et appliquer des migrations, alimenter la base, écrire des requêtes LINQ efficaces avec projection. | RG-01, RG-05, RG-10 |
| 07 | [Deux SGBD, un seul code](07-deux-sgbd-un-seul-code.md) | Faire tourner la même application sur SQL Server et PostgreSQL ; expliquer ce qui diffère (dates, concurrence, clés, migrations). | — |
| 08 | [Une API robuste](08-une-api-robuste.md) | Valider les entrées, renvoyer des erreurs normalisées (`ProblemDetails`), paginer, filtrer et trier, séparer entités et DTO, gérer la concurrence optimiste. | RG-01 à 04, RG-10 à 12 |
| 09 | [Affectations et calcul de charge](09-affectations-et-calcul-de-charge.md) | Implémenter un calcul métier non trivial, le rendre testable (`TimeProvider`), le couvrir de tests à partir d'exemples chiffrés. | RG-13, RG-20 à 25 |
| 10 | [Temps et absences](10-temps-et-absences.md) | Modéliser un cycle de validation par états et transitions ; garantir la cohérence par les transactions. | RG-30 à 43 |
| 11 | [Tests d'intégration](11-tests-d-integration.md) | Tester l'API de bout en bout contre une vraie base avec `WebApplicationFactory` et Testcontainers ; faire tourner ces tests dans le workflow d'intégration continue. | — |
| 12 | [Sécurité et traçabilité](12-securite-et-tracabilite.md) | Authentifier les utilisateurs, autoriser par rôle et par périmètre, tracer les modifications, appliquer la minimisation des données. | RG-36, RG-50 à 53 |

### Partie 2 — L'interface (Angular 22)

| N° | Module | Le mentoré saura… | Règles |
| --- | --- | --- | --- |
| 13 | [Première application Angular](13-premiere-application-angular.md) | Créer l'application, comprendre sa structure, écrire un composant, un template avec flux de contrôle, et manipuler des signals. | — |
| 14 | [Composer des composants](14-composer-des-composants.md) | Découper un écran en composants ; faire communiquer parent et enfant (`input`, `output`, `model`) ; dériver de l'état avec `computed` ; utiliser Angular Material. | — |
| 15 | [Routage](15-routage.md) | Définir les routes, charger les écrans à la demande, lier les paramètres d'URL aux composants, construire la navigation. | — |
| 16 | [Dialoguer avec l'API](16-dialoguer-avec-l-api.md) | Lire des données avec `httpResource`, écrire avec `HttpClient`, intercepter les requêtes, afficher les états de chargement et d'erreur. | — |
| 17 | [Formulaires](17-formulaires.md) | Construire un formulaire avec Signal Forms, valider côté client, afficher les erreurs renvoyées par l'API. | RG-02, RG-03, RG-11, RG-12 |
| 18 | [Écrans métier](18-ecrans-metier.md) | Construire la feuille de temps hebdomadaire et le plan de charge ; partager de l'état entre écrans avec un service à signals. | RG-24, RG-25, RG-30 à 35 |
| 19 | [Authentification côté interface](19-authentification-cote-interface.md) | Connecter l'utilisateur, protéger les routes par des gardes, adapter l'interface au rôle. | RG-50 à 52 |
| 20 | [Tester l'interface](20-tester-l-interface.md) | Tester composants et services avec Vitest et les harnais ; écrire un parcours de bout en bout ; ajouter l'interface au workflow d'intégration continue. | — |
| 21 | [Accessibilité et finitions](21-accessibilite-et-finitions.md) | Rendre l'interface utilisable au clavier et au lecteur d'écran ; afficher dates et nombres au format français. | — |

### Partie 3 — Livrer

| N° | Module | Le mentoré saura… | Règles |
| --- | --- | --- | --- |
| 22 | [Qualité du code](22-qualite-du-code.md) | Mettre en place analyseurs, formatage et lint, et les faire respecter par l'intégration continue. | — |
| 23 | [Conteneuriser l'application](23-conteneuriser-l-application.md) | Écrire le Dockerfile de l'API et celui de l'interface ; démarrer l'application complète avec Compose. | — |
| 24 | [GitHub Actions : construire et publier](24-github-actions-construire-et-publier.md) | Construire les images à chaque version, les publier dans un registre, attester leur provenance, déployer par environnement avec approbation. | — |
| 25 | [Exploiter l'application](25-exploiter-l-application.md) | Journaliser utilement, exposer traces et métriques, déployer les migrations ; découvrir Aspire. | RG-53 |

### Docker et GitHub Actions, fils conducteurs du cours

Ces deux outils ne sont pas relégués à la fin : ils sont introduits dès qu'ils deviennent utiles, puis repris à mesure que le projet grandit.

| Outil | Introduction | Reprises |
| --- | --- | --- |
| **Docker** | Module 01 — les bases et les deux bases de données en conteneur. | Module 11 — Testcontainers. Module 23 — conteneuriser l'API et l'interface. |
| **GitHub Actions** | Module 05 — dès que les premiers tests existent, ils tournent à chaque *push*. | Module 11 — tests contre une vraie base. Module 20 — l'interface. Module 22 — qualité. Module 24 — publication des images. |

## Avancement de la rédaction

| Élément | État |
| --- | --- |
| Cahier des charges | Rédigé |
| Référentiels .NET 10, Angular 22, Docker et GitHub Actions | Rédigés et vérifiés le 12 novembre 2026 |
| Plan de cours | Rédigé |
| Les 26 modules | Rédigés le 12 novembre 2026 |
| Solution de référence | À construire, module par module |

### Ce qui a été exécuté pour rédiger chaque module

Chaque module porte en en-tête son état d'exécution exact. En résumé :

| Modules | Exécuté | Non exécuté |
| --- | --- | --- |
| 00 | Commandes de vérification, comportement de `global.json` et de `devEngines`, en conteneur. | Les installations sur le poste (winget, fnm, éditeurs). |
| 01 | Tout, sous Windows 11 avec Docker Desktop. | — |
| 02 | Aucun code. Les diagrammes Mermaid passent l'analyseur. | Leur rendu dans GitHub. |
| 03, 04 | Tout le pas à pas, rejoué de zéro (SDK 10.0.404). | L'envoi depuis un éditeur (fichier `.http`). |
| 05 | Le workflow et le verrouillage des actions, sur GitHub. | Dependabot, le *ruleset*. |
| 06, 07 | Tout le pas à pas, sur PostgreSQL 18.6 et SQL Server 2025. | Les étapes de workflow proposées en exercice. |
| 08 à 10 | Validation, erreurs, concurrence, calcul de charge (58 tests), transitions, transactions, sur les deux SGBD. | Les migrations de ces trois modules (schéma créé directement pour l'essai). |
| 11, 12 | Tests d'intégration avec Testcontainers, authentification, périmètre, traçabilité (27 tests). | Le workflow sur GitHub ; les extraits réalignés ensuite sur l'organisation des migrations du module 07. |
| 13 à 15 | Tout le code, compilé et testé (Angular 22.2.6). | — |
| 16 à 18 | Tout le code, compilé et testé (55 tests) ; écrans parcourus dans un navigateur. | Le branchement sur la véritable API : ses réponses sont simulées d'après les modules 08 à 12. |
| 19 à 21 | Authentification, tests Vitest et Playwright, accessibilité (contrôle axe), compilés et testés sur des écrans minimaux. | Les extraits réécrits ensuite pour reprendre les composants réels du module 18 ; la véritable API ; le workflow sur GitHub ; Lighthouse ; un lecteur d'écran. |
| 22 | Analyseurs, `dotnet format`, ESLint, Prettier, sur des projets d'essai. | Les étapes de workflow sur GitHub. |
| 23 | Les deux images, Compose, relais `/api`, en-têtes transmis, HTTPS. | La connexion réelle (pas d'Identity dans l'essai). |
| 24 | Rien sur GitHub : YAML relu contre la documentation et passé à `actionlint`. | Publication, attestation, environnements, Dependabot. |
| 25 | Journaux, contrôles de santé, export OpenTelemetry vers le tableau de bord Aspire, bundles et scripts de migrations. | Le lancement de l'AppHost Aspire (compilé seulement). |

Trois limites valent pour l'ensemble :

- Hors module 01 et module 05, tout a été exécuté dans des **conteneurs Linux** (SDK .NET 10, Node.js 24), pas directement sous Windows.
- Chaque groupe de modules a été validé sur **son propre squelette d'essai**, pas sur une solution unique construite de bout en bout. Les exercices « À vous » n'ont pas de corrigé exécuté.
- C'est la **construction de la solution de référence**, module après module, qui fera la validation d'ensemble ; tout écart constaté à ce moment-là corrige le module.

Avant de modifier un module, on relit la section correspondante du référentiel et on revérifie dans la documentation officielle les points qu'il signale « à surveiller ».
