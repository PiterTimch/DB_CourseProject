\set ON_ERROR_STOP on
SELECT format('DROP DATABASE IF EXISTS %I WITH (FORCE)', :'database_name')
\gexec
