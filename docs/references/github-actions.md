# Référentiel GitHub Actions — pratiques à jour

> **Vérifié le 12 novembre 2026** sur docs.github.com, le journal des changements de GitHub, les dépôts `actions/*` et `docker/*`, learn.microsoft.com, xunit.net et angular.dev.
> Ce document fait foi pour le cours : en cas de doute entre un tutoriel trouvé en ligne et ce référentiel, c'est le référentiel — ou mieux, la documentation officielle du jour — qui a raison.
> GitHub Actions a beaucoup changé en 2026. À revérifier avant d'écrire chaque module qui s'y rapporte.

## 1. Versions

### Actions

| Action | Version majeure | Dernière version | Source |
| --- | --- | --- | --- |
| `actions/checkout` | **v7** | v7.0.1 | [dépôt](https://github.com/actions/checkout) |
| `actions/setup-dotnet` | **v6** | v6.1.0 | [dépôt](https://github.com/actions/setup-dotnet) |
| `actions/setup-node` | **v7** | v7.1.0 | [dépôt](https://github.com/actions/setup-node) |
| `actions/cache` | **v6** | v6.1.0 | [dépôt](https://github.com/actions/cache) |
| `actions/upload-artifact` | **v7** | v7.0.2 | [dépôt](https://github.com/actions/upload-artifact) |
| `actions/download-artifact` | **v8** | v8.0.2 | [dépôt](https://github.com/actions/download-artifact) |
| `actions/attest` | **v4** | v4.2.2 | [dépôt](https://github.com/actions/attest) |
| `docker/setup-buildx-action` | **v4** | v4.4.1 | [dépôt](https://github.com/docker/setup-buildx-action) |
| `docker/login-action` | **v4** | v4.8.0 | [dépôt](https://github.com/docker/login-action) |
| `docker/metadata-action` | **v6** | v6.2.0 | [dépôt](https://github.com/docker/metadata-action) |
| `docker/build-push-action` | **v7** | v7.4.0 | [dépôt](https://github.com/docker/build-push-action) |

Toutes ces versions s'exécutent sur Node.js 24. **Node.js 20 a été retiré des machines d'exécution le 23 septembre 2026** : les anciennes versions majeures des actions, qui en dépendent, ne fonctionnent plus ([annonce](https://github.blog/changelog/2025-09-19-deprecation-of-node-20-on-github-actions-runners/)).

### Machines d'exécution

| Label | Système | Remarque |
| --- | --- | --- |
| `ubuntu-24.04` | Ubuntu 24.04 | Label retenu par le cours. |
| `ubuntu-26.04` | Ubuntu 26.04 | Disponible depuis le 17 septembre 2026. |
| `ubuntu-latest` | **En cours de bascule** de 24.04 vers 26.04, jusqu'au 19 novembre 2026 | À éviter : le système change sans que le workflow change. |
| `ubuntu-22.04` | Ubuntu 22.04 | Retrait le 17 avril 2027. |
| `windows-2025`, `windows-latest` | Windows Server 2025 | |
| `macos-26`, `macos-latest` | macOS 26 | |

Sources : [images](https://github.com/actions/runner-images), [machines hébergées](https://docs.github.com/en/actions/reference/runners/github-hosted-runners), [annonce Ubuntu 26.04](https://github.blog/changelog/2026-09-17-ubuntu-26-generally-available-and-latest-migration/).

Sur les images Ubuntu, Docker et son extension Compose sont préinstallés, ainsi que plusieurs SDK .NET 10 et Node.js 22 et 24. Le SDK .NET 11 n'y figure pas encore ([contenu de l'image 24.04](https://github.com/actions/runner-images/blob/main/images/ubuntu/Ubuntu2404-Readme.md)).

### Coût

- Sur un **dépôt public**, les machines standard sont gratuites.
- Sur un **dépôt privé**, chaque offre inclut un quota mensuel, exprimé en dollars : 12 $ pour l'offre gratuite. Une minute sur Linux coûte 0,006 $, sur Windows 0,010 $, sur macOS 0,062 $ ([facturation](https://docs.github.com/en/billing/concepts/product-billing/github-actions), [quotas inclus](https://docs.github.com/en/billing/reference/product-usage-included)).

Conséquence pratique : on fait tourner l'intégration continue sur Linux, sauf besoin précis.

## 2. Ce que le cours enseigne

### Structure d'un workflow

| Sujet | Pratique retenue | Source |
| --- | --- | --- |
| Emplacement | `.github/workflows/*.yml`. | [syntaxe](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax) |
| Déclencheurs | `push` sur la branche principale, `pull_request`, `workflow_dispatch`. | [événements](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows) |
| Machine | Label explicite : `ubuntu-24.04`. | [annonce](https://github.blog/changelog/2026-09-17-ubuntu-26-generally-available-and-latest-migration/) |
| Durée | `timeout-minutes` sur chaque job. Le défaut est de 6 heures. | [syntaxe](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax) |
| Exécutions redondantes | `concurrency` avec `cancel-in-progress: true`. | [syntaxe](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax) |
| Dossier de travail | `defaults.run.working-directory`. | [syntaxe](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax) |
| Étape à exécuter même après un échec | `if: ${{ !cancelled() }}` — de préférence à `always()`. | [expressions](https://docs.github.com/en/actions/reference/workflows-and-actions/expressions) |
| Passer une valeur à une étape suivante | `echo "nom=valeur" >> "$GITHUB_OUTPUT"`. | [commandes](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-commands) |
| Résumé de l'exécution | Écrire du Markdown dans `$GITHUB_STEP_SUMMARY`. | [commandes](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-commands) |
| Factoriser | Workflows réutilisables (`workflow_call`) et actions composites. | [réutilisation](https://docs.github.com/en/actions/reference/workflows-and-actions/reusing-workflow-configurations) |

### Sécurité

| Sujet | Pratique retenue | Source |
| --- | --- | --- |
| Droits du jeton | Bloc `permissions` explicite, réduit au nécessaire. Dès qu'un droit est déclaré, tous les autres sont retirés. | [syntaxe](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax) |
| Versions des actions | **Fichier de verrouillage** `.github/workflows/actions.lock`, généré par `gh actions-lock`. C'est la protection que la documentation place en tête, devant l'épinglage par empreinte. | [verrouillage](https://docs.github.com/en/actions/concepts/security/workflow-dependency-lockfiles), [usage sûr](https://docs.github.com/en/actions/reference/security/secure-use) |
| Mises à jour des actions | Dependabot, écosystème `github-actions`. | [Dependabot](https://docs.github.com/en/code-security/how-tos/secure-your-supply-chain/secure-your-dependencies/keeping-your-actions-up-to-date-with-dependabot) |
| Données non fiables | Jamais d'expression `${{ ... }}` issue d'une *pull request* ou d'un ticket dans un bloc `run`. On passe par une variable d'environnement. | [usage sûr](https://docs.github.com/en/actions/reference/security/secure-use) |
| *Pull requests* | Déclencheur `pull_request`. Pas `pull_request_target`. | [`pull_request_target`](https://docs.github.com/en/actions/reference/security/securely-using-pull_request_target) |
| Secrets de déploiement | Rattachés à un environnement protégé par une approbation. | [environnements](https://docs.github.com/en/actions/reference/workflows-and-actions/deployments-and-environments) |
| Accès à un fournisseur de cloud | OpenID Connect, à la place de secrets de longue durée. | [OIDC](https://docs.github.com/en/actions/concepts/security/openid-connect) |
| Vérification obligatoire | *Ruleset* sur la branche principale exigeant la réussite du workflow. | [rulesets](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/about-rulesets) |

### Intégration continue de l'API .NET

| Sujet | Pratique retenue | Source |
| --- | --- | --- |
| SDK | `actions/setup-dotnet` avec `global-json-file` : la version vient du dépôt, pas du workflow. | [setup-dotnet](https://github.com/actions/setup-dotnet) |
| Enchaînement | `dotnet restore`, `dotnet build --no-restore`, `dotnet test --no-build`. | [tutoriel .NET](https://docs.github.com/en/actions/tutorials/build-and-test-code/net) |
| Cache NuGet | `cache: true`, qui **exige** un fichier `packages.lock.json` ; restauration en `--locked-mode`. Constaté sur la solution de référence : si le fichier d'un projet **manque**, `--locked-mode` le recrée sans erreur — seul un fichier en désaccord avec le projet fait échouer (`NU1004`). Un `packages.lock.json` oublié au commit n'est donc pas signalé. `--force-evaluate` annule le mode verrouillé. | [setup-dotnet](https://github.com/actions/setup-dotnet) |
| Résultats des tests | Rapport TRX produit par `dotnet test --report-xunit-trx --results-directory TestResults`, envoyé comme artefact. | [xUnit et MTP](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform) |
| Formatage | `dotnet format --verify-no-changes`. | [dotnet format](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-format) |
| Tests contre une vraie base | Testcontainers : rien à configurer, Docker est présent sur les machines Ubuntu. | [Testcontainers en CI](https://dotnet.testcontainers.org/cicd/) |

### Intégration continue de l'interface Angular

```yaml
- uses: actions/checkout@v7
- uses: actions/setup-node@v7
  with:
    node-version-file: src/web/package.json
    cache: npm
    cache-dependency-path: src/web/package-lock.json
- run: npm ci
- run: npx ng test
- run: npx ng build
```

- `npm ci`, jamais `npm install` : l'installation respecte strictement `package-lock.json` ([tutoriel Node.js](https://docs.github.com/en/actions/tutorials/build-and-test-code/nodejs)).
- `ng test` s'exécute une seule fois, sans mode interactif, quand la variable `CI` est définie — ce qui est le cas sur GitHub Actions ([tests](https://angular.dev/guide/testing)).
- `ng lint` **n'est pas configuré par défaut**. Il faut d'abord `ng add angular-eslint` ([`ng lint`](https://angular.dev/cli/lint)).

> Extrait adapté des documentations de `setup-node` et de GitHub. Il sera exécuté et ajusté au module 20.

### Publier une image de conteneur

- Registre : GitHub Container Registry (`ghcr.io`), avec le jeton du workflow et la permission `packages: write`.
- Actions : `docker/login-action`, `docker/metadata-action` pour les tags, `docker/setup-buildx-action`, `docker/build-push-action`.
- Cache de construction : `cache-from: type=gha` et `cache-to: type=gha,mode=max` ([cache](https://docs.docker.com/build/ci/github-actions/cache/)).
- Provenance : `actions/attest`, avec les permissions `id-token: write` et `attestations: write` ([attestations](https://docs.github.com/en/actions/how-tos/secure-your-work/use-artifact-attestations/use-artifact-attestations)).
- L'[exemple officiel](https://docs.github.com/en/actions/tutorials/publish-packages/publish-docker-images) donne la bonne structure, mais épingle d'anciennes versions des actions Docker : on reprend la structure avec les versions du tableau ci-dessus.

## 3. Pièges : ce qu'on trouve encore partout et qu'on n'écrit plus

| Ce qu'on voit dans les tutoriels | Statut | À écrire à la place | Source |
| --- | --- | --- | --- |
| `actions/checkout@v2`, `@v3`, `@v4` | Reposent sur Node.js 12, 16 ou 20, **retirés** | `actions/checkout@v7` | [annonce](https://github.blog/changelog/2025-09-19-deprecation-of-node-20-on-github-actions-runners/) |
| `actions/upload-artifact@v3` | **Fait échouer le workflow** depuis janvier 2025 | `@v7` | [annonce](https://github.blog/changelog/2024-04-16-deprecation-notice-v3-of-the-artifact-actions/) |
| `actions/cache@v1`, `@v2` | **Ne fonctionnent plus** | `@v6` | [dépôt](https://github.com/actions/cache) |
| `runs-on: ubuntu-latest` | Valide, mais le système change en cours de route | `ubuntu-24.04` | [annonce](https://github.blog/changelog/2026-09-17-ubuntu-26-generally-available-and-latest-migration/) |
| `runs-on: ubuntu-20.04` | **Retiré** | `ubuntu-24.04` | [ticket](https://github.com/actions/runner-images/issues/11101) |
| `echo "::set-output name=x::y"` | **Déprécié** depuis 2022 | `echo "x=y" >> "$GITHUB_OUTPUT"` | [annonce](https://github.blog/changelog/2022-10-10-github-actions-deprecating-save-state-and-set-output-commands/) |
| `echo "::set-env name=X::y"`, `::add-path` | **Désactivés** depuis 2020 (faille de sécurité) | `$GITHUB_ENV`, `$GITHUB_PATH` | [annonce](https://github.blog/changelog/2020-10-01-github-actions-deprecating-set-env-and-add-path-commands/) |
| `uses: quelquun/action@main` | Référence mouvante : le code exécuté peut changer à tout moment | Fichier de verrouillage, ou empreinte de commit complète | [usage sûr](https://docs.github.com/en/actions/reference/security/secure-use) |
| Aucun bloc `permissions` | Les droits dépendent des réglages du dépôt | `permissions:` explicite | [syntaxe](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax) |
| `on: pull_request_target` suivi d'un `checkout` du code de la *pull request* | **Dangereux** ; refusé par `checkout` v7 ; l'événement sera bloqué par défaut sur les dépôts publics le 2 décembre 2026 | `on: pull_request` | [annonce](https://github.blog/changelog/2026-11-02-pull_request_target-blocked-by-default-december-2/) |
| `run: echo "${{ github.event.pull_request.title }}"` | **Injection de commandes** | Passer la valeur par `env:` puis écrire `"$TITRE"` | [usage sûr](https://docs.github.com/en/actions/reference/security/secure-use) |
| `if: always()` | Déconseillé : s'exécute même si l'exécution est annulée | `if: ${{ !cancelled() }}` | [expressions](https://docs.github.com/en/actions/reference/workflows-and-actions/expressions) |
| `dotnet test --logger trx` | Option de VSTest, **refusée** par Microsoft.Testing.Platform, que configure le modèle `xunit3` | `dotnet test --report-xunit-trx` | constaté le 12 nov. 2026 |
| `dotnet-version: 6.0.x` (ou 8, ou 9) | Versions **hors support** | `global-json-file`, ou `10.0.x` | [politique de support](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) |
| `actions/cache` sur `~/.nuget/packages` | Redondant | `setup-dotnet` avec `cache: true` | [setup-dotnet](https://github.com/actions/setup-dotnet) |
| `node-version: 18` ou `20` | Fin de vie ; Angular 22 exige au moins 22.22.3 | `node-version-file` | [compatibilité](https://angular.dev/reference/versions) |
| `npm install` | Peut modifier `package-lock.json` | `npm ci` | [tutoriel Node.js](https://docs.github.com/en/actions/tutorials/build-and-test-code/nodejs) |
| `ng test --watch=false --browsers=ChromeHeadless` | Options de Karma | `ng test` | [tests](https://angular.dev/guide/testing) |
| `docker-compose up` dans un workflow | Le binaire a été **retiré** des images en 2024 | `docker compose up` | [ticket](https://github.com/actions/runner-images/issues/9692) |
| `actions/create-release` | Dépôt **archivé** depuis 2021 | `gh release create` | [dépôt](https://github.com/actions/create-release) |
| `actions/attest-build-provenance` | Supplanté | `actions/attest` | [dépôt](https://github.com/actions/attest-build-provenance) |
| `SA_PASSWORD` pour un service SQL Server | **Déprécié** | `MSSQL_SA_PASSWORD` | [configuration](https://learn.microsoft.com/en-us/sql/linux/containers/configure?view=sql-server-ver17) |

### Le piège du filtre de chemins

Un workflow exclu par un filtre `paths` ne s'exécute pas, et sa vérification reste « en attente ». Si cette vérification est **obligatoire**, la fusion est bloquée indéfiniment. Un job sauté par une condition `if` compte au contraire comme réussi ([dépannage](https://docs.github.com/en/pull-requests/how-tos/merge-and-close-pull-requests/troubleshooting-required-status-checks)).

Le cours ne met donc pas de filtre `paths` sur un workflow dont la vérification est obligatoire.

## 4. Points à surveiller

- **La documentation de GitHub est en retard sur ses propres actions.** Le tutoriel officiel .NET montre encore `setup-dotnet@v4` et une matrice `3.1.x` / `6.0.x` ; celui de Node.js, les versions 18 et 20 ; celui des images Docker, d'anciennes empreintes. On en retient la structure, et on prend les versions dans le fichier README de chaque action.
- **Fichier de verrouillage.** Annoncé en disponibilité générale le 28 octobre 2026 ([annonce](https://github.blog/changelog/2026-10-28-github-actions-workflow-lockfiles-are-generally-available/)), alors que certaines pages de documentation le présentent encore en préversion. Il ne couvre pas les workflows réutilisables, qui s'épinglent par empreinte. La commande n'a pas été exécutée lors de la rédaction.
- **Mode de `dotnet test` — tranché.** Le modèle `xunit3` active Microsoft.Testing.Platform. L'option `--logger trx` y est refusée ; le rapport s'obtient par `--report-xunit-trx`. Lancée depuis le dossier de la solution, la commande n'a pas besoin de `--solution` (constaté).
- **Rulesets sur un dépôt privé.** Avec un compte gratuit, l'API des *rulesets* répond « Upgrade to GitHub Pro or make this repository public » (constaté le 12 novembre 2026). L'étape « rendre la vérification obligatoire » du module 05 suppose donc un dépôt public ou une offre payante.
- **Environnements et attestations sur un dépôt privé.** Avec un compte gratuit, ni les environnements protégés (relecteurs requis) ni les attestations de provenance ne sont disponibles sur un dépôt privé ([environnements](https://docs.github.com/en/actions/how-tos/deploy/configure-and-manage-deployments/manage-environments), [fonctionnalités de sécurité](https://docs.github.com/en/code-security/getting-started/github-security-features)). Le module 24 suppose donc lui aussi un dépôt public ou une offre payante. `actions/attest@v4` demande la permission `artifact-metadata: write` en plus de `id-token: write` et `attestations: write`.
- **Version de Node dans le job `web`.** `actions/setup-node` avec `node-version-file: src/web/package.json` lit, dans l'ordre, `volta.node`, `devEngines.runtime` puis `engines.node` ([usage avancé](https://github.com/actions/setup-node/blob/main/docs/advanced-usage.md)). `ng new` 22 n'écrit aucun de ces champs : c'est le bloc `devEngines` ajouté au module 13 qui est lu. Le cache npm n'est automatique que si `packageManager` figure dans un `package.json` à la racine du dépôt ; ici il faut `cache: npm` et `cache-dependency-path`.
- **Résumé des tests dans GitHub.** Une extension `Microsoft.Testing.Extensions.GitHubActionsReport` (option `--report-gh`) écrit le résultat des tests dans le résumé de l'exécution. Elle suppose le mode Microsoft.Testing.Platform ; non essayée.
- **Service SQL Server.** GitHub ne documente pas d'exemple, et la commande de contrôle de santé n'est pas vérifiée. Le cours passe par Testcontainers, qui évite la question.
- **Quotas.** La page de tarification parle de minutes incluses, la documentation de facturation d'un montant en dollars. Les chiffres ci-dessus viennent de la documentation.
- **Étapes parallèles.** Depuis juin 2026, un job peut lancer des étapes en arrière-plan (`background: true`) ou en parallèle. Le statut exact de la fonctionnalité n'est pas précisé ; le cours ne l'utilise pas.
- **Ce qui a été exécuté.** Le workflow complet du module 05 l'a été le 12 novembre 2026, sur `ubuntu-24.04`, avec `checkout@v7`, `setup-dotnet@v6` et `upload-artifact@v7` : succès en moins d'une minute, sans avertissement, cache NuGet rempli puis réutilisé, rapport TRX publié en artefact. Le verrouillage des actions l'a été aussi (voir ci-dessous). N'ont **pas** été exécutés sur GitHub : Dependabot, le *ruleset*, ni aucun des ajouts au workflow faits par les modules 06, 11, 20, 22 et 24. Leurs commandes ont été rejouées localement en conteneur, dans l'ordre du job (restauration verrouillée, format, build, tests ; `npm ci`, lint, tests, build, Playwright) ; les workflows du module 24 ont été relus contre la documentation et passés à `actionlint` 1.7.12 sans erreur, rien de plus.
- **Fichier de verrouillage — constats du 12 novembre 2026** (extension `gh-actions-lock` 0.4.1). `gh actions-lock` crée `actions.lock` **et réécrit le workflow** en resserrant `@v7` en `@v7.0.1` (`--no-narrow` l'en empêche). La machine d'exécution charge ensuite les versions depuis le fichier. Une action utilisée sans être verrouillée fait échouer l'exécution au démarrage (`startup_failure`), avant tout job ; `gh actions-lock --verify` le détecte localement.
