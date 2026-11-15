package ua.zunu.reports.domain;

import java.time.LocalDate;

public record DateRange(LocalDate from, LocalDate to) {
    public DateRange {
        if (from == null || to == null) {
            throw new IllegalArgumentException("Вкажіть початкову та кінцеву дати.");
        }
        if (from.isAfter(to)) {
            throw new IllegalArgumentException("Початкова дата не може бути пізніше кінцевої.");
        }
    }
}
