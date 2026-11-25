# Команди бази даних

Перед першим запуском заповніть `PG_ADMIN_PASSWORD` у `.env`.

Windows PowerShell:

```powershell
.\commands\windows\create-database.ps1
.\commands\windows\schema.ps1
.\commands\windows\seed.ps1
.\commands\windows\populate-database.ps1
.\commands\windows\integrity-check.ps1
.\commands\windows\drop-database.ps1
```

Linux:

```bash
bash commands/linux/create-database.sh
bash commands/linux/schema.sh
bash commands/linux/seed.sh
bash commands/linux/populate-database.sh
bash commands/linux/integrity-check.sh
bash commands/linux/drop-database.sh
```

`create-database` already creates the schema, and `populate-database` already loads the seed data. The standalone `schema` and `seed` commands are available when those steps need to be run separately.

`integrity-check` prints the constraint definitions, table row counts, each integrity check with its violation count, and an overall PASS/FAIL result. It runs read-only and returns a nonzero exit code if any check fails.
