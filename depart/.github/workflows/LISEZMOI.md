# Workflows GitHub Actions : ce qu'il vous reste à écrire

Ce dossier ne contient aucun fichier YAML, volontairement : un workflow sans `jobs` est refusé par GitHub, et un `dependabot.yml` vide l'est aussi.
Les annotations ci-dessous tiennent lieu de celles qu'on aurait mises en tête des fichiers. Chaque fichier se crée **au chemin indiqué**.
Les noms de jobs viennent du cours : le *ruleset* du module 05 exige la vérification nommée `API`.

Les références `cours/…` sont relatives à la racine du dépôt du cours.

## `.github/workflows/ci.yml`

- TODO [module 05] Créer le workflow `CI` : déclencheurs `push` sur `main`, `pull_request` et `workflow_dispatch` ; un job `api` (nom affiché `API`) sur une machine Ubuntu désignée par sa version (pas `latest`)
  — cours/05-github-actions-integration-continue.md, Pas à pas, étape 1 « Un premier workflow »
- TODO [module 05] Faire de ce job la compilation et les tests de l'API : récupérer le code, installer le SDK en lisant `global.json`, restaurer, compiler en `Release`, tester sans recompiler ; limite de durée du job, dossier de travail `src/api`
  — cours/05-github-actions-integration-continue.md, Pas à pas, étape 2 « Compiler et tester l'API »
- TODO [module 05] Demander à `dotnet test` un rapport TRX et le conserver comme artefact, même quand les tests échouent
  — cours/05-github-actions-integration-continue.md, Pas à pas, étape 4 « Conserver les résultats des tests »
- TODO [module 05] Annuler les exécutions devenues inutiles sur une même branche (groupe de concurrence)
  — cours/05-github-actions-integration-continue.md, Pas à pas, étape 5 « Ne pas gaspiller de machines »
- TODO [module 05] Limiter les droits du jeton du workflow à la lecture du contenu
  — cours/05-github-actions-integration-continue.md, Pas à pas, étape 6 « Limiter les droits du workflow »
- TODO [module 05] Mettre en cache les paquets NuGet (cache de `setup-dotnet` fondé sur les fichiers `packages.lock.json`) et restaurer en mode verrouillé. Cela suppose d'avoir activé le verrouillage dans `Directory.Build.props`
  — cours/05-github-actions-integration-continue.md, Pas à pas, étape 7 « Accélérer avec un cache »
- TODO [module 05] Verrouiller les actions utilisées (voir `actions.lock` plus bas)
  — cours/05-github-actions-integration-continue.md, Pas à pas, étape 8 « Maîtriser les actions exécutées »
- TODO [module 05] Rendre la vérification `API` obligatoire par un *ruleset* de branche (disponible sur dépôt privé seulement avec une offre payante)
  — cours/05-github-actions-integration-continue.md, Pas à pas, étape 9 « Rendre la vérification obligatoire »
- TODO [module 06] Après la compilation, restaurer les outils locaux puis exécuter la vérification « le modèle n'a pas de changement sans migration », sans recompiler (attention : pour `dotnet ef`, `-c` abrège `--context`)
  — cours/06-persister-avec-ef-core.md, À vous, exercice 5 « Faire vérifier les migrations par l'intégration continue. »
- TODO [module 07] Compléter cette étape pour qu'elle vérifie les deux jeux de migrations (PostgreSQL et SQL Server), sans service de base de données
  — cours/07-deux-sgbd-un-seul-code.md, À vous, exercice 4 « Deux vérifications dans l'intégration continue. »
- TODO [module 11] Remplacer l'étape de test unique par deux étapes, tests unitaires puis tests d'intégration, qui écrivent leurs résultats dans le même dossier ; aucune action nouvelle
  — cours/11-tests-d-integration.md, Pas à pas, étape 9 « Dans l'intégration continue »
- TODO [module 20] Ajouter à la suite du job `api` un job `web` (nom affiché `Web`) : version de Node.js lue dans `package.json`, cache npm, installation stricte, tests, compilation, vérification des types des parcours, Chromium pour Playwright, tests de bout en bout, rapport conservé en artefact
  — cours/20-tester-l-interface.md, Pas à pas, étape 10 « Le job `web` du workflow »
