# Module 01 — Docker : les bases

> Pratiques vérifiées le 12 novembre 2026 — voir le [référentiel Docker](../docs/references/docker.md).
> Les commandes de ce module ont été **exécutées le 12 novembre 2026** sous Windows 11, avec Docker Desktop 4.99 (Engine 29.9, Compose 5.7). Le fichier `compose.yaml` qu'il fait écrire est celui de la racine du dépôt.

Chronos a besoin de deux bases de données : PostgreSQL et SQL Server. Les installer toutes les deux sur son poste est long, laisse des services tourner en permanence, et rien ne garantit que le mentoré et le mentor aient la même version. Docker règle ces trois problèmes : chaque base tourne dans un **conteneur**, démarré par une commande, identique sur tous les postes, et supprimé sans laisser de trace.

## Objectifs

À la fin de ce module, vous saurez :

- expliquer ce que sont une image, un conteneur, un volume et un port publié ;
- démarrer, inspecter, arrêter et supprimer un conteneur en ligne de commande ;
- dire où vont les données d'une base en conteneur, et dans quels cas elles sont perdues ;
- décrire les deux bases du projet dans un fichier `compose.yaml` et les piloter avec `docker compose` ;
- garder les mots de passe hors du dépôt git.

## Prérequis

- Module 00 : Docker Desktop est installé et démarré.
- Savoir utiliser un terminal PowerShell.

