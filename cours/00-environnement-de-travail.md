# Module 00 — Environnement de travail

> Pratiques vérifiées le 12 novembre 2026 — voir le [référentiel .NET 10](../docs/references/dotnet-10.md) et le [référentiel Angular 22](../docs/references/angular-22.md).
> **Les commandes d'installation de ce module n'ont pas été exécutées** : rien ne pouvait être installé sur le poste de rédaction. Leurs identifiants ont été relevés le 12 novembre 2026 sur les pages officielles citées. Ont été **exécutés**, dans des conteneurs Linux : le comportement de `global.json` avec les SDK 10.0.404 et 11.0.100 installés côte à côte ; `devEngines`, `engines` et `engine-strict` avec Node.js 24.22.3 et npm 11.19.1 ; `npx @angular/cli@22 version` (CLI 22.2.7) ; `init.defaultBranch` et `.gitattributes`. Sous Windows 11, seules des lectures ont été faites (`winget` 1.29, git 2.56.1). Restent **non exécutés** : `fnm`, `gh`, l'installation des éditeurs. Signalez au mentor tout écart constaté.

Chronos, c'est une API en .NET 10, une interface en Angular 22, deux bases de données en conteneurs et un dépôt sur GitHub. Avant d'écrire la première ligne, il faut que votre poste, celui du mentor et la machine d'intégration continue utilisent **les mêmes outils, aux mêmes versions**. Sans cela, la moitié des séances se passerait à comprendre pourquoi « ça marche chez moi ».

Ce module installe les outils, puis montre comment un projet **déclare lui-même** les versions qu'il attend.

## Objectifs

À la fin de ce module, vous saurez :

- installer et vérifier le SDK .NET 10, Node.js, git, la CLI GitHub, Docker Desktop et un éditeur ;
- expliquer pourquoi le cours utilise .NET 10 alors que .NET 11 vient de sortir ;
- fixer la version du SDK .NET d'un projet avec `global.json`, et celle de Node.js avec le champ `devEngines` ;
- configurer git : identité, branche `main`, fins de ligne ;
- créer votre dépôt personnel, avec ses fichiers `.gitignore`, `.gitattributes` et `.editorconfig`.

## Prérequis

- Un poste sous **Windows 11**, sur lequel vous pouvez installer des logiciels. C'est le poste de référence du cours : toutes les commandes sont données pour PowerShell.
- Un compte GitHub.
- Savoir utiliser git au quotidien (commit, branche, *push*).

Sous macOS ou Linux, les outils sont les mêmes et seules les commandes d'installation changent : [.NET sur macOS](https://learn.microsoft.com/en-us/dotnet/core/install/macos), [.NET sur Linux](https://learn.microsoft.com/en-us/dotnet/core/install/linux), [Node.js](https://nodejs.org/en/download), [Docker](https://docs.docker.com/get-started/get-docker/). Les commandes de vérification, elles, sont identiques.

## Règles de gestion

Aucune. Ce module prépare l'outillage ; le métier commence au module 02.

## Notions

### SDK, runtime, LTS et STS

Le **runtime** .NET exécute une application ; le **SDK** contient en plus le compilateur et la commande `dotnet`. Pour développer, on installe le SDK, qui inclut le runtime.

Microsoft publie une version majeure de .NET par an. Une sur deux est **LTS** (prise en charge trois ans), l'autre **STS** (deux ans). .NET 10 est une LTS ; .NET 11, sorti le 10 novembre 2026, est une STS. Les deux sont prises en charge jusqu'au 14 novembre 2028.

Le cours cible **.NET 10** : c'est la version qu'une entreprise déploie aujourd'hui, celle pour laquelle les bibliothèques du projet existent en version stable (EF Core 10, fournisseur PostgreSQL 10), et celle que proposent les machines d'intégration continue de GitHub.

Un numéro de SDK se lit ainsi : dans `10.0.404`, `10.0` est la version de .NET, `4` la **bande de fonctionnalités** (une par trimestre) et `04` le correctif.

