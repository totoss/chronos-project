# Module 24 — GitHub Actions : construire et publier

> Pratiques vérifiées le 12 novembre 2026 — voir le [référentiel GitHub Actions](../docs/references/github-actions.md).
> **Les workflows de ce module n'ont pas été exécutés sur GitHub.** Ils ont été relus contre la documentation du jour et le fichier README de chaque action, puis contrôlés par `actionlint` 1.7.12 (syntaxe, expressions, scripts), sans erreur. Ont été exécutés localement, le 12 novembre 2026 : la construction des deux images à partir des Dockerfile du module 23, et la fabrication puis l'exécution du bundle de migrations (EF Core 10.0.15, sur PostgreSQL 18 et SQL Server 2025). Tout le reste — publication, attestation, environnement, Dependabot — est à confirmer à la première exécution : signalez au mentor tout écart constaté.

Au module 23, vous avez construit les images de Chronos sur votre poste. Une image construite sur un poste a un défaut : personne ne peut dire de quel code elle vient, ni si ce code avait passé les tests. Ce module confie la construction à GitHub Actions : chaque version donne deux images, construites à partir d'un commit vérifié, rangées dans un registre, accompagnées d'une preuve de leur origine, et déployées après un accord explicite.

## Objectifs

À la fin de ce module, vous saurez :

- déclencher un workflow sur un tag de version ;
- réutiliser un workflow existant au lieu de le recopier ;
- construire les images de l'API et de l'interface et les publier dans le registre de GitHub ;
- nommer une image par sa version et la désigner par son empreinte ;
- attester la provenance d'une image, et vérifier cette attestation ;
- soumettre un déploiement à l'approbation d'une personne, et lui réserver ses secrets ;
- dire ce qui, dans un déploiement, dépend de l'hébergeur.

## Prérequis

- [Module 05](05-github-actions-integration-continue.md) : workflow `ci.yml`, `permissions`, `concurrency`, fichier `actions.lock`, Dependabot.
- [Module 07](07-deux-sgbd-un-seul-code.md) : les deux projets de migrations, l'outil local `dotnet-ef`.
- [Module 23](23-conteneuriser-l-application.md) : les deux Dockerfile et le contexte nommé `racine`.
- L'outil [`gh`](https://cli.github.com/), connecté à votre compte.

> **Dépôt privé et compte gratuit.** Deux fonctions de ce module ne sont proposées, avec un compte gratuit, que sur un dépôt **public** : les environnements (étape 8) et les attestations de GitHub (étape 5). Sur un dépôt privé, il faut une offre payante — GitHub Pro ou Team pour les environnements, Enterprise Cloud pour les attestations. Comme pour les *rulesets* du module 05 : faites ces étapes sur un dépôt public, ou lisez-les et discutez-en en séance. Le reste du module fonctionne partout.

## Règles de gestion

Aucune. Ce module livre l'application ; il ne la modifie pas.

## Notions

### Registre, tag, empreinte

Un **registre** stocke des images, comme GitHub stocke du code. Celui de GitHub s'appelle GitHub Container Registry : ses images se nomment `ghcr.io/<propriétaire>/<nom>`.

Une image y est désignée de deux façons :

- par un **tag**, lisible et **déplaçable** : `1.4.0`, `1.4`, `main` ;
- par son **empreinte** (*digest*), `sha256:…`, calculée sur son contenu : elle désigne une image et une seule, pour toujours.

On publie avec des tags, pour les humains. On déploie par l'empreinte, pour être certain de ce qui tourne.

Documentation : [Registre de conteneurs de GitHub](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-container-registry)

### Un tag git fait une version

Jusqu'ici, vos workflows réagissaient à un *push* sur une branche. Une version se marque par un **tag git** : `v1.4.0`. Pousser ce tag déclenche le workflow de publication, qui en déduit les tags des images (`1.4.0` et `1.4`). Le numéro suit la gestion sémantique de version : majeur, mineur, correctif.

### Workflow réutilisable

Avant de publier, il faut avoir vérifié. Recopier les jobs de `ci.yml` dans le nouveau workflow créerait deux versions de la même chose, qui divergeraient. Un workflow qui déclare l'événement `workflow_call` peut être **appelé** par un autre, comme une fonction.

