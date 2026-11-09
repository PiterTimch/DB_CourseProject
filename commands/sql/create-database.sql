\set ON_ERROR_STOP on
\getenv app_db_user APP_DB_USER
\getenv app_db_password APP_DB_PASSWORD
\getenv database_name PGDATABASE
SELECT format('CREATE ROLE %I LOGIN PASSWORD %L', :'app_db_user', :'app_db_password')
WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = :'app_db_user')
\gexec
SELECT format('ALTER ROLE %I WITH LOGIN PASSWORD %L', :'app_db_user', :'app_db_password')
WHERE EXISTS (SELECT 1 FROM pg_roles WHERE rolname = :'app_db_user')
\gexec
SELECT format('CREATE DATABASE %I OWNER %I ENCODING %L TEMPLATE template0', :'database_name', :'app_db_user', 'UTF8')
WHERE NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = :'database_name')
\gexec
