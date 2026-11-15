package ua.zunu.reports.service;

import ua.zunu.reports.data.DirectorRepository;
import ua.zunu.reports.domain.Director;
import ua.zunu.reports.infrastructure.PasswordHashVerifier;

import java.sql.SQLException;
import java.util.Optional;

public final class DirectorAuthenticationService {
    private final DirectorRepository directors;
    private final PasswordHashVerifier passwordHashes;

    public DirectorAuthenticationService(DirectorRepository directors, PasswordHashVerifier passwordHashes) {
        this.directors = directors;
        this.passwordHashes = passwordHashes;
    }

    public Optional<Director> authenticate(String login, String password) throws SQLException {
        if (login == null || login.isBlank() || password == null || password.isBlank()) {
            throw new IllegalArgumentException("Введіть логін і пароль.");
        }
        var director = directors.findByLogin(login.strip());
        if (director.isEmpty() || !"director".equals(director.get().role()) || !passwordHashes.verify(password, director.get().passwordHash())) {
            return Optional.empty();
        }
        return director;
    }
}
