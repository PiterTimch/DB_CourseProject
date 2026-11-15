package ua.zunu.reports.data;

import ua.zunu.reports.domain.Director;

import java.sql.SQLException;
import java.util.Optional;

public interface DirectorRepository {
    Optional<Director> findByLogin(String login) throws SQLException;
}
