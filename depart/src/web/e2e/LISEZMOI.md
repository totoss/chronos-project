# playwright.config.ts et e2e/tsconfig.json : créés par la commande du cours

Ces deux fichiers ne sont pas fournis. La commande du module 20, étape 9, `npx ng add playwright-ng-schematics`, les CRÉE (avec `e2e/example.spec.ts`) et s'arrête sur l'erreur « A merge conflicted on path » si l'un des deux existe déjà. Les autres fichiers de ce dossier ne gênent pas la commande. Supprimez ce fichier une fois ses annotations traitées.

TODO [module 20] `playwright.config.ts`, à la racine de l'application : remplacer la configuration
   générée (trois navigateurs) par un seul projet, Chromium, sur le dossier e2e ; `forbidOnly`,
   reprises (2) et `workers` (1) lus dans la variable `CI`.
   — cours/20-tester-l-interface.md, Pas à pas, étape 9 « Un parcours de bout en bout avec Playwright »

TODO [module 20] `playwright.config.ts` : rapports `list` et `html` (sans ouverture), adresse de
   base lue dans la variable `PLAYWRIGHT_TEST_BASE_URL` que `ng e2e` fournit (http://localhost:4200
   en repli), trace au premier rejeu ; supprimer e2e/example.spec.ts.
   — cours/20-tester-l-interface.md, Pas à pas, étape 9 « Un parcours de bout en bout avec Playwright »

TODO [module 20] `e2e/tsconfig.json` : écrit par la commande ; il hérite de la configuration du
   projet pour `tsc -p e2e/tsconfig.json --noEmit`, que le job `web` lance.
   — cours/20-tester-l-interface.md, Pas à pas, étape 10 « Le job `web` du workflow »
