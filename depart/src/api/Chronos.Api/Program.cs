// Fichier d'assemblage de l'API : chaque module y branche ses services (avant builder.Build()), puis ses middlewares
// et ses routes (après), puis les tâches de démarrage. Les annotations ci-dessous suivent l'ordre du fichier final.
// Les « using » nécessaires s'ajoutent au fil des modules : laissez l'éditeur les proposer.

var builder = WebApplication.CreateBuilder(args);

// TODO [module 25] Lire en configuration les secrets montés par l'hébergeur : si la clé « Chronos:DossierDesSecrets »
//    est renseignée, ajouter le fournisseur « une valeur par fichier » sur ce dossier (fichiers facultatifs ; « __ »
//    tient lieu de « : » dans un nom de fichier). Sans dossier configuré, rien ne change. À placer juste après
//    CreateBuilder, avant tout autre usage de la configuration.
//    — cours/25-exploiter-l-application.md, Pas à pas, étape 12 « Le premier administrateur » (passage « Le mot de passe, en secret »)

// TODO [module 03] Enregistrer la génération du document OpenAPI.
//    — cours/03-premiere-api.md, Pas à pas, étape 3 « Lire ce que le modèle a généré »

// TODO [module 08] Activer la validation automatique des DTO par annotations de données.
//    — cours/08-une-api-robuste.md, Pas à pas, étape 2 « Activer la validation et regarder ce qu'elle fait »

// TODO [module 08] Activer les ProblemDetails : toute erreur d'API prend le même format.
//    — cours/08-une-api-robuste.md, Pas à pas, étape 3 « Le même format pour toutes les erreurs »

// TODO [module 08] Enregistrer le gestionnaire d'exceptions des règles de gestion (422), sous les ProblemDetails.
//    — cours/08-une-api-robuste.md, Pas à pas, étape 4 « Une règle de gestion violée : 422 »

// TODO [module 08] Enregistrer le gestionnaire d'exceptions des conflits (409), après celui des règles de gestion :
//    les gestionnaires sont appelés dans l'ordre de leur enregistrement.
//    — cours/08-une-api-robuste.md, Pas à pas, étape 5 « Créer un collaborateur : 201, 409 ou 422 »

// TODO [module 08] Enregistrer l'horloge du système comme service (TimeProvider), pour que la date du jour soit
//    une donnée que les tests peuvent piloter.
//    — cours/08-une-api-robuste.md, Pas à pas, étape 6 « Paginer, filtrer, trier »

// TODO [module 03] Faire écrire les énumérations en toutes lettres dans le JSON, et refuser les rangs numériques.
//    — cours/03-premiere-api.md, Pas à pas, étape 10 « Des énumérations lisibles » (voir aussi À vous, exercice 2)

// TODO [module 03] Les services provisoires de l'étape 7 (stockages en mémoire des projets, puis des clients) s'enregistrent
//    ici, sous la ligne OpenAPI. Ces classes (ProjetsEnMemoire.cs, ClientsEnMemoire.cs) n'existent plus dans l'état final :
//    leurs lignes d'enregistrement disparaissent au module 06.
//    — cours/03-premiere-api.md, Pas à pas, étape 7 « Une liste en mémoire, fournie par injection » (clients : À vous, exercice 4)

// TODO [module 12] Rendre la requête HTTP en cours accessible aux services (IHttpContextAccessor).
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 7 « Autoriser par périmètre (RG-50, RG-51) »

// TODO [module 12] Enregistrer, par requête, l'utilisateur courant (interface IUtilisateurCourant du module Infrastructure,
//    implémentée par la classe Securite/UtilisateurCourant) puis le périmètre de l'utilisateur (Securite/PerimetreUtilisateur).
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 7 « Autoriser par périmètre (RG-50, RG-51) »

// TODO [module 12] Enregistrer, par requête, l'intercepteur de traçabilité (RG-53) de l'infrastructure.
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 9 « Tracer (RG-53) »

// TODO [module 07] Choisir le fournisseur de base de données par configuration : clé « Chronos:Fournisseur »,
//    « PostgreSql » par défaut, « SqlServer » admis ; tout autre nom arrête le démarrage avec un message clair,
//    avant l'enregistrement du contexte. Lire la chaîne de connexion « Chronos » une seule fois.
//    — cours/07-deux-sgbd-un-seul-code.md, Pas à pas, étape 3 « Choisir le fournisseur par configuration »

