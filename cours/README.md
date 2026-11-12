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

Le code de référence vit dans ce dépôt et avance avec les modules.

- `src/api/` — la solution .NET.
- `src/web/` — l'application Angular.
- À la fin de chaque module, un **tag git** `module-NN` marque l'état attendu. `git diff module-03 module-04` montre exactement ce qu'un module ajoute.

Conventions de code :

- Le **vocabulaire métier est en français**, tel qu'il figure dans le cahier des charges : `Collaborateur`, `Affectation`, `FeuilleDeTemps`.
- Le **vocabulaire technique reste en anglais** : `Repository`, `Service`, `Dto`, `Handler`.

## Parcours

Le cours traite le back-end en premier, puis le front-end. Mener les deux de front obligerait un junior à apprendre deux frameworks à la fois ; ici, quand il aborde Angular, l'API existe déjà et il peut se concentrer sur l'interface.

### Partie 0 — Démarrer

| N° | Module | Le mentoré saura… | Règles |
| --- | --- | --- | --- |
| 00 | Environnement de travail | Installer et vérifier le SDK .NET 10, Node.js, Docker et son éditeur ; fixer les versions d'outils. | — |
| 01 | [Docker : les bases](01-docker-les-bases.md) | Expliquer ce que sont une image, un conteneur et un volume ; démarrer, inspecter et arrêter un conteneur ; décrire les deux bases du projet dans un fichier Compose. | — |
| 02 | Comprendre le besoin | Lire un cahier des charges, en tirer un modèle de domaine et un découpage en lots. | toutes |

### Partie 1 — L'API (.NET 10)

| N° | Module | Le mentoré saura… | Règles |
| --- | --- | --- | --- |
| 03 | Première API | Créer une solution et un projet d'API, écrire des endpoints en Minimal API, les grouper, typer leurs réponses, explorer l'API avec OpenAPI et Scalar. | — |
| 04 | Modéliser le domaine en C# | Écrire des entités qui protègent leurs invariants ; utiliser les types adaptés (`DateOnly`, records, énumérations, références non nulles) ; écrire ses premiers tests unitaires avec xUnit v3. | RG-01 à 05, RG-10 à 13 |
| 05 | [GitHub Actions : l'intégration continue](05-github-actions-integration-continue.md) | Écrire un workflow qui compile et teste l'API à chaque *push* et à chaque *pull request* ; lire un échec ; sécuriser un workflow ; rendre la vérification obligatoire avant fusion. | — |
| 06 | Persister avec EF Core | Configurer un `DbContext`, créer et appliquer des migrations, alimenter la base, écrire des requêtes LINQ efficaces avec projection. | RG-01, RG-05, RG-10 |
| 07 | Deux SGBD, un seul code | Faire tourner la même application sur SQL Server et PostgreSQL ; expliquer ce qui diffère (dates, concurrence, clés, migrations). | — |
| 08 | Une API robuste | Valider les entrées, renvoyer des erreurs normalisées (`ProblemDetails`), paginer, filtrer et trier, séparer entités et DTO, gérer la concurrence optimiste. | RG-02, RG-03, RG-11, RG-12 |
| 09 | Affectations et calcul de charge | Implémenter un calcul métier non trivial, le rendre testable (`TimeProvider`), le couvrir de tests à partir d'exemples chiffrés. | RG-20 à 25 |
| 10 | Temps et absences | Modéliser un cycle de validation par états et transitions ; garantir la cohérence par les transactions. | RG-30 à 43 |
| 11 | Tests d'intégration | Tester l'API de bout en bout contre une vraie base avec `WebApplicationFactory` et Testcontainers ; faire tourner ces tests dans le workflow d'intégration continue. | — |
| 12 | Sécurité et traçabilité | Authentifier les utilisateurs, autoriser par rôle et par périmètre, tracer les modifications, appliquer la minimisation des données. | RG-36, RG-50 à 53 |

### Partie 2 — L'interface (Angular 22)

| N° | Module | Le mentoré saura… | Règles |
| --- | --- | --- | --- |
| 13 | Première application Angular | Créer l'application, comprendre sa structure, écrire un composant, un template avec flux de contrôle, et manipuler des signals. | — |
| 14 | Composer des composants | Découper un écran en composants ; faire communiquer parent et enfant (`input`, `output`, `model`) ; dériver de l'état avec `computed` ; utiliser Angular Material. | — |
| 15 | Routage | Définir les routes, charger les écrans à la demande, lier les paramètres d'URL aux composants, construire la navigation. | — |
| 16 | Dialoguer avec l'API | Lire des données avec `httpResource`, écrire avec `HttpClient`, intercepter les requêtes, afficher les états de chargement et d'erreur. | — |
| 17 | Formulaires | Construire un formulaire avec Signal Forms, valider côté client, afficher les erreurs renvoyées par l'API. | RG-02, RG-03, RG-11, RG-12 |
| 18 | Écrans métier | Construire la feuille de temps hebdomadaire et le plan de charge ; partager de l'état entre écrans avec un service à signals. | RG-24, RG-25, RG-30 à 35 |
| 19 | Authentification côté interface | Connecter l'utilisateur, protéger les routes par des gardes, adapter l'interface au rôle. | RG-50 à 52 |
| 20 | Tester l'interface | Tester composants et services avec Vitest et les harnais ; écrire un parcours de bout en bout ; ajouter l'interface au workflow d'intégration continue. | — |
| 21 | Accessibilité et finitions | Rendre l'interface utilisable au clavier et au lecteur d'écran ; afficher dates et nombres au format français. | — |

### Partie 3 — Livrer

| N° | Module | Le mentoré saura… | Règles |
| --- | --- | --- | --- |
| 22 | Qualité du code | Mettre en place analyseurs, formatage et lint, et les faire respecter par l'intégration continue. | — |
| 23 | Conteneuriser l'application | Écrire le Dockerfile de l'API et celui de l'interface ; démarrer l'application complète avec Compose. | — |
| 24 | GitHub Actions : construire et publier | Construire les images à chaque version, les publier dans un registre, attester leur provenance, déployer par environnement avec approbation. | — |
| 25 | Exploiter l'application | Journaliser utilement, exposer traces et métriques, déployer les migrations ; découvrir Aspire. | RG-53 |

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
| Module 01 — Docker : les bases | Rédigé et exécuté le 12 novembre 2026 |
| Module 05 — GitHub Actions : l'intégration continue | Rédigé ; workflow et verrouillage des actions exécutés le 12 novembre 2026 ; Dependabot et *ruleset* non exécutés |
| Autres modules | À rédiger |
| Solution de référence | À construire, module par module |

Avant de rédiger un module, on relit la section correspondante du référentiel et on revérifie dans la documentation officielle les points qu'il signale « à surveiller ».
