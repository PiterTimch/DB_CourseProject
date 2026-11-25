package ua.zunu.reports.domain;

import org.junit.jupiter.api.Test;

import java.time.LocalDate;
import java.util.ArrayList;
import java.util.List;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;

final class ReportResultTest {
    @Test
    void copies_input_collections_and_rows() {
        var columns = new ArrayList<>(List.of("Послуга"));
        var row = new ArrayList<>(List.of("Діагностика"));
        var rows = new ArrayList<List<String>>();
        rows.add(row);
        var points = new ArrayList<>(List.of(new ReportPoint("Діагностика", 1)));

        var result = new ReportResult(ReportType.SERVICE_COUNTS,
            new DateRange(LocalDate.of(2026, 1, 1), LocalDate.of(2026, 1, 31)),
            columns, rows, points, null);
        columns.add("Зайве");
        row.set(0, "Змінено");
        rows.clear();
        points.clear();

        assertEquals(List.of("Послуга"), result.columns());
        assertEquals(List.of(List.of("Діагностика")), result.rows());
        assertEquals(1, result.chartPoints().size());
        assertEquals("", result.chartSeriesName());
        assertThrows(UnsupportedOperationException.class, () -> result.columns().add("Нове"));
    }
}
