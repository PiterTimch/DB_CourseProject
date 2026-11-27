package ua.zunu.reports.integration;

import org.junit.jupiter.api.Test;
import ua.zunu.reports.config.AppConfig;
import ua.zunu.reports.data.JdbcDirectorRepository;
import ua.zunu.reports.data.JdbcReportRepository;
import ua.zunu.reports.domain.ReportResult;
import ua.zunu.reports.domain.ReportType;
import ua.zunu.reports.infrastructure.DatabaseConnectionFactory;
import ua.zunu.reports.infrastructure.PasswordHashVerifier;
import ua.zunu.reports.service.DirectorAuthenticationService;
import ua.zunu.reports.service.ReportService;

import java.io.IOException;
import java.sql.SQLException;
import java.time.LocalDate;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertTrue;
import static org.junit.jupiter.api.Assumptions.assumeTrue;

final class ReportIntegrationTest {
    private static final LocalDate FROM = LocalDate.of(2000, 1, 1);
    private static final LocalDate TO = LocalDate.of(2100, 1, 1);

    @Test
    void ordersReportReadsEightColumnsFromPostgresql() throws Exception {
        var report = load(ReportType.ORDERS);
        assertShape(report, ReportType.ORDERS, 8);
    }

    @Test
    void serviceCountsReportJoinsServiceAndOrderTables() throws Exception {
        var report = load(ReportType.SERVICE_COUNTS);
        assertShape(report, ReportType.SERVICE_COUNTS, 2);
        assertFalse(report.chartSeriesName().isBlank());
    }

    @Test
    void monthlyRevenueReportGroupsClosedOrdersByMonth() throws Exception {
        var report = load(ReportType.MONTHLY_REVENUE);
        assertShape(report, ReportType.MONTHLY_REVENUE, 3);
        assertFalse(report.chartSeriesName().isBlank());
    }

    @Test
    void orderStatusReportReturnsStatusAggregates() throws Exception {
        var report = load(ReportType.ORDER_STATUSES);
        assertShape(report, ReportType.ORDER_STATUSES, 3);
        assertTrue(report.rows().stream().allMatch(row -> row.size() == 3));
    }

    @Test
    void customerActivityReportJoinsCustomersCarsAndOrders() throws Exception {
        var report = load(ReportType.CUSTOMER_ACTIVITY);
        assertShape(report, ReportType.CUSTOMER_ACTIVITY, 5);
        assertTrue(report.rows().stream().allMatch(row -> row.size() == 5));
    }

    @Test
    void directorAuthenticationAcceptsTheSharedSeedAccount() throws Exception {
        requireDatabaseIntegration();
        var config = AppConfig.load();
        assumeTrue(!config.demoDirectorPassword().isBlank(), "DEMO_DIRECTOR_PASSWORD is required for this check.");
        var connections = new DatabaseConnectionFactory(config);
        var authentication = new DirectorAuthenticationService(
            new JdbcDirectorRepository(connections),
            new PasswordHashVerifier()
        );

        var director = authentication.authenticate("demo.director", config.demoDirectorPassword());

        assertTrue(director.isPresent());
        assertEquals("director", director.orElseThrow().role());
    }

    @Test
    void directorAuthenticationRejectsASeededManagerAccount() throws Exception {
        requireDatabaseIntegration();
        var config = AppConfig.load();
        var authentication = new DirectorAuthenticationService(
            new JdbcDirectorRepository(new DatabaseConnectionFactory(config)),
            new PasswordHashVerifier()
        );

        assertTrue(authentication.authenticate("demo.manager", "any-password").isEmpty());
    }

    private static ReportResult load(ReportType type) throws Exception {
        requireDatabaseIntegration();
        var config = AppConfig.load();
        var service = new ReportService(new JdbcReportRepository(new DatabaseConnectionFactory(config)));
        return service.load(type, FROM, TO);
    }

    private static void assertShape(ReportResult report, ReportType type, int columnCount) {
        assertEquals(type, report.type());
        assertEquals(columnCount, report.columns().size());
        assertNotNull(report.rows());
        assertTrue(report.rows().stream().allMatch(row -> row.size() == columnCount));
    }

    private static void requireDatabaseIntegration() throws IOException {
        assumeTrue("1".equals(System.getenv("RUN_DATABASE_INTEGRATION_TESTS")),
            "Set RUN_DATABASE_INTEGRATION_TESTS=1 to run PostgreSQL integration tests.");
    }
}
