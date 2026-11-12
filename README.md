# Chronos

Chronos est un **support de mentorat construit comme un cours**. Le mentoré y apprend à développer une application professionnelle en construisant, module après module, un outil de gestion du temps RH : charge des consultants et des employés, projets, temps passés, absences, informations RH.

## Pile technique

| Couche | Technologie |
| --- | --- |
| API | .NET 10 (LTS), ASP.NET Core, C# 14 |
| Accès aux données | EF Core 10 |
| Base de données | SQL Server 2025 et PostgreSQL 18 — le même code tourne sur les deux |
| Interface | Angular 22 |
| Environnement local | Docker et Docker Compose |
| Intégration continue | GitHub Actions |

## Par où commencer

| Vous êtes… | Lisez |
| --- | --- |
| **Mentoré** | Le [plan de cours](cours/README.md), puis le [cahier des charges](docs/cahier-des-charges.md). |
| **Mentor** | Le [plan de cours](cours/README.md) pour le déroulé, et les référentiels ci-dessous pour les choix techniques et leurs justifications. |

## Contenu du dépôt

| Emplacement | Contenu |
| --- | --- |
| [cours/](cours/README.md) | Le plan de cours et les modules. |
| [docs/cahier-des-charges.md](docs/cahier-des-charges.md) | Ce que l'application doit faire : acteurs, vocabulaire, règles de gestion numérotées. |
| [docs/references/dotnet-10.md](docs/references/dotnet-10.md) | Pratiques à jour pour .NET 10, EF Core 10 et les deux bases, avec leurs sources. |
| [docs/references/angular-22.md](docs/references/angular-22.md) | Pratiques à jour pour Angular 22, avec leurs sources. |
| [docs/references/docker.md](docs/references/docker.md) | Pratiques à jour pour Docker et Compose, avec leurs sources. |
| [docs/references/github-actions.md](docs/references/github-actions.md) | Pratiques à jour pour GitHub Actions, avec leurs sources. |
| [compose.yaml](compose.yaml) | Les deux bases de données en conteneur (module 01). Copier `.env.example` en `.env` avant de démarrer. |
| `src/` | La solution de référence. Elle sera construite au fil des modules. |

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

Le cahier des charges, les référentiels techniques, le plan de cours et les **26 modules** sont rédigés. Le code que les modules font écrire a été compilé et exécuté pendant leur rédaction, par groupes de modules, dans des conteneurs (.NET 10, Node.js 24, PostgreSQL 18, SQL Server 2025) ; ce qui ne l'a pas été — tout ce qui se passe sur GitHub après le module 05, notamment — est signalé en tête de chaque module et résumé dans le [suivi](cours/README.md#avancement-de-la-rédaction).

La **solution de référence reste à construire**. C'est elle qui validera l'enchaînement complet des modules sur un seul et même code.
