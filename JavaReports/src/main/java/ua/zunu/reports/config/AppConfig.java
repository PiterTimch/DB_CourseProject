package ua.zunu.reports.config;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.HashMap;
import java.util.Map;

public record AppConfig(String host, int port, String database, String appUser, String appPassword, String demoDirectorPassword, Path repositoryRoot) {
    public static AppConfig load() throws IOException {
        var root = findRepositoryRoot();
        var values = readEnvironment(root.resolve(".env"));
        var host = required(values, "PGHOST");
        var portValue = required(values, "PGPORT");
        int port;
        try {
            port = Integer.parseInt(portValue);
        } catch (NumberFormatException exception) {
            throw new IllegalStateException("PGPORT must be a valid TCP port.", exception);
        }
        if (port < 1 || port > 65535) {
            throw new IllegalStateException("PGPORT must be a valid TCP port.");
        }
        return new AppConfig(
            host,
            port,
            required(values, "PGDATABASE"),
            required(values, "APP_DB_USER"),
            required(values, "APP_DB_PASSWORD"),
            values.getOrDefault("DEMO_DIRECTOR_PASSWORD", ""),
            root
        );
    }

    private static Path findRepositoryRoot() {
        var current = Path.of(System.getProperty("user.dir")).toAbsolutePath();
        while (current != null) {
            if (Files.isRegularFile(current.resolve(".env")) && Files.isDirectory(current.resolve("commands"))) {
                return current;
            }
            current = current.getParent();
        }
        throw new IllegalStateException("The shared .env file was not found at the repository root.");
    }

    private static Map<String, String> readEnvironment(Path file) throws IOException {
        if (!Files.isRegularFile(file)) {
            throw new IllegalStateException("Create .env in the repository root from .env.example.");
        }
        var values = new HashMap<String, String>();
        for (var line : Files.readAllLines(file)) {
            var entry = line.trim();
            if (entry.isEmpty() || entry.startsWith("#")) {
                continue;
            }
            var separator = entry.indexOf('=');
            if (separator < 1) {
                continue;
            }
            var key = entry.substring(0, separator).trim();
            var value = entry.substring(separator + 1).trim();
            if (value.length() >= 2 && ((value.startsWith("\"") && value.endsWith("\"")) || (value.startsWith("'") && value.endsWith("'")))) {
                value = value.substring(1, value.length() - 1);
            }
            values.put(key, System.getenv().getOrDefault(key, value));
        }
        return values;
    }

    private static String required(Map<String, String> values, String key) {
        var value = values.get(key);
        if (value == null || value.isBlank()) {
            throw new IllegalStateException("Set " + key + " in the repository .env file.");
        }
        return value;
    }
}