// TODO [module 06] Enregistrer le contexte EF Core ChronosDbContext (du projet Infrastructure) avec la chaîne de connexion
//    « Chronos », lue dans les secrets utilisateur en développement. Au module 06 le fournisseur est PostgreSQL seul.
//    — cours/06-persister-avec-ef-core.md, Pas à pas, étape 4 « Enregistrer le contexte et la chaîne de connexion »
// TODO [module 07] Le contexte devient fonction du fournisseur choisi : chaque branche désigne le projet de migrations
//    de son SGBD (Chronos.Migrations.PostgreSql, Chronos.Migrations.SqlServer).
//    — cours/07-deux-sgbd-un-seul-code.md, Pas à pas, étape 3 « Choisir le fournisseur par configuration »
// TODO [module 10] Activer sur les deux fournisseurs la stratégie d'exécution qui rejoue les opérations transitoires
//    (« stratégie d'exécution »), qu'exige la transaction explicite de RG-41 et RG-43.
//    — cours/10-temps-et-absences.md, Pas à pas, étape 7 « RG-41 : deux demandes au même instant »
// TODO [module 12] Le bloc de configuration du contexte reçoit désormais le fournisseur de services ; y ajouter, après le
//    choix du fournisseur et quel qu'il soit, l'intercepteur de traçabilité (RG-53).
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 9 « Tracer (RG-53) »
// TODO [module 06] Alimenter la base de développement avec les données initiales (DonneesInitiales, projet Infrastructure),
//    en version synchrone et asynchrone, au module 06 uniquement en environnement Development.
//    — cours/06-persister-avec-ef-core.md, Pas à pas, étape 8 « Alimenter la base de développement »
// TODO [module 25] Les données de démonstration ne dépendent plus du nom de l'environnement mais d'un réglage explicite,
//    la clé booléenne « Chronos:Demo:Donnees » (posée dans appsettings.Development.json).
//    — cours/25-exploiter-l-application.md, Pas à pas, étape 11 « Données de démonstration : un réglage explicite » (manque exposé par l'encadré « Compléter le déploiement »)

// TODO [module 12] Brancher ASP.NET Core Identity sur le contexte : points d'accès d'API pour le type Utilisateur, adresse
//    électronique unique, rôles à clé entière, fabrique de claims Securite/FabriqueDeClaims.
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 2 « Brancher Identity »
// TODO [module 25] Ramener à une minute l'intervalle de validation du tampon de sécurité, pour qu'un compte verrouillé ou
//    dont les rôles changent perde sa session rapidement.
//    — cours/25-exploiter-l-application.md, Pas à pas, étape 13 « Gérer les comptes »
// TODO [module 12] Configurer le cookie de session : nom « chronos.session », HttpOnly, SameSite strict, Secure partout
//    sauf en développement, durée de 8 heures glissante.
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 2 « Brancher Identity »
// TODO [module 12] Déclarer l'autorisation : politique de repli (tout exige un utilisateur connecté, RG-50) et les quatre
//    politiques nommées dans Securite/Politiques (GestionRh, Validation, Pilotage, Administration), fondées sur les rôles
//    de Securite/Roles (RG-52).
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 3 « Tout protéger, n'exposer que la connexion » (politiques :
//    étape 6 « Autoriser par rôle (RG-52) »)
// TODO [module 12] Configurer l'anti-CSRF : en-tête « X-XSRF-TOKEN », cookie du jeton sécurisé selon la requête.
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 5 « Le jeton anti-CSRF »

// TODO [module 23] Déclarer les contrôles de santé (au module 23, un point « /health » sans contrôle).
//    — cours/23-conteneuriser-l-application.md, Pas à pas, étape 1 « Donner à l'API un point de contrôle de santé »
// TODO [module 25] Les contrôles de santé vérifient désormais la base (contrôle du contexte EF Core) : « /alive » dit que
//    le processus tourne, « /health » qu'il peut servir.
//    — cours/25-exploiter-l-application.md, Pas à pas, étape 4 « Deux points de contrôle de santé »

// TODO [module 25] Exporter les journaux en OpenTelemetry (messages formatés, portées incluses).
//    — cours/25-exploiter-l-application.md, Pas à pas, étape 5 « Produire traces et métriques »
// TODO [module 25] Enregistrer en singleton la métrique métier des feuilles de temps (FeuillesDeTemps/MetriquesFeuillesDeTemps).
//    — cours/25-exploiter-l-application.md, À vous, exercice 3 « Une métrique métier. »
// TODO [module 25] Déclarer la télémétrie : ressource « chronos-api », traces (instrumentation ASP.NET Core sans les routes
//    de santé, client HTTP, Npgsql), métriques (ASP.NET Core, client HTTP, exécution, Npgsql, métrique métier des feuilles),
//    et l'exportateur OTLP seulement si la variable OTEL_EXPORTER_OTLP_ENDPOINT est renseignée.
//    — cours/25-exploiter-l-application.md, Pas à pas, étape 5 « Produire traces et métriques »

// TODO [module 23] Faire lire à l'API l'adresse et le protocole d'origine transmis par le relais (en-têtes X-Forwarded-For
//    et X-Forwarded-Proto), seulement s'ils viennent du réseau désigné par la clé « Chronos:ReseauDuRelais ».
//    — cours/23-conteneuriser-l-application.md, Pas à pas, étape 9 « Faire confiance au relais »

