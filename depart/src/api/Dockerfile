# TODO [module 23] Dockerfile en deux étapes pour l'API. Étape « build » (image du SDK .NET 10) : copier d'abord les seuls fichiers qui décrivent les
#    dépendances (fichiers communs de la solution, .editorconfig pris dans le contexte nommé « racine », fichier de projet et fichier de
#    verrouillage des cinq projets nécessaires à l'API, tests exclus), restaurer en mode verrouillé avec un cache NuGet monté ; puis copier les
#    sources et publier sans restaurer dans /app. Étape finale (image ASP.NET .NET 10) : installer curl pour le contrôle de santé, exposer le
#    port 8080, copier /app, exécuter sous l'utilisateur non privilégié $APP_UID, point d'entrée en forme tableau. Première ligne : directive de
#    syntaxe du Dockerfile. Ne pas restaurer avec l'option d'architecture : elle est incompatible avec le mode verrouillé.
#    — cours/23-conteneuriser-l-application.md, Pas à pas, étape 3 « Écrire le Dockerfile de l'API »
# TODO [module 23] Faire survivre les sessions à un redéploiement : avant la ligne USER, créer le dossier des clés de protection des données et
#    le donner à $APP_UID, pour qu'un volume nommé monté dessus en hérite
#    — cours/23-conteneuriser-l-application.md, À vous, exercice 6 « Rester connecté. »

# Les fichiers packages.lock.json que ce Dockerfile copie sont produits au module 05 (étape 7 « Accélérer avec un cache ») : ils n'existent pas encore.
