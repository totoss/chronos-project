# Chronos

Chronos est un **support de mentorat construit comme un cours**. Le mentoré y apprend à développer une application professionnelle en construisant, module après module, un outil de gestion du temps RH : charge des consultants et des employés, projets, temps passés, absences, informations RH.

## Pile technique

| Couche | Technologie |
| --- | --- |
| API | .NET 11 (STS), ASP.NET Core, C# 15 |
| Accès aux données | EF Core 10 (LTS ; elle cible .NET 10 et fonctionne sur .NET 11 par compatibilité ascendante, ce que les tests constatent mais que sa documentation ne garantit pas) — le fournisseur PostgreSQL d'EF Core 11 n'existe qu'en préversion, donc EF Core reste en 10 |
| Base de données | SQL Server 2025 et PostgreSQL 18 — le même code tourne sur les deux |
| Interface | Angular 22 |
| Environnement local | Docker et Docker Compose |
| Intégration continue | GitHub Actions |

## Par où commencer

| Vous êtes… | Lisez |
| --- | --- |
| **Mentoré** | Le [plan de cours](cours/README.md), puis le [cahier des charges](docs/cahier-des-charges.md). Le code se construit à partir du [projet de départ](depart/README.md). |
| **Mentor** | Le [plan de cours](cours/README.md) pour le déroulé, et les référentiels ci-dessous pour les choix techniques et leurs justifications. |

## Contenu du dépôt

| Emplacement | Contenu |
| --- | --- |
| [cours/](cours/README.md) | Le plan de cours et les modules. |
| [docs/cahier-des-charges.md](docs/cahier-des-charges.md) | Ce que l'application doit faire : acteurs, vocabulaire, règles de gestion numérotées. |
| [docs/references/dotnet-11.md](docs/references/dotnet-11.md) | Pratiques à jour pour .NET 11, EF Core 10 et les deux bases, avec leurs sources, et le choix de .NET 11 argumenté. |
| [docs/references/angular-22.md](docs/references/angular-22.md) | Pratiques à jour pour Angular 22, avec leurs sources. |
| [docs/references/docker.md](docs/references/docker.md) | Pratiques à jour pour Docker et Compose, avec leurs sources. |
| [docs/references/github-actions.md](docs/references/github-actions.md) | Pratiques à jour pour GitHub Actions, avec leurs sources. |
| [compose.yaml](compose.yaml) | Les deux bases de données en conteneur (module 01) et, sous le profil `app`, l'application complète (modules 23 et 25). Copier `.env.example` en `.env` avant de démarrer. |
| [depart/](depart/README.md) | Le projet de départ du mentoré : la structure et l'architecture, vidées de leur implémentation, avec une annotation `TODO [module NN]` à chaque endroit à compléter. |
| `src/api/`, `src/web/` | La solution de référence : l'API .NET 11 et l'interface Angular 22. Un tag `module-NN` marque l'état attendu à la fin de chaque module ; les tags datent de la construction en .NET 10 (voir [la solution de référence](cours/README.md#solution-de-référence)). |

## Qualité

L'intégration continue refuse tout code mal formaté ou qui enfreint une règle. Côté API, `dotnet format --verify-no-changes` contrôle le formatage et le style, et `dotnet build` échoue au moindre avertissement (analyseurs `latest-recommended`, `TreatWarningsAsErrors`) ; seuls les avertissements d'audit des paquets (`NU1901` à `NU1904`) restent des avertissements. Côté interface, `npm run format:check` contrôle Prettier et `npx ng lint` applique ESLint (`src` et `e2e`).

Pour lancer les mêmes contrôles sur son poste :

- dans `src/api` : `dotnet format --verify-no-changes`, puis `dotnet build --no-incremental` ; `dotnet format` seul corrige le formatage et les conventions de style, à relire avec `git diff` (jamais dans un commit mêlé à une autre modification) ;
- dans `src/web` : `npm run format:check` (`npm run format` pour corriger), puis `npx ng lint`.

Une suppression n'est acceptée que si elle est la plus étroite possible (une ligne, puis un dossier, et un projet entier seulement quand la règle n'a pas d'objet pour lui), si la raison est écrite à côté, et si elle est encore vraie à la relecture. Attention : `dotnet format` sans sous-commande applique aussi les correctifs automatiques des analyseurs (c'est ce qui a réécrit le `DbContext` lors du module 22) : relire `git diff` après chaque passage. Le code généré (dossiers `Migrations/`) est déclaré comme tel avec `generated_code = true` plutôt que corrigé à la main.

## Un cours sans pratiques obsolètes

Les frameworks évoluent vite, et une grande partie des tutoriels disponibles en ligne enseignent des pratiques dépassées. Ce dépôt applique donc une règle simple : **toute indication technique est vérifiée dans la documentation officielle du moment, et sa source est citée.**

Les référentiels consignent, à une date donnée, les pratiques retenues, celles à éviter et les points restant à trancher. Ils sont revérifiés avant la rédaction de chaque module.

## État d'avancement

Le cours est complet : cahier des charges, référentiels techniques, plan de cours, **26 modules**, et la **solution de référence**, construite module par module et marquée par les tags `module-00` à `module-25`.

Chaque module a été rejoué sur ce code, puis relu indépendamment ; quand le texte et l'exécution ne s'accordaient pas, le module a été corrigé. Au dernier tag : 369 tests côté API (sur PostgreSQL 18 et SQL Server 2025), 262 tests et 33 parcours de bout en bout côté interface, et une installation à partir d'une base neuve jouée de bout en bout dans des conteneurs.

**Alignement sur .NET 11 (13 novembre 2026).** Les tags datent de la construction en .NET 10 ; le code de la branche principale est en .NET 11 (décision du 13 novembre 2026), avec EF Core resté en 10. L'état final de la solution a été revérifié en entier avec le SDK 11.0.100 ; le texte des modules et les référentiels ont été alignés, et chaque module dit dans son en-tête ce qui a été rejoué en .NET 11 et ce qui ne l'a pas été. Le détail est dans le [suivi](cours/README.md#avancement).

Ce qui n'a pas été vérifié est dit sans détour dans le [suivi](cours/README.md#avancement) : le workflow de publication, corrigé depuis son premier échec, a réussi sur GitHub (images publiées et attestées, bundle de migrations), mais son déploiement par environnement n'a jamais été exécuté ; hors GitHub, tout a tourné dans des conteneurs Linux et non directement sous Windows, et l'interface ne couvre pas tout le cahier des charges (pas d'écran pour les absences ni pour la gestion des comptes).