// TODO [module 23] Persister les clés de protection des données (cookie de session, jetons anti-CSRF) dans le dossier de
//    la clé « Chronos:DossierDesCles », nom d'application « Chronos », pour que les sessions survivent à un redéploiement.
//    — cours/23-conteneuriser-l-application.md, À vous, exercice 6 « Rester connecté. »

var app = builder.Build();

// TODO [module 23] Appliquer les en-têtes transmis par le relais, en tout premier dans le pipeline.
//    — cours/23-conteneuriser-l-application.md, Pas à pas, étape 9 « Faire confiance au relais »

// TODO [module 08] Gérer les exceptions à un seul endroit : une erreur de lecture de requête (BadHttpRequestException) garde
//    son code d'état d'origine, toute autre exception donne 500 ; puis donner un corps aux réponses d'erreur sans corps.
//    — cours/08-une-api-robuste.md, Pas à pas, étape 3 « Le même format pour toutes les erreurs »

// TODO [module 12] Ajouter, dans cet ordre, l'authentification puis l'autorisation.
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 3 « Tout protéger, n'exposer que la connexion »

// TODO [module 25] Ouvrir pour chaque requête une portée de journal portant l'identifiant technique du compte (« anonyme »
//    sans connexion), jamais son adresse électronique.
//    — cours/25-exploiter-l-application.md, Pas à pas, étape 2 « Attacher l'utilisateur à tous les messages d'une requête »

// TODO [module 03] En développement seulement : publier le document OpenAPI puis l'interface Scalar.
//    — cours/03-premiere-api.md, Pas à pas, étape 3 « Lire ce que le modèle a généré » (OpenAPI) et étape 11 « Explorer l'API avec Scalar »
// TODO [module 12] Ces deux routes restent lisibles sans connexion (documentation sans donnée) : les rendre anonymes.
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 3 « Tout protéger, n'exposer que la connexion »

// TODO [module 23] Exposer « /health », anonyme (sinon la politique de repli répondrait 401).
//    — cours/23-conteneuriser-l-application.md, Pas à pas, étape 1 « Donner à l'API un point de contrôle de santé »
// TODO [module 25] Exposer aussi « /alive », anonyme, sans exécuter aucun contrôle.
//    — cours/25-exploiter-l-application.md, Pas à pas, étape 4 « Deux points de contrôle de santé »

// TODO [module 12] Exposer la seule connexion d'Identity sous /api/compte, anonyme : tout autre chemin du groupe répond 404
//    (liste d'autorisation, par un filtre d'endpoint).
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 3 « Tout protéger, n'exposer que la connexion »

// TODO [module 03] Brancher les routes des projets (MapProjets), puis celles des clients (MapClients), directement sur l'application : il n'y a pas
//    encore de groupe racine ni d'authentification.
//    — cours/03-premiere-api.md, Pas à pas, étape 8 « Les endpoints de lecture » (clients : À vous, exercice 4 « Une deuxième ressource. »)

// TODO [module 06] Brancher les routes des collaborateurs (MapCollaborateurs), après celles des projets et des clients.
//    — cours/06-persister-avec-ef-core.md, À vous, exercice 3 « Lister les collaborateurs. »

// TODO [module 09] Brancher les routes des affectations (MapAffectations), des jours fériés (MapJoursFeries) puis du plan de charge (MapPlanDeCharge).
//    — cours/09-affectations-et-calcul-de-charge.md, Pas à pas, étape 9 « `GET /api/plan-de-charge` » (jours fériés : À vous, exercice 2 « Les jours fériés. » ; affectations : même étape 9, passage « Il reste à créer des affectations »)

// TODO [module 10] Brancher les routes des feuilles de temps (MapFeuillesDeTemps), des absences (MapAbsences) puis des types d'absence (MapTypesAbsence).
//    — cours/10-temps-et-absences.md, Pas à pas, étape 4 « Les endpoints de la feuille » et étape 7 « RG-41 : deux demandes au même instant » ; types d'absence : À vous, exercice 1 « Compléter l'API. »

// TODO [module 12] Regrouper toutes ces routes sous un groupe racine dont toute écriture exige le jeton anti-CSRF (Securite/FiltreAntiforgery), dans cet ordre
//    à l'état final : MapCompte (étape 3), MapComptes (À vous, exercice 5), MapProjets, MapClients, MapCollaborateurs, MapAffectations, MapJoursFeries,
//    MapPlanDeCharge, MapFeuillesDeTemps, MapAbsences, MapTypesAbsence.
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 5 « Le jeton anti-CSRF »

// TODO [module 25] Créer le premier administrateur au démarrage, d'après la configuration (Securite/AdministrateurInitial).
//    — cours/25-exploiter-l-application.md, Pas à pas, étape 12 « Le premier administrateur » (manque exposé par l'encadré « Compléter le déploiement »)

// TODO [module 12] En développement, si la clé « Chronos:Demo:MotDePasse » est renseignée (secrets utilisateur), créer les
//    comptes de démonstration (Securite/ComptesDeDemonstration).
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 11 « Des comptes pour le développement »

app.Run();