Documentation : [politique de support de .NET](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core), [versions du SDK](https://learn.microsoft.com/en-us/dotnet/core/porting/versioning-sdk-msbuild-vs)

### `global.json` : le projet choisit son SDK

Plusieurs SDK cohabitent sans difficulté sur un même poste. Encore faut-il savoir lequel répond quand vous tapez `dotnet build`. Par défaut, c'est **le plus récent** — donc le 11, dès qu'il est installé.

Un fichier `global.json` inverse la décision : ce n'est plus le poste qui choisit, c'est le projet. La commande `dotnet` cherche ce fichier dans le dossier courant, puis dans chaque dossier parent, et utilise le SDK qu'il désigne.

Pour Chronos, ce fichier demandera le SDK `10.0.400` avec la politique `latestFeature` : « le SDK 10.0 le plus récent installé, à partir du 10.0.400 — et jamais un SDK 11 ».

Documentation : [global.json](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json)

### Node.js, npm et `npx`

Angular s'exécute dans le navigateur, mais ses outils — compilation, serveur de développement, tests — tournent sur **Node.js**. **npm**, installé avec Node.js, télécharge les paquets ; **`npx`** exécute la commande fournie par un paquet.

Angular 22 accepte Node.js `^22.22.3`, `^24.15.0` ou `^26.0.0`, c'est-à-dire les trois versions LTS du moment. Les versions intermédiaires (25, 27) ne sont pas acceptées. Le cours a été exécuté avec **Node.js 24**.

Comme pour .NET, le projet déclarera la version attendue : c'est le champ `devEngines` de `package.json`.

Documentation : [compatibilité des versions d'Angular](https://angular.dev/reference/versions), [versions de Node.js](https://nodejs.org/en/about/previous-releases), [`devEngines`](https://docs.npmjs.com/cli/v11/configuring-npm/package-json)

### Outil global ou outil du projet

Un outil installé **globalement** vit sur votre poste, à la version du jour de l'installation. Un outil installé **dans le projet** est inscrit dans ses fichiers : tous ceux qui clonent le dépôt obtiennent la même version.

La documentation d'Angular fait installer la CLI globalement (`npm install -g @angular/cli`). Le cours s'en passe : `npx @angular/cli@22` suffit pour créer l'application, et la CLI fait ensuite partie des dépendances du projet, où on l'appelle par `npx ng`. Vous n'aurez donc jamais une CLI globale en version 22 face à un projet passé en version 23.

Documentation : [installer la CLI Angular](https://angular.dev/tools/cli/setup-local), [`npx`](https://docs.npmjs.com/cli/v11/commands/npx)

### Fins de ligne

Windows termine une ligne par deux caractères (CRLF), Linux et macOS par un seul (LF). Si rien n'est réglé, un même fichier change entièrement d'un poste à l'autre, les comparaisons deviennent illisibles, et un script écrit sous Windows refuse de s'exécuter dans un conteneur Linux.

La règle du projet s'écrit dans un fichier versionné, `.gitattributes`. Avec `* text=auto`, git enregistre tous les fichiers texte en LF dans le dépôt, quel que soit le poste qui les a écrits.

Documentation : [gitattributes](https://git-scm.com/docs/gitattributes), [fins de ligne dans git](https://docs.github.com/en/get-started/git-basics/configuring-git-to-handle-line-endings)

### EditorConfig

Tabulations ou espaces, deux ou quatre, saut de ligne final : ces réglages dépendent de l'éditeur de chacun. Un fichier `.editorconfig`, placé à la racine du dépôt, les fixe pour tout le monde. Visual Studio et Rider le lisent nativement ; VS Code a besoin d'une extension.

Les outils .NET le lisent aussi : c'est dans ce même fichier que le [module 22](22-qualite-du-code.md) inscrira les règles de style du code C#.

Documentation : [EditorConfig](https://editorconfig.org/)

### Choisir un éditeur

Le cours n'impose pas d'éditeur. Les trois ci-dessous conviennent ; ce qui les distingue le plus, pour vous, est la licence.

| Éditeur | Pour .NET 10 | Licence |
| --- | --- | --- |
| **VS Code** avec l'extension **C# Dev Kit** | L'extension utilise le SDK installé sur le poste. | VS Code est gratuit. C# Dev Kit est gratuit pour un usage personnel, académique ou open source, et pour les équipes de 5 personnes au plus en usage commercial ; au-delà, il faut un abonnement Visual Studio Professional ou supérieur. |
| **Visual Studio 2026** | Version 18.0 ou supérieure : c'est la première qui prend officiellement en charge `net10.0`. | L'édition Community est gratuite pour un développeur individuel, pour l'enseignement, la recherche et l'open source, et pour 5 utilisateurs au plus dans une organisation qui n'est pas une « entreprise » (plus de 250 postes ou plus d'un million de dollars de chiffre d'affaires). |
| **JetBrains Rider** | Pris en charge depuis la version 2025.3 ; la version courante est la 2026.3. | Gratuit pour un usage non commercial (apprentissage, loisir, open source) ; une licence payante est exigée pour un usage commercial. |

Pour Angular, le service de langage officiel existe pour VS Code et Visual Studio, entre autres ; sa documentation ne cite pas Rider.

Documentation : [C# dans VS Code](https://code.visualstudio.com/docs/csharp/get-started), [licence de C# Dev Kit](https://code.visualstudio.com/docs/csharp/cs-dev-kit-faq), [Visual Studio Community](https://visualstudio.microsoft.com/vs/community/), [Rider](https://www.jetbrains.com/rider/), [Angular Language Service](https://angular.dev/tools/language-service)

## Pas à pas

### 1. Vérifier WinGet

WinGet est le gestionnaire de paquets de Windows 11. Il est fourni avec le système.

```powershell
winget --version
```

La commande affiche un numéro de version. Si elle est introuvable, la [documentation de WinGet](https://learn.microsoft.com/en-us/windows/package-manager/winget/) indique comment l'obtenir.

### 2. Installer le SDK .NET 10

```powershell
winget install Microsoft.DotNet.SDK.10
```

Ouvrez un **nouveau** terminal, puis :

```powershell
dotnet --list-sdks
```

La liste doit contenir une ligne qui commence par `10.0.4`. Elle peut en contenir d'autres : un SDK 11, par exemple, si un éditeur l'a installé.

```powershell
dotnet --version
```

Cette commande affiche le SDK **qui sera utilisé dans le dossier courant**. Si vous avez aussi un SDK 11, c'est lui qui s'affiche : c'est normal, et c'est l'objet de l'étape suivante.

> **Si vous utilisez Visual Studio**, installez quand même le SDK 10 séparément. Visual Studio n'installe qu'une seule copie du SDK et la remplace à chaque mise à jour, y compris par une version majeure plus récente.

Documentation : [installer .NET sur Windows](https://learn.microsoft.com/en-us/dotnet/core/install/windows), [vérifier les versions installées](https://learn.microsoft.com/en-us/dotnet/core/install/how-to-detect-installed-versions)

### 3. Comprendre `global.json`

Faites l'essai dans un dossier temporaire, hors de tout dépôt.

```powershell
mkdir $env:TEMP\essai-versions
```

```powershell
cd $env:TEMP\essai-versions
```

```powershell
dotnet new globaljson --sdk-version 10.0.400 --roll-forward latestFeature
```

La commande crée ce fichier :

```json
{
  "sdk": {
    "rollForward": "latestFeature",
    "version": "10.0.400"
  }
}
```

Comparez maintenant :

```powershell
dotnet --version
```

```powershell
cd ..
```

```powershell
dotnet --version
```

Lors de l'essai, avec les SDK 10.0.404 et 11.0.100 installés, la première commande a répondu `10.0.404` et la seconde `11.0.100`. Le fichier n'agit que **dans son dossier et ses sous-dossiers**.

Ce qu'il faut retenir du fichier :

- **`version`** est un numéro complet. `10.0` ou `10.0.x` sont refusés.
- **`rollForward`** dit quoi faire si cette version exacte n'est pas installée. Avec `latestFeature`, n'importe quel SDK 10.0 plus récent convient, et le plus récent est choisi.
- Si aucun SDK ne convient, `dotnet` s'arrête avec un message qui nomme la version demandée, le fichier `global.json` en cause et les SDK installés :

```text
A compatible .NET SDK was not found.

Requested SDK version: 10.0.500
global.json file: …\global.json

Installed SDKs:
…
```

Vous créerez le vrai `global.json` de Chronos au [module 03](03-premiere-api.md), dans `src/api/`. Conséquence de ce que vous venez d'observer : à la racine de votre dépôt, au-dessus de `src/api/`, il ne s'appliquera pas. Les commandes `dotnet` se lancent depuis `src/api/`.

### 4. Installer Node.js

Le site de Node.js propose, pour Windows, de passer par **fnm**, un gestionnaire de versions : il installe Node.js et permet d'en changer d'un projet à l'autre.

```powershell
winget install Schniz.fnm
```

fnm doit s'initialiser à chaque ouverture de terminal, ce qui se règle dans votre profil PowerShell. Créez ce fichier s'il n'existe pas encore, puis ouvrez-le :

```powershell
if (-not (Test-Path $PROFILE)) { New-Item -ItemType File -Path $PROFILE -Force }
```

```powershell
notepad $PROFILE
```

Ajoutez cette ligne à la fin du fichier, enregistrez, puis ouvrez un nouveau terminal :

```powershell
fnm env --use-on-cd --shell powershell | Out-String | Invoke-Expression
```

Installez ensuite Node.js 24 :

```powershell
fnm install --use 24
```

```powershell
node --version
```

```powershell
npm --version
```

Vous devez lire `v24.` suivi d'un numéro au moins égal à `15.0`, puis une version 11 de npm.

> **PowerShell refuse de charger le profil ou d'exécuter `npm` ?** Sur un poste Windows, l'exécution de scripts est désactivée par défaut. La documentation d'Angular donne le réglage à appliquer : `Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned`. Il ne concerne que votre compte et continue de bloquer les scripts non signés téléchargés d'Internet.

> **Sans fnm.** La [page de téléchargement](https://nodejs.org/en/download) propose aussi un installeur Windows (`.msi`). Il convient si vous n'avez qu'un projet Node.js sur votre poste ; choisissez-y la version 24.

Documentation : [télécharger Node.js](https://nodejs.org/en/download), [fnm](https://github.com/Schniz/fnm), [stratégies d'exécution de PowerShell](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_execution_policies)

### 5. Fixer la version de Node.js d'un projet

Retournez dans le dossier d'essai et créez-y un fichier `package.json` qui exige une version que vous n'avez pas :

```json
{
  "name": "essai-versions",
  "version": "0.0.0",
  "private": true,
  "devEngines": {
    "runtime": {
      "name": "node",
      "version": "^26.0.0",
      "onFail": "error"
    }
  }
}
```

```powershell
npm install
```

La commande échoue avec `npm error code EBADDEVENGINES`, et le message donne la version exigée et la version présente. Remplacez `^26.0.0` par `^24.15.0` : elle réussit.

`devEngines` décrit l'outillage attendu de **ceux qui travaillent sur le projet**. npm le contrôle avant `install`, `ci` et `run`. Vous croiserez aussi un champ plus ancien, `engines` : sans autre réglage, il n'est qu'indicatif et ne produit qu'un avertissement.

Vous ajouterez ce bloc à `src/web/package.json` au [module 13](13-premiere-application-angular.md), avec la valeur `^24.15.0` : la CLI Angular ne le crée pas. Deux outils liront alors la même information :

- **npm**, sur chaque poste, comme vous venez de le voir ;
- **l'intégration continue**, au [module 20](20-tester-l-interface.md), qui installera la version de Node.js demandée par ce fichier.

C'est le pendant de `global.json` : la version est écrite à un seul endroit, dans le dépôt. fnm, lui, ne lit pas ce champ ; si plusieurs versions de Node.js cohabitent sur votre poste, `fnm use 24` sélectionne la bonne dans le terminal courant.

Documentation : [`devEngines` et `engines`](https://docs.npmjs.com/cli/v11/configuring-npm/package-json), [commandes de fnm](https://github.com/Schniz/fnm/blob/master/docs/commands.md)

### 6. Appeler la CLI Angular sans l'installer

```powershell
npx @angular/cli@22 version
```

`npx` ne trouve pas le paquet sur votre poste : il propose de le télécharger dans le cache de npm, puis exécute la commande. Vous devez lire une version 22 de la CLI et la version de Node.js qu'elle a détectée.

Rien n'a été installé globalement. C'est par cette même forme, `npx @angular/cli@22 new`, que vous créerez l'application au module 13.

Vous pouvez supprimer le dossier d'essai :

```powershell
cd ..
```

```powershell
Remove-Item -Recurse -Force $env:TEMP\essai-versions
```

### 7. Installer et configurer git

```powershell
winget install --id Git.Git -e --source winget
```

Dans un nouveau terminal :

```powershell
git --version
```

Déclarez votre identité : elle sera inscrite dans chacun de vos commits.

```powershell
git config --global user.name "Prénom Nom"
```

```powershell
git config --global user.email "vous@exemple.fr"
```

Faites de `main` le nom de la branche initiale de vos nouveaux dépôts :

```powershell
git config --global init.defaultBranch main
```

Sans ce réglage, `git init` crée encore une branche `master` ; le changement de valeur par défaut n'est annoncé que pour Git 3.0. Or les workflows du cours visent `main`.

Enfin, les fins de ligne :

```powershell
git config --global core.autocrlf true
```

C'est la valeur recommandée sous Windows (sous macOS et Linux : `input`). L'installeur de Git pour Windows la pose en général lui-même. Elle ne vaut que pour votre poste : dans un dépôt qui possède un fichier `.gitattributes`, c'est ce fichier qui décide, pour tous les contributeurs.

Vérifiez l'ensemble, avec l'origine de chaque réglage :

```powershell
git config --list --show-origin
```

Documentation : [première configuration de git](https://git-scm.com/book/en/v2/Getting-Started-First-Time-Git-Setup), [installer git sous Windows](https://git-scm.com/install/windows)

### 8. Installer la CLI GitHub

```powershell
winget install --id GitHub.cli --source winget
```

Ouvrez une **nouvelle fenêtre** de terminal — un nouvel onglet ne suffit pas —, puis :

```powershell
gh auth login
```

La commande vous guide vers une authentification dans le navigateur et enregistre le jeton dans le coffre d'identifiants de Windows.

```powershell
gh auth status
```

Documentation : [installer `gh` sous Windows](https://github.com/cli/cli/blob/trunk/docs/install_windows.md), [`gh auth login`](https://cli.github.com/manual/gh_auth_login)

### 9. Installer Docker Desktop

Suivez la [page d'installation pour Windows](https://docs.docker.com/desktop/setup/install/windows-install/) : elle fournit l'installeur et liste les prérequis, dont WSL 2. Lisez aussi les conditions de licence, rappelées en tête du [module 01](01-docker-les-bases.md).

La vérification de l'installation est la première étape du module 01 ; on ne la refait pas ici.

### 10. Installer un éditeur

Installez celui que vous avez retenu dans le tableau des notions. Avec VS Code, ajoutez trois extensions depuis le panneau *Extensions* :

| Extension | Identifiant | Rôle |
| --- | --- | --- |
| C# Dev Kit | `ms-dotnettools.csdevkit` | Projets, débogage et tests .NET. |
| Angular Language Service | `angular.ng-template` | Complétion et erreurs dans les templates Angular. |
| EditorConfig for VS Code | `EditorConfig.EditorConfig` | Application du fichier `.editorconfig`. |

Avec Visual Studio, sélectionnez à l'installation la charge de travail *ASP.NET et développement web*.

### 11. Créer votre dépôt

Tout le cours se déroule dans **votre** dépôt. Créez-le en local :

```powershell
mkdir chronos
```

```powershell
cd chronos
```

```powershell
git init
```

Ajoutez-y trois fichiers de configuration. Les deux premiers se copient depuis le dépôt du cours.

**`.gitattributes`** — les fins de ligne :

```text
# Fins de ligne normalisées dans le dépôt, quel que soit le système du poste.
* text=auto

# Les scripts exécutés dans des conteneurs Linux doivent rester en LF.
*.sh text eol=lf
```

**`.gitignore`** — ce qui ne doit jamais être versionné : secrets (`.env`), produits de compilation (`bin/`, `obj/`, `node_modules/`, `dist/`), fichiers propres à un éditeur. Reprenez celui du dépôt du cours et lisez-le ligne à ligne : vous devez pouvoir dire pourquoi chaque entrée s'y trouve.

**`.editorconfig`** — les réglages d'édition :

```ini
root = true

[*]
charset = utf-8
indent_style = space
indent_size = 4
insert_final_newline = true
trim_trailing_whitespace = true

[*.{json,yml,yaml,xml,csproj,props,slnx,html,scss,ts}]
indent_size = 2

[*.md]
trim_trailing_whitespace = false
```

- **`root = true`** arrête ici la recherche : aucun `.editorconfig` situé plus haut sur votre disque ne s'applique.
- **`[*]`** vaut pour tous les fichiers ; les sections suivantes précisent ou corrigent.
- **Il n'y a pas de ligne `end_of_line`**, volontairement : les fins de ligne sont l'affaire de git.
- En Markdown, deux espaces en fin de ligne ont un sens : on ne les supprime pas.

Ajoutez un `README.md` d'une ligne, puis enregistrez et publiez :

```powershell
git add .
```

```powershell
git commit -m "Initialise le dépôt"
```

```powershell
gh repo create chronos --private --source=. --remote=origin --push
```

La dernière commande crée le dépôt sur GitHub à partir du dossier courant, déclare le dépôt distant `origin` et y pousse la branche. `--private` le rend privé ; le module 05 explique ce que ce choix change pour l'intégration continue.

Documentation : [`gh repo create`](https://cli.github.com/manual/gh_repo_create), [créer un dépôt](https://docs.github.com/en/repositories/creating-and-managing-repositories/creating-a-new-repository)

## À vous

1. **Dresser la fiche du poste.** Dans le `README.md` de votre dépôt, ajoutez un tableau donnant, pour chaque outil du module, la commande qui affiche sa version et la version obtenue. Comparez-le à celui du mentor : quels écarts sont sans conséquence, lesquels peuvent poser un problème ?

2. **Prédire puis vérifier.** Dans un dossier d'essai, écrivez un `global.json` qui demande la version `10.0.100` **sans** ligne `rollForward`. Avant de lancer `dotnet --version`, cherchez dans la documentation la politique appliquée par défaut et prédisez le résultat. Recommencez avec `latestMajor`. Laquelle de ces politiques ne voudriez-vous pas pour Chronos, et pourquoi ?

3. **Trouver la portée.** Créez un dossier d'essai contenant un sous-dossier `api`, et placez le `global.json` dans `api`. Depuis quel dossier `dotnet new console` crée-t-il un projet `net10.0` ? Et un projet `net11.0` ? Déduisez-en ce que devra savoir la machine d'intégration continue, qui démarre à la racine du dépôt.

4. **Comparer deux mécanismes.** Dans votre essai de l'étape 5, remplacez `devEngines` par le champ plus ancien `engines`, en exigeant toujours une version que vous n'avez pas. Que fait `npm install` ? Cherchez dans la documentation de npm le réglage `engine-strict`, activez-le dans un fichier `.npmrc` et recommencez. Lequel des deux champs lisent npm, fnm et l'action `setup-node` de GitHub ? Pourquoi le cours retient-il `devEngines` ?

5. **Suivre une fin de ligne.** Dans votre dépôt, créez un fichier texte, validez-le, puis lancez `git ls-files --eol`. Que signifient les colonnes `i/`, `w/` et `attr/` ? Ajoutez un script `essai.sh`, validez-le et relancez la commande : qu'est-ce qui diffère, et quelle ligne de `.gitattributes` en est la cause ? Supprimez ensuite ces fichiers d'essai.

6. **Lire une licence.** Pour votre situation réelle — employeur, taille de l'équipe, usage —, déterminez lequel des trois éditeurs vous pouvez utiliser gratuitement dans un cadre professionnel. Appuyez votre réponse sur les pages de licence citées, pas sur un forum.

## Critères de réussite

- `dotnet --list-sdks` affiche un SDK `10.0.4xx`.
- Dans un dossier contenant le `global.json` de l'étape 3, `dotnet --version` répond `10.0.4xx`, même si un SDK 11 est installé.
- `node --version` affiche une version 24 au moins égale à `24.15.0` ; `npx @angular/cli@22 version` affiche une CLI 22.
- `git config --global --get init.defaultBranch` répond `main` ; `gh auth status` vous montre connecté.
- `docker version` affiche un bloc `Server`.
- Votre dépôt existe sur GitHub, sa branche par défaut est `main`, et il contient `.gitignore`, `.gitattributes` et `.editorconfig`.
- `git ls-files --eol` affiche `i/lf` pour tous les fichiers texte.
- La solution de référence du cours est marquée par un tag à la fin de chaque module : comparez vos fichiers de configuration à l'état du tag `module-00`. À partir du module suivant, `git diff module-00 module-01` montrera ce que chaque module ajoute.

## Pièges

| Ce qu'on voit / ce qu'on fait | Pourquoi c'est un problème | À faire à la place |
| --- | --- | --- |
| Installer « le dernier SDK » et commencer sans `global.json` | `dotnet new` crée alors des projets `net11.0` (constaté) : les paquets 10.x du cours ne correspondent plus. | SDK 10 installé, et `global.json` dans `src/api/`. |
| `"version": "10.0"` ou `"10.0.x"` dans `global.json` | Refusé : le champ exige un numéro complet. | `"10.0.400"` et une politique `rollForward`. |
| Lancer `dotnet` depuis la racine du dépôt | Le `global.json` de `src/api/` ne s'y applique pas : c'est le SDK le plus récent qui répond. | Se placer dans `src/api/`. |
| Compter sur Visual Studio pour fournir le SDK 10 | Une mise à jour de Visual Studio remplace sa copie du SDK. | Installer le SDK 10 séparément. |
| `npm install -g @angular/cli` | La CLI globale vieillit sans qu'on y pense et finit par ne plus correspondre au projet. | `npx @angular/cli@22` pour créer, `npx ng` ensuite. |
| Installer la version « la plus récente » de Node.js | Une version impaire ou en préversion (25, 27) n'est pas acceptée par Angular 22. | Une version LTS : 24 pour ce cours. |
| `npm install typescript@latest` | La dernière version de TypeScript est la 7 ; Angular 22 exige la 6.0. | Garder la version posée à la création du projet. |
| `end_of_line = crlf` dans `.editorconfig` (présent dans des exemples officiels) | Sur une machine Linux, où git livre les fichiers en LF, `dotnet format` signale alors chaque ligne en erreur (constaté). | Aucune ligne `end_of_line` ; `.gitattributes` s'en charge. |
| Pas de `.gitattributes` | Les fins de ligne dépendent du réglage de chaque poste ; un fichier entier apparaît modifié pour rien. | `* text=auto`, versionné dès le premier commit. |
| Une branche initiale `master` | Les workflows du cours se déclenchent sur `main`. | `git config --global init.defaultBranch main`. |
| `Set-ExecutionPolicy Unrestricted`, ou `Bypass` pour toute la machine | Lève une protection pour tous les utilisateurs du poste, bien au-delà du besoin. | `RemoteSigned`, portée `CurrentUser`. |
| Installer PostgreSQL et SQL Server sur le poste | Long, permanent, et versions différentes d'un poste à l'autre. | Des conteneurs : c'est le module 01. |

## Pour la séance

- .NET 10 et .NET 11 cessent d'être pris en charge le même jour. Quels arguments restent en faveur de la version LTS ? Dans quel cas choisiriez-vous la 11 ?
- Qu'est-ce qui distingue un réglage de votre poste (`git config --global`) d'un réglage du dépôt (`.gitattributes`, `global.json`, `devEngines`) ? Lequel protège l'équipe ?
- La documentation de `global.json` conseille `rollForward: disable` quand le projet utilise des fichiers de verrouillage de paquets, ce qui sera le cas dès le module 05. Le cours garde `latestFeature`. Que gagne-t-on et que risque-t-on dans chaque cas ?
- La documentation d'Angular fait installer la CLI globalement ; le cours ne le fait pas. Qui a raison ? De quoi dépend la réponse ?
- Un nouveau collègue rejoint le projet dans six mois. Que doit-il installer, et qu'est-ce que le dépôt lui fournit sans qu'il ait à y penser ?
- Dans votre entreprise, qui décide des outils installés sur les postes ? Qu'est-ce que cela change à ce module ?

## Pour aller plus loin

- [Versions du SDK, de MSBuild et de Visual Studio](https://learn.microsoft.com/en-us/dotnet/core/porting/versioning-sdk-msbuild-vs) : quelle bande de fonctionnalités accompagne quelle version de Visual Studio, et jusqu'à quand elle est maintenue.
- [`engine-strict`](https://docs.npmjs.com/cli/v11/using-npm/config) : le réglage de npm qui rend bloquant l'ancien champ `engines`.
- [Référence de `.gitattributes`](https://git-scm.com/docs/gitattributes) : les attributs `text`, `eol` et la façon de marquer un fichier comme binaire.
- [WinGet](https://learn.microsoft.com/en-us/windows/package-manager/winget/) : `winget upgrade` pour tenir les outils à jour, `winget export` pour reproduire un poste.
- Suite dans le cours : le [module 01](01-docker-les-bases.md) démarre les deux bases en conteneurs ; le module 03 crée la solution .NET et son `global.json` ; le module 13 crée l'application Angular ; le [module 22](22-qualite-du-code.md) complète le fichier `.editorconfig`.
