package ua.zunu.reports.service;

import org.junit.jupiter.api.Test;
import ua.zunu.reports.data.ReportRepository;
import ua.zunu.reports.domain.DateRange;
import ua.zunu.reports.domain.ReportResult;
import ua.zunu.reports.domain.ReportType;

import java.time.LocalDate;
import java.util.List;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;

final class ReportServiceTest {
    @Test
    void passes_the_selected_date_range_to_repository() throws Exception {
        var from = LocalDate.of(2026, 1, 1);
        var to = LocalDate.of(2026, 1, 31);
        var expectedRange = new DateRange(from, to);
        var expected = new ReportResult(ReportType.ORDERS, expectedRange, List.of("Дата"), List.of(), List.of(), "");
        ReportRepository repository = (type, range) -> {
            assertEquals(ReportType.ORDERS, type);
            assertEquals(expectedRange, range);
            return expected;
        };

        assertEquals(expected, new ReportService(repository).load(ReportType.ORDERS, from, to));
    }

    @Test
    void rejects_missing_report_type() {
        var service = new ReportService((type, range) -> null);
        assertThrows(IllegalArgumentException.class,
            () -> service.load(null, LocalDate.now(), LocalDate.now()));
    }
}
