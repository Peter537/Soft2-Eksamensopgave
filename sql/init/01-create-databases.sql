\set ON_ERROR_STOP on

-- Creates the service databases used by MToGo.
--
-- Notes:
-- - PostgreSQL does not support `CREATE DATABASE IF NOT EXISTS`.
-- - `\gexec` is a psql feature (works with the official postgres Docker image init).

SELECT format('CREATE DATABASE %I', dbname)
FROM (
  VALUES
    ('mtogo_orders'),
    ('mtogo_agents'),
    ('mtogo_feedback'),
    ('mtogo_partners'),
    ('mtogo_legacy'),
    ('mtogo_management'),
    ('mtogo_logs')
) AS v(dbname)
WHERE NOT EXISTS (
  SELECT 1 FROM pg_database WHERE datname = v.dbname
)
\gexec
