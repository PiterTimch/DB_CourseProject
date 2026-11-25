package ua.zunu.reports.domain;

import org.junit.jupiter.api.Test;

import java.time.LocalDate;

import static org.junit.jupiter.api.Assertions.*;

final class DateRangeTest {
    @Test
    void accepts_single_day_period() {
        var date = LocalDate.of(2026, 11, 25);
        assertEquals(new DateRange(date, date), new DateRange(date, date));
    }

    @Test
    void rejects_missing_dates() {
        assertThrows(IllegalArgumentException.class, () -> new DateRange(null, LocalDate.now()));
        assertThrows(IllegalArgumentException.class, () -> new DateRange(LocalDate.now(), null));
    }

    @Test
    void rejects_start_after_end() {
        assertThrows(IllegalArgumentException.class,
            () -> new DateRange(LocalDate.of(2026, 11, 26), LocalDate.of(2026, 11, 25)));
    }
}
