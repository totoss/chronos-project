# Module 23 — Conteneuriser l'application

> Pratiques vérifiées le 12 novembre 2026 — voir le [référentiel Docker](../docs/references/docker.md).
> Les deux Dockerfile, la configuration nginx et les services Compose de ce module ont été **construits et exécutés le 12 novembre 2026** sur une application minimale de même structure (API .NET 10 à cinq projets organisés comme au module 07, avec fichiers de verrouillage et les réglages d'analyse du module 22 ; application Angular créée par la CLI 22.2.7), avec Docker Desktop (Engine 29.9.3, Compose 5.7.0, Buildx 0.38.1), le SDK .NET 10.0.404, Node.js 24.22.3, nginx 1.30.6 et PostgreSQL 18. Les variantes des exercices 1 à 4 ont été essayées aussi, l'exercice 2 avec un certificat produit par `openssl`. **Non exécutés** : la connexion à l'application réelle (l'essai n'avait pas l'authentification du module 12 ; seul le comportement des cookies a été vérifié), l'exercice 5 (SQL Server) et l'exercice 6 (clés de protection des données). Signalez au mentor tout écart constaté.

Depuis le module 01, les bases de données de Chronos tournent en conteneur, mais l'application elle-même tourne « à la main » : `dotnet run` d'un côté, `ng serve` de l'autre. C'est parfait pour développer, et inutilisable pour livrer : un serveur de production n'a ni SDK .NET, ni Node.js, ni vos sources.

Ce module fabrique ce qu'on livre réellement : une **image** pour l'API, une image pour l'interface, et de quoi démarrer l'ensemble — bases comprises — par une seule commande.

## Objectifs

À la fin de ce module, vous saurez :

- écrire un Dockerfile en plusieurs étapes pour l'API, qui tire parti du cache et produit une image sans SDK, exécutée par un utilisateur non privilégié ;
- écrire le Dockerfile de l'interface : compilation par Node.js, service des fichiers par nginx ;
- configurer nginx pour une application à routage côté navigateur et pour relayer `/api` vers l'API ;
- expliquer ce que sont les en-têtes `X-Forwarded-*` et dans quelles conditions l'API peut leur faire confiance ;
- ajouter l'application au fichier Compose sous un profil, sans changer le comportement de `docker compose up` ;
- transmettre la configuration à un conteneur sans jamais placer de secret dans une image.

## Prérequis

- [Module 01](01-docker-les-bases.md) : images, conteneurs, volumes, Compose, contrôles de santé.
- [Module 03](03-premiere-api.md) : l'API a été créée avec `--no-https` ; elle n'écoute qu'en HTTP.
- [Module 05](05-github-actions-integration-continue.md) : les fichiers `packages.lock.json` sont versionnés.
- [Module 06](06-persister-avec-ef-core.md) : les migrations ont été appliquées à la base PostgreSQL du projet Compose.
- [Module 12](12-securite-et-tracabilite.md) : le cookie de session `chronos.session` et le cookie `XSRF-TOKEN`.
- [Module 13](13-premiere-application-angular.md) et suivants : l'application Angular existe dans `src/web`, et son serveur de développement relaie `/api`.
- [Module 22](22-qualite-du-code.md) : la solution compile sans avertissement, les règles étant réglées dans le fichier `.editorconfig` de la racine du dépôt.

## Règles de gestion

Aucune. Ce module ne change pas le comportement de Chronos ; il change la façon de le faire tourner.

## Notions

### Pourquoi conteneuriser l'application

Une image contient l'application compilée **et** tout ce qu'il lui faut pour s'exécuter : le runtime .NET pour l'API, un serveur web pour l'interface. Trois conséquences :

- ce que vous testez sur votre poste est, à l'octet près, ce qui tournera en production ;
- la machine cible n'a besoin que de Docker, ou d'un hébergeur qui exécute des images ;
- revenir à la version précédente, c'est redémarrer l'image précédente.

Vous continuerez à développer avec `dotnet run` et `ng serve`, qui rechargent le code à chaud. Les images servent à vérifier l'application assemblée, puis à la livrer ([module 24](24-github-actions-construire-et-publier.md)).

### Dockerfile et construction en plusieurs étapes

Un **Dockerfile** est la recette d'une image : une suite d'instructions (`FROM`, `COPY`, `RUN`…) que `docker build` exécute dans l'ordre.

Compiler l'API demande le SDK .NET, dont l'image pèse plus d'un gigaoctet. L'exécuter ne demande que le runtime ASP.NET Core. Une construction **en plusieurs étapes** enchaîne donc deux images dans le même fichier : la première compile, la seconde ne reçoit que le résultat. Le SDK, les sources et les paquets NuGet n'arrivent jamais dans l'image livrée.