- TODO [module 22] Insérer, entre la restauration et la compilation du job `api`, une étape qui vérifie le formatage sans rien corriger ni restaurer
  — cours/22-qualite-du-code.md, Pas à pas, étape 7 « Faire respecter les règles par le job `api` »
- TODO [module 22] Ajouter au job `web`, juste après l'installation des dépendances et avant les tests, une vérification Prettier et une analyse ESLint
  — cours/22-qualite-du-code.md, Pas à pas, étape 12 « Faire respecter les règles par le job `web` »
- TODO [module 24] Ajouter le déclencheur `workflow_call` pour que `publication.yml` puisse appeler ce workflow
  — cours/24-github-actions-construire-et-publier.md, Pas à pas, étape 1 « Rendre la vérification réutilisable »

## `.github/workflows/actions.lock`

Fichier **généré** par `gh actions-lock` (extension `github/gh-actions-lock`) : ne l'écrivez jamais à la main.

- TODO [module 05] Installer l'extension, lancer la commande de verrouillage, la vérifier avec `--verify`, puis versionner `actions.lock` avec le workflow ; la relancer à chaque ajout ou retrait d'action
  — cours/05-github-actions-integration-continue.md, Pas à pas, étape 8 « Maîtriser les actions exécutées »
- TODO [module 20] Relancer le verrouillage : le job `web` utilise une action nouvelle (installation de Node.js)
  — cours/20-tester-l-interface.md, Pas à pas, étape 10 « Le job `web` du workflow »
- TODO [module 24] Relancer le verrouillage : le workflow de publication utilise six actions de plus
  — cours/24-github-actions-construire-et-publier.md, Pas à pas, étape 6 « Verrouiller les nouvelles actions »

## `.github/workflows/publication.yml`

- TODO [module 24] Créer le workflow `Publication` : déclencheurs `push` sur `main` et sur les tags `v*`, lancement manuel ; droits de lecture ; groupe de concurrence propre, qui n'annule pas une publication en cours ; un premier job `verifications` qui appelle `ci.yml`
  — cours/24-github-actions-construire-et-publier.md, Pas à pas, étape 2 « Créer le workflow de publication »
- TODO [module 24] Ajouter le job `images`, qui construit l'image de l'API et la publie dans le registre de paquets du dépôt, après les vérifications
  — cours/24-github-actions-construire-et-publier.md, Pas à pas, étape 3 « Construire et publier l'image de l'API »
- TODO [module 24] Faire de `images` un job à matrice, qui construit l'image de l'API et celle de l'interface
  — cours/24-github-actions-construire-et-publier.md, Pas à pas, étape 4 « Deux images, un seul job : la matrice »
- TODO [module 24] Attester la provenance des images (trois permissions de plus et une étape après la construction)
  — cours/24-github-actions-construire-et-publier.md, Pas à pas, étape 5 « Attester la provenance »
- TODO [module 24] Ajouter le job `migrations`, qui fabrique le bundle de migrations à partir du même commit : sur une machine neuve, les outils et les paquets doivent être restaurés (en mode verrouillé) avant la fabrication, faute de quoi elle échoue
  — cours/24-github-actions-construire-et-publier.md, Pas à pas, étape 8 « Préparer le déploiement : migrations et environnement »
- TODO [module 24] Ajouter le job `deploiement`, réservé aux tags `v*`, dans l'environnement `production` protégé par une approbation
  — cours/24-github-actions-construire-et-publier.md, Pas à pas, étape 9 « Déployer après approbation »

## `.github/dependabot.yml`

À créer **à côté** du dossier `workflows/`, et non dedans.

- TODO [module 05] Créer la configuration de Dependabot (version 2) : mise à jour hebdomadaire des actions GitHub, à la racine du dépôt
  — cours/05-github-actions-integration-continue.md, Pas à pas, étape 8 « Maîtriser les actions exécutées »
- TODO [module 24] Ajouter le suivi hebdomadaire des images de base des deux Dockerfile (`/src/api` et `/src/web`) et du fichier Compose de la racine
  — cours/24-github-actions-construire-et-publier.md, Pas à pas, étape 6 « Verrouiller les nouvelles actions »
