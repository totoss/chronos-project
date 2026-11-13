#!/bin/sh
# Exécuté une seule fois par l'image PostgreSQL, à la création du volume de données
# (dossier /docker-entrypoint-initdb.d). Il crée le compte de l'application, qui ne peut que lire et écrire des
# lignes. Le compte « chronos » (POSTGRES_USER) reste celui des migrations : il possède le schéma.
# Pas de « set -e » ici : si le fichier n'est pas exécutable, l'image le charge (« source ») et l'option vaudrait pour tout son
# script d'entrée.

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" \
    -v mot_de_passe="$POSTGRES_APP_PASSWORD" -v base="$POSTGRES_DB" -v proprietaire="$POSTGRES_USER" <<'SQL'
CREATE ROLE chronos_app LOGIN PASSWORD :'mot_de_passe';
GRANT CONNECT ON DATABASE :"base" TO chronos_app;
GRANT USAGE ON SCHEMA public TO chronos_app;
-- Les tables et séquences que les prochaines migrations créeront, sous le compte propriétaire.
ALTER DEFAULT PRIVILEGES FOR ROLE :"proprietaire" IN SCHEMA public
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO chronos_app;
ALTER DEFAULT PRIVILEGES FOR ROLE :"proprietaire" IN SCHEMA public
    GRANT USAGE, SELECT ON SEQUENCES TO chronos_app;
SQL
