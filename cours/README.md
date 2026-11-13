# Plan de cours — Chronos

Ce cours accompagne un mentorat. Le mentoré y construit, module après module, une application complète de gestion du temps RH : une API en .NET 11, une base SQL Server ou PostgreSQL, une interface en Angular 22.

## À qui s'adresse ce cours

À un développeur **junior qui sait déjà programmer** : il connaît un langage objet, a des notions de web (HTTP, HTML) et de SQL, et sait utiliser git au quotidien. Il découvre .NET et Angular.

Le cours n'enseigne donc pas la programmation. Il enseigne **comment on construit une application professionnelle** avec ces deux frameworks, tels qu'ils se pratiquent fin 2026.

## Comment le cours fonctionne

Chaque module suit le même déroulé :

1. **Le mentoré lit le module** et les pages de documentation officielle qu'il cite.
2. **Il réalise le travail demandé** dans son propre dépôt, à partir du [projet de départ](#projet-de-départ), sans regarder la solution.
3. **Il compare** son résultat à la solution de référence du module.
4. **Séance de mentorat** : revue du code, discussion des questions prévues par le module, puis passage au module suivant.

Trois principes guident l'ensemble :

- **La documentation officielle d'abord.** Chaque notion renvoie à sa page de référence. Savoir lire la documentation de la bonne version est une compétence que le cours entraîne explicitement.
- **Le métier avant la technique.** Chaque module met en œuvre des règles de gestion précises du [cahier des charges](../docs/cahier-des-charges.md). On ne code pas une fonctionnalité technique « pour voir ».
- **Rien d'obsolète.** Les pratiques enseignées sont celles des [référentiels](../docs/references/) vérifiés. Quand un tutoriel trouvé en ligne contredit le cours, on vérifie dans la documentation officielle — et on corrige le cours s'il a tort.

## Projet de départ

Le mentoré ne part pas d'un dossier vide. Le dossier [`depart/`](../depart/README.md) contient le **corps du projet** : la solution .NET et ses projets, l'espace de travail Angular, et un fichier pour chaque fichier de l'application finale, vidé de son implémentation. Il en copie le contenu à la racine de son dépôt au module 00.

Chaque endroit à compléter porte une annotation `TODO [module NN]` qui dit quoi faire et renvoie à l'étape ou à l'exercice du module qui le demande. `git grep "TODO \[module 08\]"` liste le travail du module 08.

Conséquence pour la lecture des modules : ils décrivent la construction à partir de rien. Les commandes qui créent un projet ou un fichier que le squelette fournit déjà (`dotnet new`, `dotnet sln add`, `ng new`, `ng generate`) **ne se relancent pas**. Le [mode d'emploi du squelette](../depart/README.md) donne la liste des étapes déjà faites et ce qu'il reste à faire pour chacune.

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

Le code de référence vit dans ce dépôt. Il a été construit module par module, dans l'ordre du cours.

- `src/api/` — la solution .NET.
- `src/web/` — l'application Angular.
- À la fin de chaque module, un **tag git** `module-NN` marque l'état attendu. `git diff module-03 module-04` montre exactement ce qu'un module ajoute ; `git switch --detach module-08` place le dépôt dans l'état de la fin du module 08.

**Les tags et la branche principale n'ont pas la même version de .NET.** Les tags `module-NN` sont l'historique de la construction, faite en .NET 10 (SDK 10.0.404) : ils n'ont pas bougé. La branche principale, elle, est en .NET 11 (SDK 11.0.100), avec EF Core resté en 10. `git diff` entre deux tags reste la bonne façon de voir ce qu'un module ajoute — les lignes de version mises à part. En revanche, **l'état d'un tag ne compile pas tel quel avec le seul SDK 11** : son `global.json` demande un SDK 10. Pour l'exécuter, il faut soit installer un SDK 10 à côté (`winget install Microsoft.DotNet.SDK.10`), soit appliquer par-dessus la plomberie .NET 11 : `global.json` en `11.0.100`, `net11.0` dans `Directory.Build.props`, `Microsoft.AspNetCore.OpenApi` et `Microsoft.AspNetCore.Mvc.Testing` en `11.0.0` (quand ils sont présents ; les paquets EF Core restent en `10.0.15`, le fournisseur PostgreSQL en `10.0.3`), `dotnet restore --force-evaluate` pour régénérer les fichiers de verrouillage — et, à partir du module 11, le contournement du défaut du SDK 11.0.100 que ce module décrit ; à partir du module 23, les images `sdk:10.0` et `aspnet:10.0` du Dockerfile passent en `11.0`. C'est ce qui a été fait pour rejouer les commandes des modules en .NET 11 (état du tag mis à niveau, puis compilation et tests) ; l'en-tête de chaque module dit ce qui a été rejoué, et ce qui ne l'a pas été.

La solution contient le pas à pas de chaque module et ceux de ses exercices qui ajoutent quelque chose à l'application — les modules suivants les supposent faits. Elle n'est pas un corrigé à recopier : comparez-la à votre travail **après** l'avoir fait.

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
| 00 | [Environnement de travail](00-environnement-de-travail.md) | Installer et vérifier le SDK .NET 11, Node.js, Docker et son éditeur ; fixer les versions d'outils. | — |
| 01 | [Docker : les bases](01-docker-les-bases.md) | Expliquer ce que sont une image, un conteneur et un volume ; démarrer, inspecter et arrêter un conteneur ; décrire les deux bases du projet dans un fichier Compose. | — |
| 02 | [Comprendre le besoin](02-comprendre-le-besoin.md) | Lire un cahier des charges, en tirer un modèle de domaine et un découpage en lots. | toutes |

### Partie 1 — L'API (.NET 11)

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

## Avancement

| Élément | État |
| --- | --- |
| Cahier des charges | Rédigé ; précisions de RG-23, RG-24 et RG-25 consignées en section 8 |
| Référentiels .NET 11, Angular 22, Docker et GitHub Actions | Rédigés le 12 novembre 2026 (celui de .NET s'appelait alors « .NET 10 »), complétés par les constats de la construction ; **alignés sur .NET 11 le 13 novembre 2026** pour .NET, Docker et GitHub Actions (voir ci-dessous) |
| Plan de cours et les 26 modules | Rédigés le 12 novembre 2026, puis corrigés module par module pendant la construction ; **alignés sur .NET 11 le 13 novembre 2026** (voir ci-dessous) |
| Solution de référence | **Construite** les 12 et 13 novembre 2026 en .NET 10 : tags `module-00` à `module-25` (inchangés) ; **branche principale migrée en .NET 11 le 13 novembre 2026** |
| Projet de départ | **Construit** le 13 novembre 2026 dans `depart/`, à partir de la solution ; relu indépendamment, compilation rejouée avec le code de dix tags posé dessus. Personne n'a encore suivi le cours de bout en bout à partir de lui |

### Comment la solution a été construite

Chaque module a été rejoué sur un seul et même code, dans l'ordre du cours : le pas à pas, puis les exercices qui ajoutent à la solution. Une relecture indépendante a ensuite recompilé, relancé les tests et rejoué les critères de réussite avant que le tag ne soit posé. Quand le texte d'un module ne correspondait pas à ce qui s'exécutait, **c'est le module qui a été corrigé** : chaque module dit dans son en-tête ce qui a été rejoué, avec quelles versions.

État final, au tag `module-25` :

| | Mesure |
| --- | --- |
| API | 369 tests (225 unitaires, 144 d'intégration), rejoués sur PostgreSQL 18 et SQL Server 2025 ; compilation sans avertissement, avertissements traités en erreurs |
| Interface | 262 tests unitaires, 33 parcours Playwright dont un contrôle d'accessibilité sans violation sur chaque écran ; `ng build` sans avertissement |
| Livraison | Deux images construites, pile Compose démarrée ; installation **à partir d'une base neuve** jouée en `Production` à travers nginx, jusqu'à la feuille de temps d'un collaborateur |

### Alignement sur .NET 11 (13 novembre 2026)

Le 13 novembre 2026, trois jours après la sortie de .NET 11, le propriétaire du cours a décidé de passer à cette version. Ce n'est pas un remplacement de « 10 » par « 11 » : le choix est argumenté dans le [référentiel](../docs/references/dotnet-11.md) (version courante, même date de fin de support que la LTS, mais une STS, un SDK tout neuf et un écosystème qui n'a pas fini de suivre) et plusieurs explications changent de sens.

**Ce qui a été revérifié en .NET 11** (SDK 11.0.100, conteneurs Linux) :

- l'**état final de la solution**, en entier : compilation sans avertissement (Debug et Release, avertissements traités en erreurs), `dotnet format --verify-no-changes`, 225 tests unitaires et 144 tests d'intégration sur PostgreSQL 18 et sur SQL Server 2025, aucun changement de modèle en attente, bundle de migrations, images construites, pile Compose saine derrière nginx. Les chiffres de tests donnés plus haut restent ceux de l'état final ;
- les **commandes et les sorties** des modules 00, 03, 04 et 05 (`global.json`, `dotnet new`, `dotnet package add`, modèle `xunit3`, `dotnet test`, restauration verrouillée) ; les modules 06 à 12, 19 et 22 à 25 portent leur propre liste dans leur en-tête ;
- les **liens de documentation** `?view=aspnetcore-11.0` : chaque page a été ouverte dans cette version avant le changement (elle existe, et propose la version 11.0) ; le contenu cité n'a été relu que pour quelques-unes.

**Ce que l'alignement a changé de sens** : le piège des versions de paquets se déplace vers EF Core (EF Core reste en 10, car le fournisseur PostgreSQL d'EF Core 11 n'existe qu'en préversion ; un `dotnet package add` sans version prend une 11.0.0 incompatible, `NU1608`) ; le SDK 11.0.100 a un défaut connu qui touche le module 11 ; le modèle de projet `xunit3` n'offre pas `net11.0` ; le module 00 n'enseigne plus `global.json` par la cohabitation de deux SDK.

**Ce qui n'a pas été revérifié** : l'interface Angular (inchangée, non concernée, ses 262 tests et ses 33 parcours n'ont pas été relancés dans ce cadre) ; l'installation du SDK 11 et les commandes du module 00 sous Windows  ; le workflow de publication corrigé sur GitHub (le workflow `CI` a réussi sur la pull request de la migration) ; les passages que le texte d'un module marque « non rejoué en .NET 11 » dans son en-tête et dans les constats du référentiel ; les lignes du référentiel dont la colonne « En .NET 11 » dit « à revérifier ».

### Ce que la construction a changé dans le cours

Rédigés par groupes, les modules avaient chacun été validés sur un squelette d'essai. Les enchaîner a fait apparaître ce qu'aucun essai isolé ne pouvait montrer :

- **Des raccords manquants** entre modules : un exercice du module 03 que le module 06 ignorait, des tests que le module 20 faisait réécrire alors qu'ils existaient, des écrans réels que le module 21 ne connaissait pas.
- **Des défauts réels**, corrigés : le document OpenAPI en erreur depuis le module 08, une règle de gestion mal appliquée sur les demi-journées d'absence (module 10), une suppression de jour férié qui visait la mauvaise date (module 09), une borne de pagination qui faisait répondre 500 (module 08).
- **Quatre modules font évoluer l'API depuis l'interface ou le déploiement**, et le disent dans un encadré « Compléter le contrat de l'API » : le module 16 (indicateur de sortie d'un collaborateur), le module 17 (conflits distinguables, messages en français), le module 18 (feuilles de temps vides), le module 25 (données de référence, premier administrateur, gestion des comptes). C'est voulu : le premier client d'une API en révèle les manques.

### Ce qui n'est pas couvert

- **Sur GitHub, seule une partie a tourné** (relevé du 13 novembre 2026). Le workflow `CI` (jobs `API` et `Web`) a réussi sur les pull requests de la solution en .NET 10, sur `main` après fusion, puis sur la pull request de la migration vers .NET 11. `publication.yml` a tourné une fois sur `main` : images construites, mais le job du bundle de migrations a échoué (`NETSDK1004`), faute d'une étape de restauration ; le workflow corrigé n'a pas encore tourné. L'attestation de provenance et le déploiement par environnement (module 24) n'ont jamais été exécutés ; ils ne sont que relus contre la documentation.
- **Hors module 01, tout a tourné dans des conteneurs Linux** (SDK .NET 10 pour la construction, SDK .NET 11 pour l'alignement ; Node.js 24), pas directement sous Windows ; les installations du module 00 n'ont pas été jouées.
- **Pas de lecteur d'écran réel ni de Lighthouse** (module 21) ; l'AppHost Aspire est compilé, non lancé (module 25).
- **L'interface ne couvre pas tout le cahier des charges** : il n'y a pas d'écran pour les absences (l'API les gère entièrement, l'écran reste un titre), ni pour la gestion des comptes et des jours fériés (par l'API seulement). Un utilisateur ne peut pas changer son mot de passe. Ces limites sont dites dans les modules 21 et 25.
- **Les exercices d'exploration** (« provoquez », « comparez », « prédisez ») n'ont pas de trace dans la solution : ils sont signalés comme tels dans chaque module.

Avant de modifier un module, on relit la section correspondante du référentiel et on revérifie dans la documentation officielle les points qu'il signale « à surveiller ».
