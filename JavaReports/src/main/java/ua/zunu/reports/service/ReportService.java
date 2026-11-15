package ua.zunu.reports.service;

import ua.zunu.reports.data.ReportRepository;
import ua.zunu.reports.domain.DateRange;
import ua.zunu.reports.domain.ReportResult;
import ua.zunu.reports.domain.ReportType;

import java.sql.SQLException;
import java.time.LocalDate;

public final class ReportService {
    private final ReportRepository reports;

    public ReportService(ReportRepository reports) {
        this.reports = reports;
    }

    public ReportResult load(ReportType type, LocalDate from, LocalDate to) throws SQLException {
        if (type == null) {
            throw new IllegalArgumentException("Оберіть тип звіту.");
        }
        return reports.load(type, new DateRange(from, to));
    }
}