Si Docker Desktop n'est pas encore installé : la [page d'installation pour Windows](https://docs.docker.com/desktop/setup/install/windows-install/) donne les prérequis (WSL 2, virtualisation). Vérifiez aussi les [conditions de licence](https://docs.docker.com/subscription/desktop-license/) : au-delà de 250 salariés ou de 10 millions de dollars de chiffre d'affaires, votre employeur doit disposer d'un abonnement.

## Règles de gestion

Aucune. Ce module prépare l'outillage ; le métier commence au module 02.

## Notions

### Image et conteneur

Une **image** est un modèle figé, en lecture seule : un système de fichiers contenant un logiciel et tout ce qu'il lui faut pour tourner. `postgres:18` est une image.

Un **conteneur** est une exécution de cette image : un processus isolé, avec son propre système de fichiers, son propre réseau et ses propres variables d'environnement.

Si vous venez de la programmation objet, le rapport est le même qu'entre une classe et ses instances : on peut créer plusieurs conteneurs à partir d'une image, chacun avec son état, et supprimer un conteneur ne touche pas à l'image.

Documentation : [Qu'est-ce qu'une image ?](https://docs.docker.com/get-started/docker-concepts/the-basics/what-is-an-image/), [Qu'est-ce qu'un conteneur ?](https://docs.docker.com/get-started/docker-concepts/the-basics/what-is-a-container/)

### Tag

Le nom d'une image se termine par un **tag** qui désigne une version : dans `postgres:18`, le tag est `18`. Sans tag, Docker prend `latest`, c'est-à-dire « la plus récente au moment du téléchargement ».

On écrit **toujours** le tag. Le jour où PostgreSQL 19 sortira, `postgres:latest` changera de version majeure sans prévenir, et deux développeurs qui ont téléchargé l'image à une semaine d'écart n'auront plus la même base.

### Port publié

Un conteneur a son propre réseau. PostgreSQL y écoute sur le port 5432, mais ce port n'est pas accessible depuis votre poste tant qu'il n'est pas **publié**.

`-p 127.0.0.1:5432:5432` se lit de droite à gauche : le port 5432 du conteneur est relié au port 5432 de votre poste, sur l'adresse `127.0.0.1` uniquement.

Cette adresse a son importance. Sans elle (`-p 5432:5432`), Docker relie le port à toutes les interfaces réseau : la base devient joignable par les autres machines du réseau, ce qu'on ne veut pas pour une base de développement.

### Volume

Le système de fichiers d'un conteneur disparaît avec lui. Pour une base de données, c'est un problème : supprimer le conteneur supprimerait les données.

Un **volume** est un espace de stockage géré par Docker, qui existe indépendamment des conteneurs. On le **monte** à l'endroit où la base écrit ses fichiers. Le conteneur peut alors être supprimé et recréé : les données restent dans le volume.

Documentation : [Volumes](https://docs.docker.com/engine/storage/volumes/)

### Compose

Démarrer une base demande une commande longue : image, mot de passe, port, volume. Deux bases, c'est deux commandes longues à retenir et à transmettre aux collègues.

**Docker Compose** remplace ces commandes par un fichier, `compose.yaml`, qui décrit les conteneurs dont le projet a besoin. Le fichier est versionné avec le code : quiconque clone le dépôt démarre le même environnement avec `docker compose up`.

Documentation : [Comment fonctionne Compose](https://docs.docker.com/compose/intro/compose-application-model/)

## Pas à pas

### 1. Vérifier l'installation

```powershell
docker version
```

```powershell
docker compose version
```

La première commande affiche deux blocs, `Client` et `Server`. Si le bloc `Server` est remplacé par une erreur, Docker Desktop n'est pas démarré.

La seconde doit afficher une version 5 ou supérieure. Notez la commande : `docker compose`, avec une espace. `docker-compose`, avec un tiret, est l'ancienne version, qui n'est plus maintenue.

### 2. Démarrer un premier conteneur

```powershell
docker run --name pg-essai -e POSTGRES_PASSWORD=essai-local -p 127.0.0.1:5432:5432 -d postgres:18
```

Décomposons :

| Élément | Rôle |
| --- | --- |
| `docker run` | Crée un conteneur et le démarre. |
| `--name pg-essai` | Donne un nom au conteneur, pour le désigner ensuite. |
| `-e POSTGRES_PASSWORD=essai-local` | Définit une variable d'environnement dans le conteneur. L'image `postgres` l'exige : c'est le mot de passe de l'administrateur. |
| `-p 127.0.0.1:5432:5432` | Publie le port. |
| `-d` | Détache : le conteneur tourne en arrière-plan et la commande rend la main. |
| `postgres:18` | L'image, avec son tag. |

À la première exécution, Docker télécharge l'image. Les suivantes sont immédiates.

### 3. Observer le conteneur

```powershell
docker ps
```

Liste les conteneurs en cours d'exécution. `docker ps -a` montre aussi ceux qui sont arrêtés.

```powershell
docker logs pg-essai
```

Affiche ce que le processus a écrit. **C'est le premier réflexe quand un conteneur ne se comporte pas comme prévu.**

```powershell
docker exec -it pg-essai psql -U postgres
```

Lance une commande *dans* le conteneur — ici `psql`, le client en ligne de commande de PostgreSQL, qui est fourni avec l'image. Vous n'avez donc rien à installer sur votre poste. `-it` rend la session interactive.

Dans `psql`, créez une table et une ligne, puis quittez :

```sql
CREATE TABLE essai (id int PRIMARY KEY, libelle text);
INSERT INTO essai VALUES (1, 'bonjour');
\q
```

### 4. Arrêter, redémarrer, supprimer

```powershell
docker stop pg-essai
```

```powershell
docker start pg-essai
```

Après un arrêt et un redémarrage, la table `essai` est toujours là : le conteneur existait encore, son système de fichiers aussi.

```powershell
docker rm -f pg-essai
```

`rm` supprime le conteneur ; `-f` l'arrête d'abord s'il tourne. Relancez maintenant la commande `docker run` de l'étape 2, puis reconnectez-vous avec `psql` : la table a disparu. Le nouveau conteneur est reparti de l'image.

Supprimez ce second conteneur avant de continuer :

```powershell
docker rm -f -v pg-essai
```

Pourquoi `-v` cette fois ? L'image `postgres` déclare un volume pour ses données. Quand vous n'en fournissez pas, Docker en crée un **anonyme**, au nom illisible, que `docker rm` laisse derrière lui. Le premier conteneur en a donc abandonné un :

```powershell
docker volume ls
```

L'option `-v` de `docker rm` supprime le volume anonyme avec le conteneur. Supprimez celui qui reste avec `docker volume rm` suivi de son nom.

### 5. Conserver les données avec un volume

```powershell
docker run --name pg-essai -e POSTGRES_PASSWORD=essai-local -p 127.0.0.1:5432:5432 -v pg-essai-donnees:/var/lib/postgresql -d postgres:18
```

L'ajout est `-v pg-essai-donnees:/var/lib/postgresql` : le volume nommé `pg-essai-donnees` est monté sur le dossier où PostgreSQL range ses fichiers. Docker crée le volume s'il n'existe pas.

Recréez la table, supprimez le conteneur avec `docker rm -f pg-essai`, relancez la même commande `docker run` : cette fois, la table est toujours là.

> **Le chemin a changé avec PostgreSQL 18.** Jusqu'à la version 17, le volume se montait sur `/var/lib/postgresql/data`, et c'est ce que montrent presque tous les tutoriels. À partir de la 18, c'est `/var/lib/postgresql` ([fiche de l'image](https://hub.docker.com/_/postgres)). Avec l'ancien chemin sur une image 18, les données ne sont pas conservées.

Faites le ménage :

```powershell
docker rm -f pg-essai
```

```powershell
docker volume rm pg-essai-donnees
```

### 6. Décrire les deux bases avec Compose

À la racine du dépôt, créez `compose.yaml` :

```yaml
name: chronos

services:
  postgres:
    image: postgres:18
    environment:
      POSTGRES_USER: chronos
      POSTGRES_DB: chronos
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:?POSTGRES_PASSWORD manquant, voir .env.example}
    ports:
      - "127.0.0.1:5432:5432"
    volumes:
      - postgres-donnees:/var/lib/postgresql
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U $${POSTGRES_USER} -d $${POSTGRES_DB}"]
      interval: 10s
      timeout: 10s
      retries: 5
      start_period: 30s

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2025-latest
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: ${MSSQL_SA_PASSWORD:?MSSQL_SA_PASSWORD manquant, voir .env.example}
    ports:
      - "127.0.0.1:1433:1433"
    volumes:
      - sqlserver-donnees:/var/opt/mssql
    healthcheck:
      test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -S localhost -C -U sa -P \"$${MSSQL_SA_PASSWORD}\" -Q \"SELECT 1\" -b"]
      interval: 10s
      timeout: 10s
      retries: 10
      start_period: 30s

volumes:
  postgres-donnees:
  sqlserver-donnees:
```

Ce qu'il faut y lire :

- **`name`** nomme le projet. Compose s'en sert pour préfixer les conteneurs et les volumes.
- **Pas de ligne `version`.** Elle est obsolète et provoque un avertissement.
- **`services`** : un service par conteneur. On retrouve, sous forme de clés, les options de `docker run` — `image`, `environment` (`-e`), `ports` (`-p`), `volumes` (`-v`).
- **`${POSTGRES_PASSWORD:?...}`** : Compose remplace cette expression par la valeur de la variable, et **refuse de démarrer** avec le message indiqué si elle n'est pas définie. Le mot de passe n'est donc pas écrit dans le fichier.
- **`healthcheck`** : une commande que Docker exécute régulièrement dans le conteneur pour savoir si le service répond. Un conteneur démarré n'est pas une base prête : PostgreSQL met quelques secondes à accepter des connexions, SQL Server nettement plus.
- **`$$`** : dans un fichier Compose, `$$` produit un `$` littéral. La variable est ainsi lue *dans le conteneur* au moment du contrôle, et non remplacée par Compose.
- **`ACCEPT_EULA: "Y"`** : SQL Server exige l'acceptation explicite de sa licence. Les guillemets évitent que YAML interprète `Y` comme un booléen.
- **`volumes`**, à la racine : la déclaration des volumes nommés.

Sans autre réglage, l'image démarre en édition *Enterprise Developer* : gratuite pour le développement et les tests, et **interdite en production**.

### 7. Sortir les mots de passe du dépôt

Compose lit automatiquement un fichier `.env` placé à côté de `compose.yaml`. Créez-le :

```dotenv
POSTGRES_PASSWORD=choisissez-un-mot-de-passe
MSSQL_SA_PASSWORD=Choisissez-1-MotDePasse
```

SQL Server impose ses règles : au moins 8 caractères, pris dans trois familles parmi majuscules, minuscules, chiffres et symboles. Si le mot de passe est trop faible, le conteneur s'arrête au démarrage et la raison se trouve dans ses journaux.

Ce fichier **ne doit pas être versionné**. Le `.gitignore` du dépôt l'exclut déjà ; vérifiez-le :

```powershell
git status --short
```

`.env` ne doit pas apparaître dans la liste. Créez en revanche un fichier `.env.example`, versionné, qui indique les variables attendues sans leurs valeurs :

```dotenv
POSTGRES_PASSWORD=
MSSQL_SA_PASSWORD=
```

Un nouveau venu copie `.env.example` en `.env` et le remplit.

### 8. Piloter l'environnement

```powershell
docker compose up -d --wait
```

Crée et démarre tous les services. `-d` les détache ; `--wait` attend que les contrôles de santé soient verts avant de rendre la main. Quand la commande se termine sans erreur, les deux bases sont prêtes.

La première fois, Docker télécharge l'image de SQL Server, qui pèse plus de 2 Go : prévoyez quelques minutes. Les démarrages suivants prennent une trentaine de secondes.

```powershell
docker compose ps
```

La colonne d'état doit indiquer `healthy` pour les deux services.

```powershell
docker compose logs -f sqlserver
```

Suit les journaux d'un service (`Ctrl+C` pour arrêter de suivre).

```powershell
docker compose exec postgres psql -U chronos -d chronos
```

```powershell
docker compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -C -U sa
```

Ouvre un client dans chaque base. `sqlcmd` demande le mot de passe ; l'option `-C` accepte le certificat auto-signé du conteneur, sans quoi la connexion chiffrée échoue.

```powershell
docker compose up -d --wait postgres
```

Ne démarre qu'un service. Utile au quotidien : SQL Server réclame au moins 2 Go de mémoire, inutile de le lancer quand on travaille sur PostgreSQL.

```powershell
docker compose stop
```

Arrête les conteneurs sans les supprimer.

```powershell
docker compose down
```

Arrête et supprime les conteneurs et le réseau. **Les volumes, donc les données, sont conservés.**

> **`docker compose down -v` supprime aussi les volumes.** C'est la bonne commande pour repartir d'une base vide, et la mauvaise si vous vouliez seulement « nettoyer ». Beaucoup de tutoriels la donnent sans avertissement.

## À vous

1. **Lire des journaux.** Démarrez l'environnement et retrouvez, dans les journaux de chaque base, la ligne qui indique qu'elle accepte les connexions. Combien de temps sépare le démarrage du conteneur de cette ligne, pour l'une et pour l'autre ?

2. **Suivre les données.** Créez une table dans PostgreSQL. Prédisez, *avant d'exécuter*, si elle survivra à chacune de ces commandes, puis vérifiez : `docker compose stop` suivi de `up` ; `docker compose down` suivi de `up` ; `docker compose down -v` suivi de `up`.

3. **Provoquer une panne.** Donnez à SQL Server un mot de passe trop simple dans `.env`, puis lancez `docker compose up -d --wait`. Que se passe-t-il ? Retrouvez la cause sans chercher sur Internet, uniquement avec les commandes de ce module.

4. **Résoudre un conflit de port.** Supposez que le port 5432 de votre poste soit déjà pris par un PostgreSQL installé localement. Modifiez `compose.yaml` pour publier la base du projet sur le port 5433. Qu'est-ce qui change pour une application qui s'y connecte depuis votre poste ? Et pour une application qui tournerait dans un autre conteneur du même fichier Compose ?

5. **Initialiser une base.** Les tests d'intégration auront besoin d'une seconde base, `chronos_test`, dans le même serveur PostgreSQL. L'image `postgres` exécute au premier démarrage les scripts placés dans `/docker-entrypoint-initdb.d`. Faites en sorte que cette base soit créée automatiquement. Pourquoi votre script ne s'exécute-t-il pas si vous relancez simplement `docker compose up` ?

## Critères de réussite

- `docker compose up -d --wait` se termine sans erreur et `docker compose ps` affiche les deux services en `healthy`.
- Vous vous connectez à chacune des deux bases avec le client fourni dans son conteneur.
- `compose.yaml` ne contient ni ligne `version`, ni tag `latest`, ni mot de passe.
- `git status` ne liste pas `.env` ; `.env.example` est versionné.
- Vous savez dire, sans l'essayer, ce que deviennent les données après `down` et après `down -v`.

## Pièges

| Ce qu'on voit dans les tutoriels | Pourquoi on ne l'écrit plus |
| --- | --- |
| `docker-compose up` | C'est Compose v1, qui n'est plus maintenu. La commande est `docker compose`. |
| `version: '3.8'` en tête de fichier | Obsolète ; Compose l'ignore et affiche un avertissement. |
| `docker-compose.yml` | Encore accepté, mais le nom recommandé est `compose.yaml`. |
| `image: postgres` | Sans tag, c'est `latest` : la version change sans prévenir. |
| `ports: ["5432:5432"]` | Expose la base à tout le réseau. On préfixe par `127.0.0.1:`. |
| Volume sur `/var/lib/postgresql/data` | Faux à partir de PostgreSQL 18. |
| `SA_PASSWORD` | Dépréciée au profit de `MSSQL_SA_PASSWORD`. |
| `/opt/mssql-tools/bin/sqlcmd` | Ancien chemin ; c'est désormais `/opt/mssql-tools18/bin/sqlcmd`. |
| `depends_on` sans condition | N'attend pas que la base soit prête, seulement que le conteneur soit lancé. |

La liste complète, avec les sources, est dans le [référentiel Docker](../docs/references/docker.md#3-pièges--ce-quon-trouve-encore-partout-et-quon-nécrit-plus).

## Pour la séance

- Quelle différence faites-vous entre arrêter un conteneur, le supprimer, et supprimer son volume ?
- Pourquoi versionner `compose.yaml` mais pas `.env` ? Que se passerait-il si un mot de passe était versionné par erreur, puis retiré dans un commit suivant ?
- `postgres:18` désigne-t-il toujours exactement la même image ? Quand cela pose-t-il un problème, et comment s'en prémunir ?
- Un conteneur « démarré » et un service « prêt » : quelle différence, et qui en a besoin ?
- Quels arguments donneriez-vous pour et contre l'installation directe des bases sur le poste ?
- Dans votre entreprise, a-t-on le droit d'utiliser Docker Desktop gratuitement ?

## Pour aller plus loin

- [Profils Compose](https://docs.docker.com/compose/how-tos/profiles/) : rendre un service facultatif — par exemple ne démarrer SQL Server que sur demande.
- [Secrets Compose](https://docs.docker.com/compose/how-tos/use-secrets/) : transmettre un mot de passe par un fichier monté plutôt que par une variable d'environnement.
- [`docker debug`](https://docs.docker.com/reference/cli/docker/debug/) : ouvrir un terminal dans un conteneur qui n'en contient pas.
- [`docker system prune`](https://docs.docker.com/reference/cli/docker/system/prune/) : récupérer de l'espace disque — lisez ce que la commande supprime avant de la lancer.
- Suite dans le cours : le module 11 utilise Docker depuis les tests (Testcontainers), le module 23 construit les images de l'API et de l'interface.