Documentation : [Bonnes pratiques de construction](https://docs.docker.com/build/building/best-practices/), [référence du Dockerfile](https://docs.docker.com/reference/dockerfile/)

### Couches et cache

Chaque instruction produit une **couche**. Docker réutilise une couche tant que l'instruction et les fichiers qu'elle copie n'ont pas changé ; dès qu'une couche change, toutes les suivantes sont refaites.

D'où l'ordre des instructions : on copie d'abord ce qui change rarement (la liste des dépendances), on restaure les paquets, puis on copie le code, qui change à chaque commit. Modifier un endpoint ne retélécharge alors aucun paquet.

Documentation : [Optimiser l'usage du cache](https://docs.docker.com/build/cache/optimize/)

### Contexte de construction et `.dockerignore`

`docker build` envoie un dossier au moteur : le **contexte**. `COPY` ne peut lire que dans ce dossier. Pour l'API, le contexte est `src/api` : le Dockerfile a besoin de plusieurs projets, de `Directory.Build.props` et de `Directory.Packages.props`.

Le fichier `.dockerignore`, à la racine du contexte, liste ce qui n'y entre pas. Il est indispensable ici : vos dossiers `bin/` et `obj/` contiennent des fichiers produits sous Windows, avec des chemins Windows, qui écraseraient ceux que la restauration vient de créer dans l'image.

Un fichier manque pourtant à ce contexte : le `.editorconfig` de la racine du dépôt, deux dossiers plus haut. Depuis le module 22, c'est lui qui règle la sévérité des règles d'analyse, et tout avertissement est une erreur : sans lui, la compilation dans l'image n'appliquerait pas les mêmes règles que sur votre poste. Un **contexte nommé** donne au Dockerfile accès à un second dossier, pour ce seul fichier.

Documentation : [Contexte de construction](https://docs.docker.com/build/concepts/context/#dockerignore-files) — voir aussi, sur la même page, la section *Named contexts* ; [`additional_contexts` dans Compose](https://docs.docker.com/reference/compose-file/build/)

### Les images .NET

| Image | Rôle |
| --- | --- |
| `mcr.microsoft.com/dotnet/sdk:10.0` | Compiler. Jamais en production. |
| `mcr.microsoft.com/dotnet/aspnet:10.0` | Exécuter une application ASP.NET Core. Ubuntu 24.04. |
| `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled` | La même, réduite au strict nécessaire : ni shell, ni gestionnaire de paquets. |

Trois faits à connaître :

- l'application écoute sur le port **8080**, fixé par la variable `ASPNETCORE_HTTP_PORTS` de l'image ;
- l'image fournit un utilisateur non privilégié, `app`, dont l'identifiant est dans la variable `APP_UID` (1654). Il faut le choisir explicitement par `USER $APP_UID` ; sans cette ligne, l'application tourne sous `root` ;
- l'image d'exécution ne contient ni `curl` ni `wget` (constaté), ce qui compte pour le contrôle de santé.

Documentation : [Images de conteneur .NET](https://learn.microsoft.com/en-us/dotnet/core/docker/container-images), [utilisateur non root](https://learn.microsoft.com/en-us/dotnet/core/compatibility/containers/8.0/app-user), [images *chiseled*](https://github.com/dotnet/dotnet-docker/blob/main/documentation/ubuntu-chiseled.md)

### Sans Dockerfile : la publication par le SDK

Le SDK .NET sait fabriquer une image seul : `dotnet publish /t:PublishContainer`. Il choisit l'image de base, l'utilisateur non privilégié et le port. Microsoft ne recommande pas une approche plutôt que l'autre.

Le cours enseigne le Dockerfile parce qu'il vaut pour tous les langages — l'interface Angular en a besoin de toute façon — et parce qu'il montre ce qui se passe. L'exercice 4 vous fait essayer l'autre voie.

Documentation : [Conteneuriser avec `dotnet publish`](https://learn.microsoft.com/en-us/dotnet/core/containers/sdk-publish)

### Servir l'interface : repli et relais

`ng build` produit des fichiers statiques. N'importe quel serveur web peut les servir, à deux conditions.

**Le repli.** `/collaborateurs/12` n'est pas un fichier : c'est une route de l'application Angular. Si l'utilisateur recharge cette page, le serveur doit renvoyer `index.html`, et Angular affichera la bonne vue. Sans ce repli, le rechargement donne une erreur 404.

**Le relais.** Depuis le module 16, l'interface appelle `/api/...` sur sa propre origine, et le serveur de développement relaie vers l'API. En conteneur, nginx reprend ce rôle : même origine, donc toujours pas de CORS, et les cookies d'authentification du [module 12](12-securite-et-tracabilite.md) continuent de fonctionner.

Documentation : [Déploiement d'une application Angular](https://angular.dev/tools/cli/deployment), [`try_files`](https://nginx.org/en/docs/http/ngx_http_core_module.html#try_files), [`proxy_pass`](https://nginx.org/en/docs/http/ngx_http_proxy_module.html#proxy_pass)

### Les en-têtes transmis

Depuis le module 03, l'API n'écoute qu'en HTTP : le HTTPS est l'affaire du **serveur frontal**, celui qui reçoit les requêtes du navigateur. C'est lui qui porte le certificat ; derrière lui, sur un réseau privé, tout circule en HTTP.

Conséquence : l'API ne voit plus le navigateur. Toutes les requêtes lui semblent venir de nginx, en HTTP. Elle ignore l'adresse du client, et surtout elle ignore que celui-ci était en HTTPS. Or le module 12 en dépend : le cookie `XSRF-TOKEN` n'est marqué `Secure` que si `Request.IsHttps` est vrai. Le cookie de session, lui, a été réglé sur `Secure` en toutes circonstances hors développement, précisément pour ne pas dépendre de ce que voit l'API.

Le relais transmet donc ces informations dans des en-têtes, `X-Forwarded-For` et `X-Forwarded-Proto`, et ASP.NET Core les applique grâce à un middleware, `UseForwardedHeaders`. Mais ces en-têtes sont de simples chaînes : n'importe qui peut les écrire. ASP.NET Core **ne les accepte donc que des relais qu'on lui a désignés** — par défaut, ceux de sa propre machine. Un nginx placé dans un autre conteneur est ignoré, sans erreur.

Documentation : [ASP.NET Core derrière un proxy](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0)

### Profils Compose

Un service qui porte l'attribut `profiles` n'est démarré que si son profil est activé. Les services sans profil le sont toujours. `docker compose up` continuera donc à ne démarrer que les bases ; `docker compose --profile app up` démarrera aussi l'API et l'interface.

Documentation : [Profils](https://docs.docker.com/compose/how-tos/profiles/)

### Configuration et secrets

Une image ne contient **aucune configuration propre à un environnement, et aucun secret** : la même image doit pouvoir tourner partout, et tout ce qu'elle contient est lisible par quiconque la télécharge.

La configuration arrive au démarrage, par des variables d'environnement. ASP.NET Core les lit d'office, et elles l'emportent sur `appsettings.json`. La clé `ConnectionStrings:Chronos` s'écrit `ConnectionStrings__Chronos` : deux tirets bas remplacent le deux-points, que certains systèmes refusent dans un nom de variable.

Une variable d'environnement n'est pas un coffre : elle est visible de tous les processus du conteneur et de quiconque peut lancer `docker inspect`. Les **secrets** de Compose transmettent la valeur par un fichier monté dans `/run/secrets/`, lisible du seul conteneur qui le demande (exercice 3).

Documentation : [Configuration d'ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-10.0), [secrets Compose](https://docs.docker.com/compose/how-tos/use-secrets/)

## Pas à pas

### 1. Donner à l'API un point de contrôle de santé

Compose devra savoir quand l'API est prête. Dans `Program.cs` :

```csharp
builder.Services.AddHealthChecks();
```

puis, après `var app = builder.Build();` :

```csharp
app.MapHealthChecks("/health");
```

Lancez l'API et appelez `http://localhost:5080/health` : la réponse est `Healthy`, en texte, avec le code 200. Pour l'instant, ce point de contrôle dit seulement « le processus répond » ; le [module 25](25-exploiter-l-application.md) lui fera vérifier la base.

Documentation : [Contrôles de santé](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-10.0)

### 2. Exclure ce qui ne doit pas entrer dans l'image

Créez `src/api/.dockerignore` :

```text
**/bin/
**/obj/
**/TestResults/
**/artifacts/
**/*.user
**/appsettings.Development.json
**/Properties/launchSettings.json
.vs/
Dockerfile
.dockerignore
```

`**` désigne n'importe quelle profondeur de dossiers. Les deux fichiers de configuration de développement sont exclus parce qu'ils n'ont rien à faire dans une image de production.

### 3. Écrire le Dockerfile de l'API

Créez `src/api/Dockerfile` :

```dockerfile
# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY --link global.json Directory.Build.props Directory.Packages.props ./
COPY --link --from=racine .editorconfig ./
COPY --link Chronos.Domain/Chronos.Domain.csproj Chronos.Domain/packages.lock.json Chronos.Domain/
COPY --link Chronos.Infrastructure/Chronos.Infrastructure.csproj Chronos.Infrastructure/packages.lock.json Chronos.Infrastructure/
COPY --link Chronos.Migrations.PostgreSql/Chronos.Migrations.PostgreSql.csproj Chronos.Migrations.PostgreSql/packages.lock.json Chronos.Migrations.PostgreSql/
COPY --link Chronos.Migrations.SqlServer/Chronos.Migrations.SqlServer.csproj Chronos.Migrations.SqlServer/packages.lock.json Chronos.Migrations.SqlServer/
COPY --link Chronos.Api/Chronos.Api.csproj Chronos.Api/packages.lock.json Chronos.Api/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore Chronos.Api/Chronos.Api.csproj --locked-mode

COPY --link Chronos.Domain/ Chronos.Domain/
COPY --link Chronos.Infrastructure/ Chronos.Infrastructure/
COPY --link Chronos.Migrations.PostgreSql/ Chronos.Migrations.PostgreSql/
COPY --link Chronos.Migrations.SqlServer/ Chronos.Migrations.SqlServer/
COPY --link Chronos.Api/ Chronos.Api/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish Chronos.Api/Chronos.Api.csproj --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN --mount=type=cache,target=/var/lib/apt/lists,sharing=locked \
    apt-get update && apt-get install -y --no-install-recommends curl
EXPOSE 8080
WORKDIR /app
COPY --link --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "Chronos.Api.dll"]
```

Ce qu'il faut y lire :

- **`# syntax=docker/dockerfile:1`** demande la version courante de la syntaxe, nécessaire à `--link` et `--mount`.
- **`FROM ... AS build`** ouvre la première étape et la nomme.
- **Les premiers `COPY`** n'apportent que ce qui décrit les dépendances : les fichiers de projet, les fichiers communs, les fichiers de verrouillage. On y trouve les cinq projets dont l'API a besoin — les deux projets de migrations du [module 07](07-deux-sgbd-un-seul-code.md) compris, puisque `Chronos.Api` les référence. Les projets de tests n'y figurent pas.
- **`COPY --from=racine .editorconfig`** prend le fichier dans le contexte nommé `racine`, que vous fournirez à l'étape 4. Placé dans `/source`, il s'applique à tout le code compilé dans l'image.
- **`dotnet restore --locked-mode`** installe exactement les versions des fichiers de verrouillage, comme l'intégration continue du module 05.
- **`--mount=type=cache`** conserve les paquets NuGet d'une construction à l'autre, hors de l'image. Le montage figure sur les deux `RUN` : les paquets n'étant pas dans une couche, la publication doit pouvoir les retrouver.
- **`dotnet publish --no-restore -o /app`** compile en configuration `Release` et rassemble l'application dans `/app`.
- **Le second `FROM`** ouvre l'image livrée. Elle ne reçoit de la première étape que `/app`.
- **`curl`** est installé pour le contrôle de santé de l'étape 7. Le montage de cache évite de laisser les listes de paquets d'`apt` dans l'image.
- **`USER $APP_UID`** : l'application ne tourne pas sous `root`.
- **`ENTRYPOINT`** en forme tableau : le processus reçoit directement le signal d'arrêt.

> **Écart avec l'exemple officiel.** Le [Dockerfile d'exemple de Microsoft](https://github.com/dotnet/dotnet-docker/blob/main/samples/AspNetCoreRazorApp/Dockerfile) restaure avec `-a $TARGETARCH`, pour construire des images de plusieurs architectures. Cette option est **incompatible avec `--locked-mode`** tant que les fichiers de verrouillage ne mentionnent pas l'architecture : la restauration échoue avec l'erreur `NU1004` (constaté). Le cours garde le verrouillage et publie une application indépendante de l'architecture, lancée par `dotnet Chronos.Api.dll` : l'image se construit ainsi à l'identique sur un poste x64 ou ARM.

### 4. Construire l'image et l'essayer

Depuis la racine du dépôt :

```powershell
docker build --build-context racine=. -t chronos-api:local src/api
```

`-t` nomme l'image ; le dernier argument est le contexte ; `--build-context racine=.` déclare le contexte nommé `racine`, ici le dossier courant. Docker n'en transfère que le fichier demandé.

La première construction télécharge les paquets ; modifiez ensuite une ligne de `Program.cs` et relancez : l'étape de restauration affiche `CACHED`.

```powershell
docker run --rm -d --name api-essai -p 127.0.0.1:8081:8080 chronos-api:local
```

```powershell
curl.exe -i http://localhost:8081/health
```

La réponse est `200` et `Healthy`. Vérifiez sous quel utilisateur tourne l'application, puis arrêtez le conteneur :

```powershell
docker exec api-essai id
```

```powershell
docker stop api-essai
```

`id` affiche `uid=1654(app)`. Notez aussi, dans `docker logs`, que l'environnement est `Production` : c'est la valeur par défaut quand `ASPNETCORE_ENVIRONMENT` n'est pas définie. La documentation OpenAPI et Scalar, réservées au développement, ne sont donc pas exposées.

### 5. Configurer nginx

Créez `src/web/nginx.conf` :

```nginx
server {
    listen 8080;
    root /usr/share/nginx/html;
    index index.html;

    location /api/ {
        proxy_pass http://api:8080;
        proxy_set_header Host $http_host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    location / {
        try_files $uri $uri/ /index.html;
    }
}
```

- **`location /api/`** : tout ce qui commence par `/api/` part vers le service `api`, port 8080. `api` est le nom du service Compose de l'étape 7 ; sur le réseau du projet, ce nom suffit. `proxy_pass` est écrit sans chemin final : l'adresse demandée est transmise telle quelle, `/api` compris.
- **`Host $http_host`** transmet à l'API le nom d'hôte et le port demandés par le navigateur. L'[exemple de Microsoft](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx?view=aspnetcore-10.0) écrit `$host`, qui omet le port : sans importance derrière le port 443, gênant ici où l'application sera sur le port 8080.
- **`X-Forwarded-For`** et **`X-Forwarded-Proto`** : l'adresse du client et le protocole qu'il a utilisé.
- **`try_files`** : servir le fichier demandé s'il existe, sinon `index.html`. C'est le repli.

### 6. Écrire le Dockerfile de l'interface

Créez `src/web/.dockerignore` :

```text
node_modules/
dist/
.angular/
Dockerfile
.dockerignore
```

Puis `src/web/Dockerfile` :

```dockerfile
# syntax=docker/dockerfile:1
FROM node:24 AS build
WORKDIR /source

COPY --link package.json package-lock.json ./
RUN --mount=type=cache,target=/root/.npm \
    npm ci

COPY --link . .
RUN npx ng build

FROM nginx/nginx-unprivileged:1.30-alpine
COPY --link nginx.conf /etc/nginx/conf.d/default.conf
COPY --link --from=build /source/dist/chronos-web/browser /usr/share/nginx/html
EXPOSE 8080
```

Même logique que pour l'API : les fichiers de dépendances d'abord, `npm ci` — qui respecte strictement `package-lock.json` —, puis les sources et la compilation. `ng build` utilise la configuration de production par défaut.

Deux points d'attention :

- **Le dossier copié est `dist/<projet>/browser`**, et non `dist/<projet>`. `<projet>` est le nom de votre application dans `angular.json` — `chronos-web` depuis le module 13. `ng build` l'affiche à la ligne `Output location`.
- **`nginx/nginx-unprivileged`** est la variante de l'image nginx qui tourne sous un utilisateur non privilégié (identifiant 101) et écoute sur le port 8080, puisqu'un tel utilisateur ne peut pas ouvrir le port 80. Elle est publiée par l'éditeur de nginx.

### 7. Ajouter l'application au fichier Compose

Dans `compose.yaml`, ajoutez deux services après `sqlserver` :

```yaml
  api:
    profiles: [app]
    build:
      context: src/api
      additional_contexts:
        racine: .
    environment:
      ConnectionStrings__Chronos: Host=postgres;Database=chronos;Username=chronos;Password=${POSTGRES_PASSWORD:?POSTGRES_PASSWORD manquant, voir .env.example}
      Chronos__Fournisseur: PostgreSql
    depends_on:
      postgres:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "curl", "--fail", "--silent", "http://localhost:8080/health"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 20s

  web:
    profiles: [app]
    build:
      context: src/web
    ports:
      - "127.0.0.1:8080:8080"
    depends_on:
      api:
        condition: service_healthy
        restart: true
```

- **`profiles: [app]`** : ces deux services ne démarrent que sur demande.
- **`build`** : Compose construit l'image à partir du Dockerfile du contexte, et la nomme `chronos-api` ou `chronos-web`. `additional_contexts` joue le rôle de `--build-context`.
- **`Host=postgres`** : dans le réseau du projet, la base s'appelle par le nom de son service, sur son port interne 5432. Le port publié sur votre poste ne joue aucun rôle ici ; il ne sert qu'à vous, pour `dotnet run` et vos outils.
- **`depends_on` avec `service_healthy`** : l'API attend que la base accepte les connexions, l'interface attend que l'API réponde.
- **`restart: true`** : `docker compose restart api` redémarre aussi `web`.
- **L'API n'a pas de `ports`.** Elle n'est joignable que par nginx. L'étape 9 montre pourquoi c'est important.

### 8. Démarrer et vérifier

```powershell
docker compose up -d --wait
```

Rien de nouveau : seules les bases démarrent.

```powershell
docker compose --profile app up -d --build --wait
```

`--build` reconstruit les images si les sources ont changé. La commande rend la main quand tout est sain. Ouvrez `http://localhost:8080` : l'application s'affiche et vous pouvez vous connecter. Les tables existent déjà, puisque vous avez appliqué les migrations à cette même base au module 06.

> **Un cookie `Secure` en HTTP ?** L'API tourne ici en environnement `Production` : le cookie de session est donc marqué `Secure`, alors que vous êtes en HTTP. La connexion fonctionne parce que les navigateurs traitent `localhost` comme une origine sûre (constaté avec Chromium 156). Ce n'est vrai que pour `localhost`.

Vérifiez le repli et le relais :

```powershell
curl.exe -i http://localhost:8080/collaborateurs/12
```

```powershell
curl.exe -i http://localhost:8080/api/projets
```

La première commande renvoie `200` et du HTML — c'est `index.html`. La seconde renvoie une réponse de l'API : du JSON, ou `401` si la route exige une connexion.

Pour arrêter, le profil doit être répété, sans quoi seules les bases sont arrêtées :

```powershell
docker compose --profile app down
```

> **Et les migrations ?** L'API n'appelle pas `Migrate()` à son démarrage, et c'est voulu : en production, le compte de l'application ne devrait pas avoir le droit de modifier le schéma, et plusieurs exemplaires de l'API démarreraient en même temps. Le [module 25](25-exploiter-l-application.md) traite le déploiement des migrations.

### 9. Faire confiance au relais

Ajoutez provisoirement à l'API un endpoint qui montre ce qu'elle perçoit :

```csharp
app.MapGet("/api/diagnostic", (HttpContext http) => new
{
    Schema = http.Request.Scheme,
    Hote = http.Request.Host.Value,
    AdresseDistante = http.Connection.RemoteIpAddress?.ToString(),
});
```

Reconstruisez, puis appelez `http://localhost:8080/api/diagnostic`. L'adresse distante est celle du conteneur nginx : rien ne lit encore les en-têtes transmis.

Dans `Program.cs`, avec `using Microsoft.AspNetCore.HttpOverrides;` en tête de fichier, déclarez les en-têtes à lire et le réseau dont ils sont acceptés :

```csharp
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    var reseau = builder.Configuration["Chronos:ReseauDuRelais"];
    if (!string.IsNullOrEmpty(reseau))
    {
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(reseau));
    }
});
```

Puis, juste après `var app = builder.Build();`, avant tout autre middleware :

```csharp
app.UseForwardedHeaders();
```

Le réseau de confiance vient de la configuration : en développement, la clé est absente et seule votre machine est crue. Dans `compose.yaml`, donnez une adresse fixe au réseau du projet, à la fin du fichier :

```yaml
networks:
  default:
    ipam:
      config:
        - subnet: 10.89.0.0/24
```

et transmettez-la à l'API, dans son bloc `environment` :

```yaml
      Chronos__ReseauDuRelais: 10.89.0.0/24
```

Un réseau existant ne change pas d'adresse : arrêtez tout avec `docker compose --profile app down`, puis relancez avec `--build`. Rappelez l'endpoint : l'adresse distante est maintenant celle du client. Mettez une adresse de réseau erronée dans la variable et recommencez : les en-têtes sont de nouveau ignorés, et l'API ne le signale qu'au niveau de journalisation `Debug`, par le message `Unknown proxy`.

**Pourquoi l'API n'a pas de port publié.** Une requête arrivée par un port publié provient, vue du conteneur, de la passerelle du réseau du projet — donc d'une adresse de confiance. Quiconque atteindrait ce port pourrait écrire lui-même `X-Forwarded-Proto: https` et une fausse adresse, et l'API les croirait (constaté). La confiance accordée au réseau ne vaut que si nginx en est la seule porte d'entrée.

**Et le HTTPS ?** Ici, nginx reçoit du HTTP : `$scheme` vaut `http`, et le cookie `XSRF-TOKEN` part sans `Secure`. En production, quelqu'un doit porter le certificat. Si c'est nginx, `$scheme` vaut `https`, l'API reçoit `X-Forwarded-Proto: https`, `Request.IsHttps` devient vrai et le cookie est marqué `Secure` : c'est l'exercice 2. Si c'est un serveur de l'hébergeur placé devant nginx, c'est son en-tête que nginx doit retransmettre, et non `$scheme` ; cela dépend de l'hébergeur ([module 24](24-github-actions-construire-et-publier.md)).

Supprimez l'endpoint de diagnostic.

> **Le raccourci que vous verrez ailleurs.** La variable d'environnement `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` active le même middleware sans une ligne de code — et accepte les en-têtes de **n'importe quelle** adresse (constaté). Elle est faite pour des hébergements dont on ne connaît pas l'adresse des relais. Quand on peut nommer le réseau, on le nomme.

Documentation : [Changement de comportement des en-têtes transmis](https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/8/forwarded-headers-unknown-proxies), [réseaux dans le fichier Compose](https://docs.docker.com/reference/compose-file/networks/)

### 10. Examiner les images

```powershell
docker image ls
```

Dans l'essai : environ 470 Mo pour l'API, dont 342 pour l'image `aspnet:10.0` ; 82 Mo pour l'interface. L'image du SDK, elle, pèse 1,3 Go : c'est ce que la construction en plusieurs étapes vous évite de livrer.

Le reste de l'image de l'API, c'est votre application publiée. Le fournisseur SQL Server y pèse lourd : une publication indépendante de l'architecture embarque ses bibliothèques natives pour **toutes** les plateformes, soit 66 Mo sur 84 dans l'essai.

```powershell
docker image history chronos-api
```

Liste les couches et leur taille. Vous y retrouvez chaque instruction du Dockerfile — et tout ce qu'une instruction a copié. **Un secret copié dans une couche reste dans l'image, même si une instruction suivante le supprime.**

```powershell
docker scout quickview chronos-api
```

Docker Scout, fourni avec Docker Desktop, compare le contenu de l'image aux bases de vulnérabilités connues. Il demande d'être connecté à un compte Docker. `docker scout cves chronos-api` donne le détail.

Documentation : [`docker image history`](https://docs.docker.com/reference/cli/docker/image/history/), [Docker Scout](https://docs.docker.com/scout/)

## À vous

1. **Réduire l'image.** Remplacez l'image d'exécution de l'API par `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra`. La construction échoue : pourquoi ? Corrigez le Dockerfile, comparez la taille des deux images, puis essayez d'ouvrir un terminal dans le conteneur. Que devient le contrôle de santé du fichier Compose ? Qu'apporte le suffixe `-extra`, et Chronos en a-t-il besoin ? La page sur les [variantes d'images](https://github.com/dotnet/dotnet-docker/blob/main/documentation/image-variants.md) donne la réponse.

2. **Passer en HTTPS.** Procurez-vous un certificat auto-signé pour `localhost` (un fichier `.crt` et sa clé), montez-les dans le conteneur `web` par un volume, et faites écouter nginx en HTTPS sur le port 8443 avec les directives `listen ... ssl`, `ssl_certificate` et `ssl_certificate_key` ([configuration HTTPS de nginx](https://nginx.org/en/docs/http/configuring_https_servers.html)). Publiez ce port, puis comparez les en-têtes `Set-Cookie` de `GET /api/compte/moi` en HTTP et en HTTPS (`curl.exe -k -i`). Qu'est-ce qui a changé, et quelle ligne de `nginx.conf` en est responsable ? Pourquoi ni la clé ni le certificat ne doivent-ils entrer dans l'image ?

3. **Un vrai secret.** Transmettez la chaîne de connexion par un secret Compose plutôt que par une variable d'environnement. ASP.NET Core sait lire un dossier où chaque fichier est une clé : cherchez `AddKeyPerFile` dans la page de configuration. Vérifiez avec `docker inspect` que le mot de passe n'apparaît plus dans l'environnement du conteneur.

4. **Sans Dockerfile.** Fabriquez l'image de l'API avec `dotnet publish /t:PublishContainer`. Quelle image de base, quel utilisateur et quel point d'entrée le SDK a-t-il choisis ? Regardez ensuite `git status` : qu'est-il arrivé à vos fichiers `packages.lock.json`, et pourquoi ? Remettez-les en état.

5. **L'autre base.** Faites tourner l'application conteneurisée sur SQL Server ([module 07](07-deux-sgbd-un-seul-code.md)) : un second profil, ou une variable. Quelles valeurs changent, et lesquelles ne doivent surtout pas figurer dans `compose.yaml` ?

6. **Rester connecté.** Connectez-vous à l'application, puis recréez le conteneur de l'API avec `docker compose --profile app up -d --force-recreate api`. Êtes-vous toujours connecté ? Lisez la section sur les conteneurs de la page [Protection des données](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-10.0) et faites en sorte que les sessions survivent à un redéploiement.

## Critères de réussite

- `docker compose up -d --wait` ne démarre que les bases ; `docker compose --profile app up -d --build --wait` démarre l'application complète, tous les services étant sains.
- `http://localhost:8080` affiche l'application ; on s'y connecte ; recharger la page sur `/collaborateurs/12` ne produit pas d'erreur 404.
- `docker compose exec api id` affiche l'utilisateur `app`, et `docker compose exec web id` l'utilisateur `nginx`.
- Modifier un fichier `.cs` puis reconstruire ne relance pas la restauration des paquets.
- `docker build --check --build-context racine=. src/api` et `docker build --check src/web` ne signalent rien.
- Derrière nginx, l'API voit l'adresse du client ; avec un réseau de confiance erroné, elle voit celle de nginx.
- Aucun mot de passe ne figure dans un Dockerfile, dans `compose.yaml` ni dans `docker image history`.
- Le port de l'API n'est pas publié.
- Comparez avec la solution de référence : `git diff module-22 module-23`.

## Pièges

| Ce qu'on voit / ce qu'on fait | Pourquoi c'est un problème | À faire à la place |
| --- | --- | --- |
| Une seule étape `FROM ...sdk` | L'image livrée contient le SDK et les sources : plus d'un gigaoctet. | Deux étapes : `sdk` pour compiler, `aspnet` pour exécuter. |
| `COPY . .` avant `dotnet restore` | La moindre modification du code retélécharge tous les paquets. | Copier d'abord les fichiers de projet et de verrouillage. |
| Pas de `.dockerignore` | `bin/` et `obj/` du poste entrent dans l'image et faussent la publication. | Les exclure. |
| `docker build src/api` sans `--build-context racine=.` | Docker prend `racine` pour un nom d'image et échoue sur `pull access denied` (constaté). | Fournir le contexte nommé ; dans Compose, `additional_contexts`. |
| `dotnet restore -a $TARGETARCH --locked-mode` | Erreur `NU1004` : les fichiers de verrouillage ne connaissent pas l'architecture. | Restaurer sans `-a`, ou déclarer les architectures dans les projets. |
| `FROM ...aspnet:latest` | `latest` désigne .NET 11. | `aspnet:10.0`. |
| `aspnet:10.0-bookworm-slim` | N'existe pas : les images .NET 10 reposent sur Ubuntu. | `aspnet:10.0`. |
| `EXPOSE 80`, `-p 8080:80` | L'application écoute sur 8080 depuis .NET 8. | `EXPOSE 8080`. |
| Pas de ligne `USER` | L'application tourne sous `root`. | `USER $APP_UID`. |
| `HEALTHCHECK` avec `curl` sans l'avoir installé | L'image n'en contient pas : le conteneur reste `unhealthy`. | Installer `curl`, ou sonder de l'extérieur. |
| `ENTRYPOINT dotnet Chronos.Api.dll` (forme texte) | Le signal d'arrêt n'atteint pas l'application. | La forme tableau. |
| `COPY dist/chronos-web /usr/share/nginx/html` | Le dossier contient un sous-dossier `browser` : nginx ne trouve pas `index.html`. | `dist/chronos-web/browser`. |
| `nginxinc/nginx-unprivileged` | Ancien emplacement de l'image. | `nginx/nginx-unprivileged`. |
| Pas de `try_files` | Recharger une page autre que l'accueil donne 404. | `try_files $uri $uri/ /index.html;` |
| `proxy_pass http://api:8080/;` avec une barre finale | nginx retire `/api/` de l'adresse : l'API répond 404. | `proxy_pass http://api:8080;` |
| Une faute dans une adresse d'API renvoie du HTML et un code 200 | Hors de `/api/`, le repli répond à tout par `index.html`. | Garder toutes les routes de l'API sous `/api/`. |
| Erreur 502 après avoir recréé l'API | nginx a résolu le nom `api` à son démarrage ; si l'adresse du conteneur a changé, il l'ignore (constaté). | `docker compose --profile app restart web`. |
| `options.KnownNetworks.Add(...)` | Obsolète en .NET 10 (avertissement `ASPDEPR005`), bien que la documentation l'emploie encore. | `options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(...))`. |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` par habitude | Accepte les en-têtes de n'importe quelle adresse. | Désigner le réseau du relais. |
| `app.UseHttpsRedirection()` dans l'API | Elle n'écoute qu'en HTTP ; derrière un relais mal déclaré, la redirection boucle. | Laisser le HTTPS au serveur frontal. |
| Publier le port de l'API « pour tester » | Les en-têtes transmis peuvent alors être falsifiés. | Passer par nginx, ou `docker compose exec`. |
| `ENV ConnectionStrings__Chronos=...` ou `ARG MOT_DE_PASSE` dans un Dockerfile | Le secret est lisible dans l'image, pour toujours. | Variable ou secret fourni au démarrage. |
| `docker compose down` pour tout arrêter | Sans `--profile app`, l'API et l'interface continuent de tourner. | `docker compose --profile app down`. |
| `db.Database.Migrate()` au démarrage de l'API | Droits de schéma donnés à l'application, migration sans relecture. | Voir le module 25. |

La liste complète, avec les sources, est dans le [référentiel Docker](../docs/references/docker.md#3-pièges--ce-quon-trouve-encore-partout-et-quon-nécrit-plus).

## Pour la séance

- Qu'est-ce qui distingue l'image de l'API de ce que produit `dotnet publish` sur votre poste ? Qu'y gagne celui qui exploite l'application ?
- Pourquoi l'ordre des instructions d'un Dockerfile compte-t-il ? Montrez-le sur le vôtre.
- L'image minimale n'a ni terminal ni `curl`. Est-ce un progrès ou une gêne ? Pour qui ?
- Votre API fait confiance aux en-têtes `X-Forwarded-*`. Qui peut les écrire ? Que se passerait-il si un second relais — celui de l'hébergeur, qui termine le HTTPS — se plaçait devant nginx ?
- Variable d'environnement, secret Compose, coffre de secrets : contre quelles menaces chacun protège-t-il, et lesquelles subsistent ?
- Faut-il le même fichier Compose pour le développement et pour la production ?
- Dockerfile ou publication par le SDK : que choisiriez-vous pour une équipe qui ne fait que du .NET ?

## Pour aller plus loin

- [Images multi-plateformes](https://docs.docker.com/build/building/multi-platform/) : construire pour `amd64` et `arm64`. Déclarer `<RuntimeIdentifiers>linux-x64</RuntimeIdentifiers>` dans `Directory.Build.props`, puis régénérer les fichiers de verrouillage, rend `dotnet restore -r linux-x64 --locked-mode` possible ; la publication ne contient alors que les bibliothèques de cette plateforme (56 Mo au lieu de 84 dans l'essai).
- [Options de la publication par le SDK](https://learn.microsoft.com/en-us/dotnet/core/containers/publish-configuration) : `ContainerRepository`, `ContainerFamily`, `ContainerBaseImage`.
- [Guide Docker pour Angular](https://docs.docker.com/guides/angular/) : une configuration nginx plus complète (compression, cache des fichiers statiques). Ses tags d'images sont antérieurs à ceux du cours.
- [Contrôles de construction](https://docs.docker.com/build/checks/) : ce que vérifie `docker build --check`.
- [Ordre de démarrage dans Compose](https://docs.docker.com/compose/how-tos/startup-order/) : `depends_on`, ses conditions et leurs limites.
- [Réseaux dans Compose](https://docs.docker.com/compose/how-tos/networking/) : isoler la base sur un réseau interne.
- Suite dans le cours : le module 24 construit et publie ces images à chaque version, le module 25 les rend observables.
