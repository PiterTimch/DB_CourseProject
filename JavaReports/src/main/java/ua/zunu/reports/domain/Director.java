package ua.zunu.reports.domain;

public record Director(int employeeId, String firstName, String lastName, String login, String role, String passwordHash) {
}
