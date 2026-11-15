package ua.zunu.reports.data;

import ua.zunu.reports.domain.Director;
import ua.zunu.reports.infrastructure.DatabaseConnectionFactory;

import java.sql.SQLException;
import java.util.Optional;

public final class JdbcDirectorRepository implements DirectorRepository {
    private final DatabaseConnectionFactory connections;

    public JdbcDirectorRepository(DatabaseConnectionFactory connections) {
        this.connections = connections;
    }

    @Override
    public Optional<Director> findByLogin(String login) throws SQLException {
        var sql = "SELECT employee_id, first_name, last_name, login, role, password_hash FROM employee WHERE login = ?";
        try (var connection = connections.open(); var statement = connection.prepareStatement(sql)) {
            statement.setString(1, login);
            try (var result = statement.executeQuery()) {
                if (!result.next()) {
                    return Optional.empty();
                }
                return Optional.of(new Director(
                    result.getInt("employee_id"),
                    result.getString("first_name"),
                    result.getString("last_name"),
                    result.getString("login"),
                    result.getString("role"),
                    result.getString("password_hash")
                ));
            }
        }
    }
}
