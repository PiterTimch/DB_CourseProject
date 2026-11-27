package ua.zunu.reports;

import javafx.application.Application;
import javafx.scene.control.Alert;
import javafx.stage.Stage;
import ua.zunu.reports.config.AppConfig;
import ua.zunu.reports.data.JdbcDirectorRepository;
import ua.zunu.reports.data.JdbcReportRepository;
import ua.zunu.reports.domain.Director;
import ua.zunu.reports.export.ReportPdfExporter;
import ua.zunu.reports.infrastructure.CommandRunner;
import ua.zunu.reports.infrastructure.DatabaseBootstrapper;
import ua.zunu.reports.infrastructure.DatabaseConnectionFactory;
import ua.zunu.reports.infrastructure.PasswordHashVerifier;
import ua.zunu.reports.service.DirectorAuthenticationService;
import ua.zunu.reports.service.ReportService;
import ua.zunu.reports.ui.DashboardView;
import ua.zunu.reports.ui.LoginView;

public final class ReportApplication extends Application {
    private static DirectorAuthenticationService authentication;
    private ReportService reports;
    private ReportPdfExporter pdfExporter;

    @Override
    public void start(Stage stage) {
        try {
            var config = AppConfig.load();
            var connections = new DatabaseConnectionFactory(config);
            var commands = new CommandRunner(config.repositoryRoot());
            var hashes = new PasswordHashVerifier();
            new DatabaseBootstrapper(config, connections, commands, hashes).initialize();
            authentication = new DirectorAuthenticationService(new JdbcDirectorRepository(connections), hashes);
            reports = new ReportService(new JdbcReportRepository(connections));
            pdfExporter = new ReportPdfExporter();
            new LoginView(authentication).show(stage, director -> openDashboard(stage, director));
        } catch (Exception exception) {
            var alert = new Alert(Alert.AlertType.ERROR);
            alert.setTitle("Garage · Помилка запуску");
            alert.setHeaderText("Не вдалося підготувати застосунок звітів");
            alert.setContentText(exception.getMessage());
            alert.showAndWait();
            stage.close();
        }
    }

    private void openDashboard(Stage stage, Director director) {
        new DashboardView(reports, pdfExporter).show(stage, director);
    }

    public static DirectorAuthenticationService authentication() {
        return authentication;
    }

    public static void main(String[] args) {
        launch(args);
    }
}
