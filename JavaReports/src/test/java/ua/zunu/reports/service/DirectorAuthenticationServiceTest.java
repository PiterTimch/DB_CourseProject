package ua.zunu.reports.service;

import org.junit.jupiter.api.Test;
import ua.zunu.reports.data.DirectorRepository;
import ua.zunu.reports.domain.Director;
import ua.zunu.reports.infrastructure.PasswordHashVerifier;

import java.util.Optional;

import static org.junit.jupiter.api.Assertions.*;

final class DirectorAuthenticationServiceTest {
    @Test
    void authenticates_only_director_with_correct_password() throws Exception {
        var hashes = new PasswordHashVerifier();
        var hash = hashes.hash("correct-password");
        DirectorRepository repository = login -> Optional.of(new Director(1, "Олена", "Керівник", login, "director", hash));
        var service = new DirectorAuthenticationService(repository, hashes);

        assertTrue(service.authenticate("  director.login  ", "correct-password").isPresent());
        assertTrue(service.authenticate("director.login", "wrong-password").isEmpty());

        DirectorRepository managerRepository = login -> Optional.of(new Director(2, "Марія", "Менеджер", login, "manager", hash));
        var managerService = new DirectorAuthenticationService(managerRepository, hashes);
        assertTrue(managerService.authenticate("manager.login", "correct-password").isEmpty());
    }

    @Test
    void rejects_blank_credentials_before_repository_lookup() {
        DirectorRepository repository = login -> fail("Repository must not be called for blank credentials.");
        var service = new DirectorAuthenticationService(repository, new PasswordHashVerifier());

        assertThrows(IllegalArgumentException.class, () -> service.authenticate(" ", "password"));
        assertThrows(IllegalArgumentException.class, () -> service.authenticate("login", " "));
    }
}