Documentation : [Réutiliser des workflows](https://docs.github.com/en/actions/how-tos/reuse-automations/reuse-workflows)

### Provenance et attestation

Une **attestation de provenance** est un document signé qui dit : cette image, identifiée par son empreinte, a été construite par tel workflow, à partir de tel commit de tel dépôt. Celui qui déploie peut vérifier la signature et refuser une image dont l'origine n'est pas prouvée.

Deux mécanismes coexistent, et le module utilise les deux :

- **BuildKit**, le moteur de construction de Docker, joint à l'image une attestation de provenance et un inventaire de ses composants (*SBOM*). Ils voyagent avec l'image, dans le registre ;
- **GitHub** signe une attestation avec l'identité du workflow et la conserve. C'est elle que vérifie `gh attestation verify`.

Documentation : [Attestations d'artefacts](https://docs.github.com/en/actions/concepts/security/artifact-attestations), [attestations de construction Docker](https://docs.docker.com/build/metadata/attestations/)

### Environnement

Un **environnement** GitHub représente une cible de déploiement : `production`. Il porte trois choses :

- des **règles de protection** — par exemple l'accord d'un relecteur avant que le job ne démarre ;
- des **secrets** que seuls les jobs de cet environnement peuvent lire ;
- des **variables** non secrètes, lues par le contexte `vars`.

Le mot de passe de la base de production n'est donc jamais accessible à un workflow lancé depuis une branche de travail.

Documentation : [Déploiements et environnements](https://docs.github.com/en/actions/reference/workflows-and-actions/deployments-and-environments), [variables](https://docs.github.com/en/actions/how-tos/write-workflows/choose-what-workflows-do/use-variables)

### Des identités plutôt que des secrets

Pour agir chez un fournisseur de cloud, le réflexe est d'enregistrer une clé d'accès dans les secrets du dépôt. Cette clé ne périme pas : si elle fuit, elle sert jusqu'à ce que quelqu'un s'en aperçoive.

**OpenID Connect** l'évite. Le job présente au fournisseur un jeton d'identité émis par GitHub (« je suis tel workflow de tel dépôt »), et reçoit en échange un accès de courte durée. Il n'y a plus de secret à stocker ni à renouveler. Le job a besoin de la permission `id-token: write`.

Documentation : [OpenID Connect](https://docs.github.com/en/actions/concepts/security/openid-connect), [durcir les déploiements](https://docs.github.com/en/actions/how-tos/secure-your-work/security-harden-deployments) (Azure, AWS, Google Cloud)

## Pas à pas

### 1. Rendre la vérification réutilisable

Dans `.github/workflows/ci.yml`, ajoutez un déclencheur :

```yaml
on:
  push:
    branches: [main]
  pull_request:
  workflow_dispatch:
  workflow_call:
```

Rien d'autre ne change : le workflow continue de réagir aux *pull requests*, et peut désormais être appelé.

### 2. Créer le workflow de publication

Créez `.github/workflows/publication.yml` :

```yaml
name: Publication

on:
  push:
    branches: [main]
    tags: ['v*']
  workflow_dispatch:

permissions:
  contents: read

concurrency:
  group: images-${{ github.ref }}
  cancel-in-progress: false

jobs:
  verifications:
    name: Vérifications
    uses: ./.github/workflows/ci.yml
    permissions:
      contents: read
```

- **`tags: ['v*']`** : le workflow réagit aussi aux tags dont le nom commence par `v`. Chaque fusion dans `main` publiera une image de travail ; chaque tag, une version.
- **`cancel-in-progress: false`** : on n'interrompt pas une publication en cours. Une exécution plus récente attend.
- **Le groupe ne s'appelle pas `${{ github.workflow }}-…`** comme dans `ci.yml`. Dans un workflow appelé, `github.workflow` désigne l'appelant : les deux workflows calculeraient le même groupe et se bloqueraient l'un l'autre. Les noms de groupe ne distinguent pas majuscules et minuscules.
- **`uses: ./.github/workflows/ci.yml`** au niveau d'un job : ce job *est* le workflow appelé. Ses jobs apparaissent sous le nom « Vérifications / API ».
- **`permissions`** : un workflow appelé reçoit au plus les droits que l'appelant lui donne.

Documentation : [Contrôler la concurrence](https://docs.github.com/en/actions/how-tos/write-workflows/choose-when-workflows-run/control-workflow-concurrency)

### 3. Construire et publier l'image de l'API

Ajoutez un second job :

```yaml
  images:
    name: Image api
    needs: verifications
    runs-on: ubuntu-24.04
    timeout-minutes: 20
    permissions:
      contents: read
      packages: write
    steps:
      - name: Récupérer le code
        uses: actions/checkout@v7

      - name: Calculer le nom de l'image
        id: nom
        run: echo "image=ghcr.io/${GITHUB_REPOSITORY,,}-api" >> "$GITHUB_OUTPUT"

      - name: Préparer Buildx
        uses: docker/setup-buildx-action@v4

      - name: Se connecter au registre
        uses: docker/login-action@v4
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Calculer les tags et les labels
        id: meta
        uses: docker/metadata-action@v6
        with:
          images: ${{ steps.nom.outputs.image }}
          flavor: |
            latest=false
          tags: |
            type=semver,pattern={{version}}
            type=semver,pattern={{major}}.{{minor}}
            type=ref,event=branch
            type=sha

      - name: Construire et publier
        id: construction
        uses: docker/build-push-action@v7
        with:
          context: src/api
          build-contexts: racine=.
          push: true
          tags: ${{ steps.meta.outputs.tags }}
          labels: ${{ steps.meta.outputs.labels }}
          cache-from: type=gha,scope=api
          cache-to: type=gha,mode=max,scope=api
          provenance: mode=max
          sbom: true
```

- **`needs: verifications`** : pas d'image si les tests échouent.
- **`packages: write`** : le droit de publier dans le registre, donné à ce seul job. Le jeton `GITHUB_TOKEN` suffit ; aucun secret à créer.
- **Le nom de l'image** est `ghcr.io/<propriétaire>/<dépôt>-api`. Un nom d'image s'écrit en minuscules : `${GITHUB_REPOSITORY,,}` convertit le nom du dépôt.
- **`metadata-action`** déduit les tags de l'événement. Sur le tag git `v1.4.0` : `1.4.0`, `1.4` et `sha-` suivi du début de l'empreinte du commit. Sur une fusion dans `main` : `main` et `sha-…`. Elle produit aussi des **labels** normalisés, qui inscrivent dans l'image l'adresse du dépôt et le commit d'origine.
- **`latest=false`** : par défaut, l'action ajouterait le tag `latest` à chaque version. Le cours n'utilise jamais `latest`.
- **`context`** et **`build-contexts`** sont les deux arguments de votre commande `docker build` du module 23.
- **`cache-from` et `cache-to`** conservent les couches dans le cache de GitHub Actions : une construction qui ne change que le code ne refait pas la restauration. `scope` sépare le cache de chaque image.
- **`provenance: mode=max` et `sbom: true`** joignent à l'image les attestations de BuildKit.

Poussez sur une branche, fusionnez dans `main`, puis ouvrez la page de votre dépôt : l'image apparaît dans la rubrique *Packages*.

Documentation : [Publier des images Docker](https://docs.github.com/en/actions/tutorials/publish-packages/publish-docker-images) — bonne structure, anciennes versions d'actions ; [`metadata-action`](https://github.com/docker/metadata-action), [`build-push-action`](https://github.com/docker/build-push-action), [cache](https://docs.docker.com/build/ci/github-actions/cache/)

### 4. Deux images, un seul job : la matrice

L'interface se construit de la même façon. Plutôt que de recopier le job, décrivez ce qui diffère dans une **matrice** :

```yaml
  images:
    name: Image ${{ matrix.image }}
    needs: verifications
    runs-on: ubuntu-24.04
    timeout-minutes: 20
    permissions:
      contents: read
      packages: write
    strategy:
      fail-fast: false
      matrix:
        include:
          - image: api
            contexte: src/api
            contextes-nommes: racine=.
          - image: web
            contexte: src/web
            contextes-nommes: ""
    outputs:
      api: ${{ steps.reference.outputs.api }}
      web: ${{ steps.reference.outputs.web }}
```

GitHub lance le job une fois par entrée de `include`, en parallèle. Dans les étapes, remplacez ce qui était propre à l'API :

```yaml
      - name: Calculer le nom de l'image
        id: nom
        env:
          IMAGE: ${{ matrix.image }}
        run: echo "image=ghcr.io/${GITHUB_REPOSITORY,,}-${IMAGE}" >> "$GITHUB_OUTPUT"
```

```yaml
          context: ${{ matrix.contexte }}
          build-contexts: ${{ matrix.contextes-nommes }}
```

```yaml
          cache-from: type=gha,scope=${{ matrix.image }}
          cache-to: type=gha,mode=max,scope=${{ matrix.image }}
```

Et ajoutez une dernière étape, qui transmet aux jobs suivants la référence exacte de l'image publiée — son nom et son empreinte :

```yaml
      - name: Transmettre la référence de l'image
        id: reference
        env:
          IMAGE: ${{ matrix.image }}
          NOM: ${{ steps.nom.outputs.image }}
          EMPREINTE: ${{ steps.construction.outputs.digest }}
        run: echo "${IMAGE}=${NOM}@${EMPREINTE}" >> "$GITHUB_OUTPUT"
```

- **`fail-fast: false`** : si une image échoue, l'autre va au bout. Par défaut, GitHub annulerait tout.
- **La valeur de la matrice passe par `env`** avant d'entrer dans le script. C'est la règle du module 05 : jamais d'expression `${{ … }}` dans un bloc `run`.
- **`outputs`** : chaque exécution de la matrice renseigne la sortie qui porte son nom, `api` ou `web`.

Documentation : [Syntaxe des workflows](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax) — sections `strategy.matrix` et `jobs.<job_id>.outputs`

### 5. Attester la provenance

Ajoutez trois permissions au job `images` :

```yaml
      id-token: write
      attestations: write
      artifact-metadata: write
```

et une étape, après la construction :

```yaml
      - name: Attester la provenance
        if: ${{ !github.event.repository.private }}
        uses: actions/attest@v4
        with:
          subject-name: ${{ steps.nom.outputs.image }}
          subject-digest: ${{ steps.construction.outputs.digest }}
          push-to-registry: true
```

- **`subject-name`** est le nom complet de l'image, **sans tag** ; **`subject-digest`**, l'empreinte que vient de produire la construction. L'attestation porte sur ce contenu précis.
- **`id-token: write`** permet au job d'obtenir le jeton d'identité avec lequel l'attestation est signée. C'est le même mécanisme qu'OpenID Connect.
- **La condition `if`** saute l'étape sur un dépôt privé, où les attestations ne sont pas proposées avec un compte gratuit.

Vérifiez ensuite, depuis votre poste, une image publiée :

```powershell
gh attestation verify oci://ghcr.io/VOTRE_COMPTE/VOTRE_DEPOT-api:main -R VOTRE_COMPTE/VOTRE_DEPOT
```

La commande réussit si l'image a bien été construite par un workflow de ce dépôt. Essayez-la sur une image construite et poussée à la main : elle échoue.

Documentation : [Utiliser les attestations](https://docs.github.com/en/actions/how-tos/secure-your-work/use-artifact-attestations/use-artifact-attestations), [`actions/attest`](https://github.com/actions/attest), [`gh attestation verify`](https://cli.github.com/manual/gh_attestation_verify)

### 6. Verrouiller les nouvelles actions

Le workflow utilise cinq actions de plus. Sans mise à jour du fichier de verrouillage, GitHub refuserait de le démarrer :

```powershell
gh actions-lock
```

```powershell
gh actions-lock --verify
```

Versionnez `actions.lock` avec les workflows. Le fichier de verrouillage ne couvre pas la ligne `uses: ./.github/workflows/ci.yml` : c'est un fichier de votre propre dépôt, exécuté dans la version du commit en cours.

Complétez `.github/dependabot.yml` pour que les images de base soient suivies comme les actions :

```yaml
  - package-ecosystem: "docker"
    directories:
      - "/src/api"
      - "/src/web"
    schedule:
      interval: "weekly"

  - package-ecosystem: "docker-compose"
    directory: "/"
    schedule:
      interval: "weekly"
```

Dependabot proposera une *pull request* quand `nginx/nginx-unprivileged` ou `postgres` publieront une version plus récente que celle de vos fichiers.

Documentation : [Options de Dependabot](https://docs.github.com/en/code-security/reference/supply-chain-security/dependabot-options-reference)

### 7. Publier une version

```powershell
git tag v0.1.0
```

```powershell
git push origin v0.1.0
```

Le workflow démarre sur le tag. À la fin, la page du paquet liste les tags `0.1.0`, `0.1` et `sha-…`, qui désignent la même empreinte.

Un paquet publié pour la première fois est **privé**, même si le dépôt est public, et il est rattaché automatiquement au dépôt dont le workflow l'a publié. Pour le télécharger, il faut s'authentifier :

```powershell
docker login ghcr.io
```

```powershell
docker buildx imagetools inspect ghcr.io/VOTRE_COMPTE/VOTRE_DEPOT-api:0.1.0
```

La seconde commande affiche l'empreinte de l'image et les attestations jointes par BuildKit. La visibilité se règle dans les paramètres du paquet ; attention, un paquet rendu public ne peut plus redevenir privé.

Documentation : [Visibilité et accès d'un paquet](https://docs.github.com/en/packages/learn-github-packages/configuring-a-packages-access-control-and-visibility)

### 8. Préparer le déploiement : migrations et environnement

Une nouvelle version peut exiger un nouveau schéma. Le [module 25](25-exploiter-l-application.md) explique le **bundle de migrations** : un exécutable autonome qui applique les migrations manquantes. On le fabrique ici, à partir du même commit que les images :

```yaml
  migrations:
    name: Bundle de migrations
    needs: verifications
    runs-on: ubuntu-24.04
    timeout-minutes: 10
    defaults:
      run:
        working-directory: src/api
    steps:
      - name: Récupérer le code
        uses: actions/checkout@v7

      - name: Installer le SDK .NET
        uses: actions/setup-dotnet@v6
        with:
          global-json-file: src/api/global.json

      - name: Restaurer les outils
        run: dotnet tool restore

      - name: Construire le bundle
        run: >
          dotnet ef migrations bundle
          --project Chronos.Migrations.PostgreSql
          --startup-project Chronos.Api
          --self-contained --target-runtime linux-x64
          --output artifacts/efbundle
          -- --Chronos:Fournisseur PostgreSql

      - name: Conserver le bundle
        uses: actions/upload-artifact@v7
        with:
          name: migrations
          path: src/api/artifacts/efbundle
          if-no-files-found: error
```

`dotnet tool restore` installe `dotnet-ef` dans la version fixée par `dotnet-tools.json` (module 07). Le signe `>` replie la commande sur une ligne.

Créez ensuite l'environnement. Dans les réglages du dépôt, rubrique *Environments* :

- créez `production` ;
- cochez *Required reviewers* et désignez-vous, ou désignez votre mentor ;
- limitez les déploiements aux tags `v*` (*Deployment branches and tags*) ;
- ajoutez le secret `CHAINE_MIGRATIONS` — la chaîne de connexion du compte de migration — et les variables `FOURNISSEUR` (`PostgreSql`) et `URL_APPLICATION`.

### 9. Déployer après approbation

```yaml
  deploiement:
    name: Déploiement en production
    needs: [images, migrations]
    if: ${{ startsWith(github.ref, 'refs/tags/v') }}
    runs-on: ubuntu-24.04
    timeout-minutes: 15
    environment:
      name: production
      url: ${{ vars.URL_APPLICATION }}
    concurrency:
      group: deploiement-production
      cancel-in-progress: false
    steps:
      - name: Récupérer le bundle
        uses: actions/download-artifact@v8
        with:
          name: migrations

      - name: Appliquer les migrations
        env:
          CHAINE_MIGRATIONS: ${{ secrets.CHAINE_MIGRATIONS }}
          Chronos__Fournisseur: ${{ vars.FOURNISSEUR }}
        run: |
          chmod +x efbundle
          ./efbundle --connection "$CHAINE_MIGRATIONS"

      - name: Mettre à jour l'application
        env:
          IMAGE_API: ${{ needs.images.outputs.api }}
          IMAGE_WEB: ${{ needs.images.outputs.web }}
        run: |
          echo "À remplacer par la commande de votre hébergeur."
          echo "API : ${IMAGE_API}"
          echo "Web : ${IMAGE_WEB}"
```

- **`if`** : seul un tag de version déploie. Les fusions dans `main` s'arrêtent à la publication.
- **`environment`** : le job attend l'approbation, puis reçoit les secrets et les variables de `production`. L'adresse `url` s'affiche sur la page de l'exécution.
- **`concurrency`** : un seul déploiement à la fois vers la production.
- **Les migrations d'abord, l'application ensuite.** Le module 25 explique pourquoi cet ordre impose des migrations compatibles avec la version précédente.
- **`Chronos__Fournisseur`** : c'est à l'exécution que le bundle choisit son fournisseur, par la configuration, comme l'API (constaté).
- **`IMAGE_API`** vaut `ghcr.io/…-api@sha256:…` : l'image exacte que ce workflow vient de construire, et non « la dernière ».

Poussez un tag `v0.1.1` : les images se publient, puis l'exécution s'arrête sur *Waiting for review*. Approuvez-la.

**Ce qui dépend de l'hébergeur.** Le cours n'en impose aucun. La dernière étape est à écrire avec lui :

| Question | Exemples de réponse |
| --- | --- |
| Comment lui dire quelle image exécuter ? | Une commande de son outil ; `docker compose pull` puis `up -d` sur un serveur ; un manifeste Kubernetes. |
| Comment le workflow s'y authentifie-t-il ? | OpenID Connect si l'hébergeur le propose ; sinon un secret de l'environnement. |
| La base est-elle joignable depuis GitHub ? | Souvent non. Le bundle s'exécute alors depuis le réseau de l'hébergeur : tâche ponctuelle, ou machine d'exécution installée chez lui. |
| Comment télécharge-t-il une image privée ? | Un jeton en lecture seule sur le registre, configuré chez lui. |
| Qui porte le HTTPS ? | Lui, le plus souvent : voir les en-têtes transmis du module 23. |

Documentation : [Déployer avec GitHub Actions](https://docs.github.com/en/actions/concepts/use-cases/deploying-with-github-actions), [gérer les environnements](https://docs.github.com/en/actions/how-tos/deploy/configure-and-manage-deployments/manage-environments)

## À vous

1. **Remonter à la source.** Téléchargez une image publiée et retrouvez, avec `docker image inspect`, le commit dont elle provient. Quel label le porte ? Qui l'a écrit ? Retrouvez la même information dans la sortie de `gh attestation verify`. Laquelle des deux croiriez-vous si elles se contredisaient ?

2. **Une pré-version.** Avant de pousser le tag `v0.2.0-rc.1`, prédisez les tags d'image que produira `metadata-action` ; le fichier README de l'action donne la règle. Vérifiez. Pourquoi ce comportement est-il le bon pour `0.2` ?

3. **Deux exécutions pour une fusion.** Une fusion dans `main` exécute les tests deux fois : par le workflow `CI` et par `Publication`. Proposez deux façons de l'éviter, et dites ce que chacune change pour la vérification obligatoire du module 05.

4. **Une image de base vulnérable.** Une faille est corrigée dans l'image `aspnet:10.0`, sans que son tag change. Vos images publiées contiennent-elles le correctif ? Dependabot vous préviendra-t-il ? Que faudrait-il écrire dans les Dockerfile pour qu'il le fasse, et qu'y perdriez-vous en lisibilité ? Proposez un déclencheur qui reconstruise les images chaque semaine.

5. **Revenir en arrière.** La version `0.3.0` est en production et pose un problème. Décrivez, commande par commande, comment remettre la `0.2.1` : quelle image, désignée comment, et par quel workflow ? Que devient la base si la `0.3.0` avait ajouté une colonne ?

6. **Un environnement de recette.** Ajoutez un environnement `recette`, déployé sans approbation à chaque fusion dans `main`, avec sa propre base. Le job de déploiement ne doit pas être recopié : cherchez comment un workflow réutilisable reçoit des paramètres (`inputs`) et des secrets.

## Critères de réussite

- Une fusion dans `main` publie `…-api:main` et `…-web:main` ; un tag `v0.1.0` publie `0.1.0` et `0.1`. Aucune image ne porte le tag `latest`.
- Un test en échec empêche toute publication.
- `gh actions-lock --verify` ne signale rien.
- La seconde construction est nettement plus rapide que la première : les couches viennent du cache.
- Sur un dépôt public : `gh attestation verify` réussit sur vos images, et le déploiement attend une approbation.
- Le job de déploiement désigne les images par leur empreinte et n'utilise aucun secret défini au niveau du dépôt.
- Aucun mot de passe ne figure dans les fichiers de `.github/`.
- Comparez avec la solution de référence : `git diff module-23 module-24`.

## Pièges

| Ce qu'on voit / ce qu'on fait | Pourquoi c'est un problème | À faire à la place |
| --- | --- | --- |
| `docker/build-push-action@v5`, `docker/login-action@v3`, `actions/checkout@v4` | Anciennes versions, que montrent encore le tutoriel de GitHub et le README d'`actions/attest`. | Les versions du référentiel, puis `gh actions-lock`. |
| `actions/attest-build-provenance` | Simple enveloppe d'`actions/attest` depuis sa version 4. | `actions/attest`. |
| Un jeton personnel enregistré pour pousser sur `ghcr.io` | Secret de longue durée, lié à une personne. | `GITHUB_TOKEN` et `packages: write`. |
| `packages: write` au niveau du workflow | Tous les jobs, vérifications comprises, peuvent publier. | La permission sur le seul job qui publie. |
| `tags: ghcr.io/moi/chronos-api:latest` | On ne sait plus quelle version tourne ; un retour arrière devient une devinette. | Tags de version, déploiement par empreinte. |
| `ghcr.io/${{ github.repository }}` avec un nom de dépôt en majuscules | `invalid reference format: repository name must be lowercase`. | Convertir en minuscules. |
| `subject-name` avec un tag | L'attestation porte sur une empreinte, pas sur un tag. | Le nom seul, et `subject-digest`. |
| `cache-to: type=gha` sans `scope` dans une matrice | Chaque image écrase le cache de l'autre. | Un `scope` par image. |
| `context: src/api` sans étape `checkout` | Avec un chemin pour contexte, l'action lit les fichiers de la machine : il n'y en a pas. | `actions/checkout` d'abord. |
| Oublier `build-contexts: racine=.` | La construction de l'API échoue sur `COPY --from=racine`. | Les mêmes arguments que sur votre poste. |
| Le même groupe `concurrency` dans l'appelant et dans le workflow appelé | Les deux exécutions se bloquent ou s'annulent. | Des groupes distincts. |
| `environment:` sur le job qui appelle un workflow réutilisable | Mot-clé refusé à cet endroit. | Le déclarer dans un job du workflow appelé. |
| `needs.images.outputs.digest` avec une matrice | Les exécutions écrivent la même sortie : une seule valeur survit. | Une sortie par entrée de la matrice. |
| Déployer depuis le job qui construit | Le droit de publier et les secrets de production se retrouvent ensemble. | Un job par responsabilité. |
| Secrets de production au niveau du dépôt | Tout workflow, depuis toute branche, peut les lire. | Secrets d'environnement. |
| `cancel-in-progress: true` sur un déploiement | Un déploiement interrompu à mi-chemin : migrations appliquées, application non mise à jour. | `false`. |
| Exécuter le bundle sans `Chronos__Fournisseur` sur SQL Server | Le bundle emploie le fournisseur par défaut, PostgreSQL, et échoue sur la chaîne de connexion (constaté). | Toujours fournir la variable. |

La liste complète, avec les sources, est dans le [référentiel GitHub Actions](../docs/references/github-actions.md#3-pièges--ce-quon-trouve-encore-partout-et-quon-nécrit-plus).

## Pour la séance

- Une image construite sur votre poste et une image construite par le workflow, à partir du même commit : en quoi diffèrent-elles pour celui qui doit décider de les mettre en production ?
- Un tag d'image peut être déplacé, une empreinte non. Citez un cas où chacun est le bon outil.
- Que prouve exactement une attestation de provenance ? Que ne prouve-t-elle pas ?
- L'approbation d'un déploiement : que doit avoir vérifié la personne qui clique ? Peut-on approuver son propre déploiement ?
- Votre dépôt est privé, votre compte gratuit : ni environnement, ni attestation. Par quoi les remplaceriez-vous ? Qu'est-ce qui justifierait de payer ?
- Faut-il déployer automatiquement chaque fusion dans `main` ? De quoi auriez-vous besoin pour oser le faire ?
- OpenID Connect supprime les secrets de longue durée. Que reste-t-il à protéger ?

## Pour aller plus loin

- [Images multi-plateformes avec GitHub Actions](https://docs.docker.com/build/ci/github-actions/multi-platform/) : publier aussi pour `arm64`.
- [Docker et GitHub Actions](https://docs.docker.com/build/ci/github-actions/) : toutes les actions de Docker, et le résumé de construction.
- [Attestations avec les actions Docker](https://docs.docker.com/build/ci/github-actions/attestations/) : ce que contient la provenance selon le mode.
- [Référence des workflows réutilisables](https://docs.github.com/en/actions/reference/workflows-and-actions/reusing-workflow-configurations) : paramètres, secrets, limites.
- [Fichiers de verrouillage des workflows](https://docs.github.com/en/actions/concepts/security/workflow-dependency-lockfiles) : ce qu'ils couvrent et ce qu'ils ne couvrent pas.
- Suite dans le cours : le module 25 traite ce qui se passe après le déploiement — journaux, traces, migrations, arrêt propre.
