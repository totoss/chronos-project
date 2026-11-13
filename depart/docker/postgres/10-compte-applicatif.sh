# TODO [module 25] Script d'initialisation de l'image PostgreSQL (exécuté une seule fois, à la création du volume de données, depuis le dossier
#    /docker-entrypoint-initdb.d) : créer le compte « chronos_app », qui ne peut que lire et écrire des lignes, avec le mot de passe de
#    POSTGRES_APP_PASSWORD. Lui donner la connexion à la base et l'usage du schéma public, puis, par les privilèges par défaut du compte
#    propriétaire « chronos », les droits sur les tables et les séquences que les migrations créeront. Passer le mot de passe à psql par une
#    variable de psql, jamais par concaténation. Ne pas écrire « set -e » (la raison est dans le module). Le fichier reste en fins de ligne LF
#    (voir .gitattributes)
#    — cours/25-exploiter-l-application.md, Pas à pas, étape 7 « Séparer le compte de l'application »
