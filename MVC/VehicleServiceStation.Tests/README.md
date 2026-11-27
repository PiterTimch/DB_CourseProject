# MVC tests

`VehicleServiceStation.Tests.csproj` is included in `VehicleServiceStation.slnx`. Its `Integration` folder contains six PostgreSQL-backed integration tests, marked with the `Integration` category.

## Run from Visual Studio

1. Start PostgreSQL and make sure the repository root `.env` contains working application database settings and the schema exists.
2. Open `VehicleServiceStation.slnx` and build the solution so Test Explorer discovers the test methods.
3. In Test Explorer, filter by `Category=Integration` to show only the database integration tests.
4. Run the six integration tests from Test Explorer.

The tests connect directly using the `.env` at the repository root and clean up the records they create. They always run when selected; a missing or unreachable database is reported as a test failure instead of a silent skip.
