# Référentiel Docker — pratiques à jour

> **Vérifié le 12 novembre 2026** sur docs.docker.com, les fiches des images officielles, learn.microsoft.com, le dépôt `dotnet/dotnet-docker`, angular.dev et nginx.org.
> Ce document fait foi pour le cours : en cas de doute entre un tutoriel trouvé en ligne et ce référentiel, c'est le référentiel — ou mieux, la documentation officielle du jour — qui a raison.
> Les numéros de version de Docker changent presque chaque semaine : seules les versions majeures comptent ici.

## 1. Versions

| Élément | Valeur | Source |
| --- | --- | --- |
| Docker Desktop | 4.99.0 | [notes de version](https://docs.docker.com/desktop/release-notes/) |
| Docker Engine | 29.9.3 | [notes de version](https://docs.docker.com/engine/release-notes/) |
| Docker Compose | v5.7 | [versions](https://github.com/docker/compose/releases) |
| Image PostgreSQL | `postgres:18` (18.6) | [fiche de l'image](https://hub.docker.com/_/postgres) |
| Image SQL Server | `mcr.microsoft.com/mssql/server:2025-latest` | [démarrage rapide](https://learn.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker?view=sql-server-ver17) |
| Images .NET | `mcr.microsoft.com/dotnet/sdk:10.0`, `mcr.microsoft.com/dotnet/aspnet:10.0` | [dotnet-docker](https://github.com/dotnet/dotnet-docker) |
| Image Node.js | `node:24` (LTS) | [versions de Node.js](https://nodejs.org/en/about/previous-releases) |
| Image nginx | `nginx:1.30` (stable) | [fiche de l'image](https://hub.docker.com/_/nginx) |

Compose est passé directement de la v2 à la **v5** en 2025. Le changement est de pure numérotation : il évite la confusion avec les anciens formats de fichier « v2 » et « v3 » ([historique](https://docs.docker.com/compose/intro/history/)).

### Docker Desktop sous Windows

- **Prérequis** : Windows 11 64 bits (23H2 ou plus récent), WSL 2, 8 Go de mémoire, virtualisation activée dans le BIOS/UEFI ([installation](https://docs.docker.com/desktop/setup/install/windows-install/)).
- **WSL 2 est le moteur par défaut.** Hyper-V n'est pas requis. On tient WSL à jour avec `wsl --update` ([WSL](https://docs.docker.com/desktop/features/wsl/)).
- **Licence.** Docker Desktop est gratuit pour un usage personnel, l'enseignement, l'open source non commercial et les petites entreprises : moins de 250 salariés **et** moins de 10 millions de dollars de chiffre d'affaires annuel. Au-delà de l'un de ces seuils, un abonnement payant est nécessaire ([licence](https://docs.docker.com/subscription/desktop-license/)). Un mentoré en entreprise vérifie donc sa situation avant d'installer.
- **Alternatives libres** : [Podman Desktop](https://podman-desktop.io/) et [Rancher Desktop](https://rancherdesktop.io/), tous deux sous licence Apache 2.0. Le cours est écrit pour Docker Desktop.

## 2. Ce que le cours enseigne

### Compose

| Sujet | Pratique retenue | Source |
| --- | --- | --- |
| Commande | `docker compose` (avec une espace). | [produits retirés](https://docs.docker.com/retired/) |
| Fichier | `compose.yaml`, à la racine du dépôt. | [modèle d'application](https://docs.docker.com/compose/intro/compose-application-model/) |
| En-tête | Pas de clé `version`. Une clé `name` pour nommer le projet. | [version et nom](https://docs.docker.com/reference/compose-file/version-and-name/) |
| Ports | Liés à la machine locale uniquement : `"127.0.0.1:5432:5432"`. | [services](https://docs.docker.com/reference/compose-file/services/) |
| Données | Volumes nommés, déclarés à la racine du fichier. | [volumes](https://docs.docker.com/reference/compose-file/volumes/) |
| Disponibilité | `healthcheck` sur chaque base ; `depends_on` avec `condition: service_healthy`. | [ordre de démarrage](https://docs.docker.com/compose/how-tos/startup-order/) |
| Démarrage | `docker compose up -d --wait` : rend la main quand les services sont sains. | [`up`](https://docs.docker.com/reference/cli/docker/compose/up/) |
| Mots de passe | Dans un fichier `.env` non versionné, lu par interpolation (`${VAR:?message}`). | [interpolation](https://docs.docker.com/compose/how-tos/environment-variables/variable-interpolation/) |
| Communication entre services | Par le nom du service, sur le réseau créé par Compose. Pas de `links`. | [services](https://docs.docker.com/reference/compose-file/services/) |

### Images et Dockerfile

| Sujet | Pratique retenue | Source |
| --- | --- | --- |
| Tags | Toujours une version explicite. Jamais `latest`. | [bonnes pratiques](https://docs.docker.com/build/building/best-practices/) |
| Construction | En plusieurs étapes : une image pour compiler, une image minimale pour exécuter. | [bonnes pratiques](https://docs.docker.com/build/building/best-practices/) |
| Syntaxe | `# syntax=docker/dockerfile:1` en première ligne. | [frontend](https://docs.docker.com/build/buildkit/frontend/) |
| Cache des dépendances | `RUN --mount=type=cache,target=...` pour NuGet et npm. | [cache](https://docs.docker.com/build/cache/optimize/) |
| Fichiers exclus | `.dockerignore`. | [bonnes pratiques](https://docs.docker.com/build/building/best-practices/) |
| Utilisateur | Non root. | [bonnes pratiques](https://docs.docker.com/build/building/best-practices/) |
| Copie de fichiers | `COPY`. `ADD` seulement pour extraire une archive. | [bonnes pratiques](https://docs.docker.com/build/building/best-practices/) |
| Commande de démarrage | Forme tableau : `ENTRYPOINT ["./Chronos.Api"]`. | [référence](https://docs.docker.com/reference/dockerfile/) |
| Secrets de construction | `--secret` et `RUN --mount=type=secret`. Jamais `ARG` ni `ENV`. | [secrets](https://docs.docker.com/build/building/secrets/) |
| Contrôle du Dockerfile | `docker build --check .` | [build checks](https://docs.docker.com/build/checks/) |

### Conteneuriser l'API .NET 10

- **Images** : `sdk:10.0` pour compiler, `aspnet:10.0` pour exécuter. Le tag `latest` désigne .NET 11 : on écrit toujours `10.0`.
- **Distribution** : Ubuntu 24.04 par défaut. Il n'existe plus d'image Debian pour .NET 10 ([rupture](https://learn.microsoft.com/en-us/dotnet/core/compatibility/containers/10.0/default-images-use-ubuntu)).
- **Port** : l'application écoute sur **8080**, et non 80, depuis .NET 8. On le change avec `ASPNETCORE_HTTP_PORTS` ([rupture](https://learn.microsoft.com/en-us/dotnet/core/compatibility/containers/8.0/aspnet-port)).
- **Utilisateur** : les images fournissent un utilisateur non root `app` ; le Dockerfile se termine par `USER $APP_UID` ([introduction](https://learn.microsoft.com/en-us/dotnet/core/docker/introduction)).
- **Sans Dockerfile** : `dotnet publish /t:PublishContainer` fabrique une image directement depuis le SDK ([publication](https://learn.microsoft.com/en-us/dotnet/core/containers/sdk-publish)). Microsoft ne recommande pas une approche plutôt que l'autre. Le cours enseigne le Dockerfile, parce qu'il vaut pour tous les langages, et présente la publication par le SDK en complément.

Dockerfile de référence, adapté de l'[exemple officiel](https://github.com/dotnet/dotnet-docker/blob/main/samples/AspNetCoreRazorApp/Dockerfile) en remplaçant `11.0` par `10.0` :

```dockerfile
# syntax=docker/dockerfile:1
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /source
COPY --link Chronos.Api/*.csproj .
RUN dotnet restore -a $TARGETARCH
COPY --link Chronos.Api/. .
RUN dotnet publish -a $TARGETARCH --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
EXPOSE 8080
WORKDIR /app
COPY --link --from=build /app .
USER $APP_UID
ENTRYPOINT ["./Chronos.Api"]
```

> Ce Dockerfile suppose un projet unique et une restauration non verrouillée. **Essai du 12 novembre 2026 (module 23)** : avec les fichiers `packages.lock.json` du module 05 et `dotnet restore --locked-mode`, l'option `-a $TARGETARCH` fait échouer la restauration (`NU1004`). Le [module 23](../../cours/23-conteneuriser-l-application.md) contient le Dockerfile réellement construit pour la solution à plusieurs projets : publication portable, sans `-a`, et `ENTRYPOINT ["dotnet", "Chronos.Api.dll"]`. L'image `aspnet:10.0` ne contient pas `curl` : il faut l'installer, ou sonder autrement, pour un contrôle de santé.

### Conteneuriser l'interface Angular

- `ng build` écrit dans `dist/<projet>/`. Pour un hébergement statique, on copie **`dist/<projet>/browser/`** ([déploiement](https://angular.dev/tools/cli/deployment)).
- Le serveur web doit renvoyer `index.html` pour toute route inconnue, sans quoi un rechargement de page sur `/collaborateurs/12` échoue.
- Image de construction : `node:24`, dans la plage acceptée par Angular 22 (`^24.15.0`).
- Image d'exécution : nginx en variante non privilégiée, `nginx/nginx-unprivileged`, qui écoute sur 8080 ([fiche de l'image](https://hub.docker.com/r/nginx/nginx-unprivileged)).
- Le guide Docker [« Containerize an Angular application »](https://docs.docker.com/guides/angular/containerize/) fournit un Dockerfile et une configuration nginx complets. On reprend sa structure, **pas ses tags** : il épingle une version de Node antérieure au minimum exigé par Angular 22.

## 3. Pièges : ce qu'on trouve encore partout et qu'on n'écrit plus

| Ce qu'on voit dans les tutoriels | Statut | À écrire à la place | Source |
| --- | --- | --- | --- |
| `docker-compose up` (avec un tiret) | Compose v1, **plus maintenu** | `docker compose up` | [produits retirés](https://docs.docker.com/retired/) |
| `version: '3.8'` en tête de fichier | **Obsolète**, provoque un avertissement | Supprimer la ligne | [version et nom](https://docs.docker.com/reference/compose-file/version-and-name/) |
| `docker-compose.yml` | Accepté pour compatibilité | `compose.yaml` | [modèle d'application](https://docs.docker.com/compose/intro/compose-application-model/) |
| `links:` | Inutile | Le nom du service suffit | [services](https://docs.docker.com/reference/compose-file/services/) |
| `docker run --link` | **Déprécié**, retrait prévu | Un réseau défini par l'utilisateur, ou Compose | [fonctionnalités dépréciées](https://docs.docker.com/engine/deprecated/) |
| `depends_on: [db]` | Valide, mais n'attend pas que la base soit prête | `condition: service_healthy` et un `healthcheck` | [ordre de démarrage](https://docs.docker.com/compose/how-tos/startup-order/) |
| `ports: ["5432:5432"]` | Valide, mais expose la base à tout le réseau | `"127.0.0.1:5432:5432"` | [services](https://docs.docker.com/reference/compose-file/services/) |
| `image: postgres` ou `postgres:latest` | Version imprévisible | `postgres:18` | [bonnes pratiques](https://docs.docker.com/build/building/best-practices/) |
| Volume sur `/var/lib/postgresql/data` | **Faux à partir de PostgreSQL 18** | `/var/lib/postgresql` | [fiche de l'image](https://hub.docker.com/_/postgres) |
| `SA_PASSWORD` | **Déprécié** | `MSSQL_SA_PASSWORD` | [variables](https://learn.microsoft.com/en-us/sql/linux/sql-server-linux-configure-environment-variables?view=sql-server-ver17) |
| `/opt/mssql-tools/bin/sqlcmd` | Ancien chemin | `/opt/mssql-tools18/bin/sqlcmd`, avec l'option `-C` | [démarrage rapide](https://learn.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker?view=sql-server-ver17) |
| `docker compose down -v` « pour nettoyer » | **Détruit les données** | `docker compose down` | [`down`](https://docs.docker.com/reference/cli/docker/compose/down/) |
| Mot de passe dans `compose.yaml` versionné | Déconseillé | Fichier `.env` ignoré par git, ou secrets Compose | [variables](https://docs.docker.com/compose/how-tos/environment-variables/set-environment-variables/) |
| `MAINTAINER` | **Déprécié** | `LABEL org.opencontainers.image.authors=...` | [fonctionnalités dépréciées](https://docs.docker.com/engine/deprecated/) |
| `ENV CLE valeur` | Déconseillé | `ENV CLE=valeur` | [référence](https://docs.docker.com/reference/dockerfile/) |
| `CMD dotnet app.dll` (forme texte) | Déconseillé : les signaux d'arrêt n'arrivent pas au processus | `ENTRYPOINT ["dotnet", "app.dll"]` | [build checks](https://docs.docker.com/reference/build-checks/) |
| Secret passé par `ARG` ou `ENV` | Déconseillé : il reste lisible dans l'image | `RUN --mount=type=secret` | [secrets](https://docs.docker.com/build/building/secrets/) |
| `DOCKER_BUILDKIT=0` | Ancien constructeur, **déprécié** | Rien : BuildKit est le défaut | [fonctionnalités dépréciées](https://docs.docker.com/engine/deprecated/) |
| `EXPOSE 80` pour une image .NET | Faux depuis .NET 8 | `EXPOSE 8080` | [rupture](https://learn.microsoft.com/en-us/dotnet/core/compatibility/containers/8.0/aspnet-port) |
| `aspnet:10.0-bookworm-slim` | N'existe pas pour .NET 10 | `aspnet:10.0` | [rupture](https://learn.microsoft.com/en-us/dotnet/core/compatibility/containers/10.0/default-images-use-ubuntu) |
| `COPY dist/app /usr/share/nginx/html` | Incomplet | `dist/app/browser` | [déploiement](https://angular.dev/tools/cli/deployment) |
| `nginxinc/nginx-unprivileged` | Dépôt archivé | `nginx/nginx-unprivileged` | [fiche de l'image](https://hub.docker.com/r/nginxinc/nginx-unprivileged) |
| « Docker Desktop exige Hyper-V » | Dépassé | WSL 2 | [WSL](https://docs.docker.com/desktop/features/wsl/) |

### Ce qui n'est *pas* obsolète, contrairement à ce qu'on lit parfois

- **`docker run`, `docker ps`, `docker images`** restent valides. Ce sont des alias de `docker container run`, `docker container ls` et `docker image ls` ; la documentation n'exprime pas de préférence.
- **L'option `-v`** n'est pas dépréciée. `--mount` est simplement plus explicite.
- **`ASPNETCORE_URLS`** fonctionne toujours ; `ASPNETCORE_HTTP_PORTS` est plus simple.
- **`container_name` et `restart`** ne sont pas dépréciés.

## 4. Points à surveiller

- **Contrôle de santé de SQL Server.** Microsoft n'en documente aucun. La commande utilisée par le cours (`sqlcmd ... -Q "SELECT 1"`) est une pratique de la communauté. **Elle a été validée par l'exécution** : le service passe à l'état sain dès que SQL Server accepte les connexions.
- **SQL Server sous Docker Desktop pour Windows.** La documentation n'affirme la prise en charge que pour des « hôtes Linux x86-64 ». **L'image fonctionne** sous Docker Desktop avec WSL 2 sur un processeur x86-64 (essai du 12 novembre 2026). Sur un processeur ARM, elle n'est pas prise en charge.
- **`MSSQL_PID` — tranché par l'essai** (image `2025-latest`, module 07). Variable absente ou `EnterpriseDeveloper` : édition *Enterprise Developer*. `StandardDeveloper` : *Standard Developer*. `Developer` est encore accepté et donne *Enterprise Developer*, mais ne figure plus dans la [liste des valeurs de SQL Server 2025](https://learn.microsoft.com/en-us/sql/linux/sql-server-linux-configure-environment-variables?view=sql-server-ver17). Le cours **omet** la variable.
- **Volumes anonymes.** Un conteneur lancé sans volume à partir d'une image qui en déclare un (c'est le cas de `postgres`) crée un volume anonyme, que `docker rm` ne supprime pas. `docker rm -v` le supprime avec le conteneur (constaté).
- **Limites de Docker Hub.** 100 téléchargements d'image par tranche de 6 heures et par adresse IP sans compte, 200 avec un compte gratuit ([limites](https://docs.docker.com/docker-hub/usage/pulls/)). Derrière une adresse IP d'entreprise partagée, l'erreur 429 arrive vite : `docker login` la repousse.
- **Tags de `nginx/nginx-unprivileged` — relevés au module 23.** `1.30` et `1.30-alpine` ; l'image tourne sous l'identifiant 101 et écoute sur 8080. Le cours utilise `1.30-alpine`.
- **Tableau de bord Aspire autonome.** L'image actuelle est `mcr.microsoft.com/aspire/dashboard` (13.6.5 à l'essai du module 25) ; `dotnet/aspire-dashboard` est l'ancien nom.
- **Testcontainers lancé depuis un conteneur** (essai du module 11, Docker Desktop) : monter `/var/run/docker.sock` suffit, à condition de définir `TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal`. Sur le poste du mentoré et sur un runner GitHub, rien de tout cela n'est nécessaire.
- **Tags flottants.** `postgres:18` et `2025-latest` suivent les correctifs. Pour une reproductibilité stricte, on épingle une version complète, voire une empreinte (`@sha256:...`).
- **Ce qui a été exécuté.** Toutes les commandes du module 01 et le fichier `compose.yaml` l'ont été le 12 novembre 2026 (Docker Desktop 4.99, Engine 29.9.3, Compose 5.7.0, PostgreSQL 18.6, SQL Server 17.0.5045.3). Les deux Dockerfile du module 23 (API à cinq projets, interface servie par nginx), le profil Compose `app`, le relais `/api` et les en-têtes transmis ont été construits et exécutés le même jour (Buildx 0.38.1, nginx 1.30.6). Le Dockerfile d'exemple de la section 2, lui, ne l'a été que pour constater son échec en mode verrouillé.
