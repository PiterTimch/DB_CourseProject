package ua.zunu.reports.infrastructure;

import ua.zunu.reports.config.AppConfig;

import java.sql.Connection;
import java.sql.DriverManager;
import java.sql.SQLException;
import java.util.Properties;

public final class DatabaseConnectionFactory {
    private final AppConfig config;

    public DatabaseConnectionFactory(AppConfig config) {
        this.config = config;
    }

    public Connection open() throws SQLException {
        var url = "jdbc:postgresql://" + config.host() + ":" + config.port() + "/" + config.database() + "?connectTimeout=5&socketTimeout=15&ApplicationName=ServiceCenterReports";
        var properties = new Properties();
        properties.setProperty("user", config.appUser());
        properties.setProperty("password", config.appPassword());
        return DriverManager.getConnection(url, properties);
    }
}
