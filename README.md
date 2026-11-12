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

## Un cours sans pratiques obsolètes

Les frameworks évoluent vite, et une grande partie des tutoriels disponibles en ligne enseignent des pratiques dépassées. Ce dépôt applique donc une règle simple : **toute indication technique est vérifiée dans la documentation officielle du moment, et sa source est citée.**

Les référentiels consignent, à une date donnée, les pratiques retenues, celles à éviter et les points restant à trancher. Ils sont revérifiés avant la rédaction de chaque module.

## État d'avancement

Le cadrage est terminé : cahier des charges, référentiels techniques et plan de cours sont rédigés. Deux modules le sont aussi : [Docker : les bases](cours/01-docker-les-bases.md) et [GitHub Actions : l'intégration continue](cours/05-github-actions-integration-continue.md). Leurs commandes et leurs workflows ont été vérifiés dans la documentation, mais pas encore exécutés.

Les autres modules et la solution de référence restent à écrire — voir le [suivi](cours/README.md#avancement-de-la-rédaction).
