package ua.zunu.reports.infrastructure;

import ua.zunu.reports.config.AppConfig;

import java.io.IOException;
import java.sql.SQLException;

public final class DatabaseBootstrapper {
    private final AppConfig config;
    private final DatabaseConnectionFactory connections;
    private final CommandRunner commands;
    private final PasswordHashVerifier passwordHashes;

    public DatabaseBootstrapper(AppConfig config, DatabaseConnectionFactory connections, CommandRunner commands, PasswordHashVerifier passwordHashes) {
        this.config = config;
        this.connections = connections;
        this.commands = commands;
        this.passwordHashes = passwordHashes;
    }

    public void initialize() throws Exception {
        try {
            connections.open().close();
        } catch (SQLException exception) {
            if (!isInitializationError(exception)) {
                throw exception;
            }
            commands.run("create-database");
        }
        if (!schemaExists()) {
            commands.run("create-database");
        }
        if (!hasApplicationPrivileges()) {
            commands.run("create-database");
            if (!hasApplicationPrivileges()) {
                throw new IllegalStateException("The application database user still lacks table access after initialization.");
            }
        }
        if (!hasData()) {
            commands.run("populate-database");
        }
        ensureSeededDirectorPassword();
        try (var connection = connections.open(); var statement = connection.createStatement()) {
            statement.executeQuery("SELECT 1");
        }
    }

    private boolean schemaExists() throws SQLException {
        var sql = "SELECT to_regclass('public.customer') IS NOT NULL AND to_regclass('public.employee') IS NOT NULL AND to_regclass('public.car') IS NOT NULL AND to_regclass('public.orders') IS NOT NULL AND to_regclass('public.service') IS NOT NULL AND to_regclass('public.part') IS NOT NULL AND to_regclass('public.order_service') IS NOT NULL AND to_regclass('public.order_part') IS NOT NULL";
        try (var connection = connections.open(); var statement = connection.createStatement(); var result = statement.executeQuery(sql)) {
            return result.next() && result.getBoolean(1);
        }
    }

    private boolean hasApplicationPrivileges() throws SQLException {
        try (var connection = connections.open(); var statement = connection.createStatement(); var result = statement.executeQuery("SELECT has_table_privilege(current_user, 'customer', 'SELECT')")) {
            return result.next() && result.getBoolean(1);
        } catch (SQLException exception) {
            if ("42501".equals(exception.getSQLState())) {
                return false;
            }
            throw exception;
        }
    }

    private boolean hasData() throws SQLException {
        var sql = "SELECT EXISTS (SELECT 1 FROM customer UNION ALL SELECT 1 FROM employee UNION ALL SELECT 1 FROM car UNION ALL SELECT 1 FROM orders UNION ALL SELECT 1 FROM service UNION ALL SELECT 1 FROM part UNION ALL SELECT 1 FROM order_service UNION ALL SELECT 1 FROM order_part)";
        try (var connection = connections.open(); var statement = connection.createStatement(); var result = statement.executeQuery(sql)) {
            return result.next() && result.getBoolean(1);
        }
    }

    private void ensureSeededDirectorPassword() throws SQLException {
        var currentHash = "";
        try (var connection = connections.open(); var statement = connection.prepareStatement("SELECT password_hash FROM employee WHERE login = 'demo.director' AND role = 'director'")) {
            try (var result = statement.executeQuery()) {
                if (!result.next()) {
                    return;
                }
                currentHash = result.getString(1);
            }
        }
        if (!"!".equals(currentHash)) {
            return;
        }
        if (config.demoDirectorPassword().isBlank()) {
            throw new IllegalStateException("Set DEMO_DIRECTOR_PASSWORD in the shared .env file to enable the seeded director account.");
        }
        try (var connection = connections.open(); var statement = connection.prepareStatement("UPDATE employee SET password_hash = ? WHERE login = 'demo.director' AND role = 'director' AND password_hash = '!'")) {
            statement.setString(1, passwordHashes.hash(config.demoDirectorPassword()));
            statement.executeUpdate();
        }
    }

    private static boolean isInitializationError(SQLException exception) {
        for (var current = exception; current != null; current = current.getNextException()) {
            if ("3D000".equals(current.getSQLState()) || "28P01".equals(current.getSQLState())) {
                return true;
            }
        }
        return false;
    }
}
