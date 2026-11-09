# Course Project Database Commands

Командні файли для PostgreSQL 18 розміщені в `commands`. Локальні налаштування беруться з `.env`; файл не додається до Git. Локальний `.env` уже створений із випадковим `APP_DB_PASSWORD`. Перед першим запуском заповніть `PG_ADMIN_PASSWORD` паролем PostgreSQL-адміністратора. Якщо створюєте `.env` з шаблону, задайте також власний випадковий `APP_DB_PASSWORD`; він використовується для створення ролі `APP_DB_USER`.

## Створення й заповнення бази

Windows PowerShell:

```powershell
if (-not (Test-Path .env)) { Copy-Item .env.example .env }
.\commands\windows\create-database.ps1
.\commands\windows\populate-database.ps1
```

Linux:

```bash
test -f .env || cp .env.example .env
bash commands/linux/create-database.sh
bash commands/linux/populate-database.sh
```

Після копіювання шаблону задайте в `.env` паролі `PG_ADMIN_PASSWORD` та `APP_DB_PASSWORD`.

Скрипт створення безпечно створює роль і базу, якщо їх ще немає, та застосовує схему. Скрипт заповнення додає демонстраційних клієнтів, працівників, автомобілі, замовлення, послуги й запчастини. У демонстраційних працівників `password_hash` дорівнює `!`, тобто це вимкнені для входу облікові записи без придатного пароля.

## Імпорт довідників

CSV мають заголовки `name,price` для послуг та `name,article,price` для запчастин. Значення з наявним унікальним ім'ям послуги або артикулом запчастини оновлюються.

Windows PowerShell:

```powershell
.\commands\windows\import-services.ps1
.\commands\windows\import-parts.ps1
.\commands\windows\import-services.ps1 -CsvPath C:\data\services.csv
.\commands\windows\import-parts.ps1 -CsvPath C:\data\parts.csv
```

Linux:

```bash
bash commands/linux/import-services.sh
bash commands/linux/import-parts.sh
bash commands/linux/import-services.sh /data/services.csv
bash commands/linux/import-parts.sh /data/parts.csv
```

Без параметра імпортуються приклади з `commands/data`.

## Звіти

Файли `commands/sql/report-orders.sql`, `report-employee-cars.sql` і `report-service-counts.sql` формують три таблиці зі специфікації.

```powershell
.\commands\windows\report-orders.ps1
.\commands\windows\report-employee-cars.ps1
.\commands\windows\report-service-counts.ps1
```

Linux:

```bash
bash commands/linux/report-orders.sh
bash commands/linux/report-employee-cars.sh
bash commands/linux/report-service-counts.sh
```

## Видалення бази

Windows PowerShell:

```powershell
.\commands\windows\drop-database.ps1
```

Linux:

```bash
bash commands/linux/drop-database.sh
```

Команда видаляє базу, вказану в `PGDATABASE`, разом з усіма її даними. Вона відмовиться працювати, якщо цільова база збігається з `PGMAINTENANCE_DB`. Роль `APP_DB_USER` при цьому залишається.
