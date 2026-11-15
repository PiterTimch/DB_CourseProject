package ua.zunu.reports.data;

import ua.zunu.reports.domain.DateRange;
import ua.zunu.reports.domain.ReportResult;
import ua.zunu.reports.domain.ReportType;

import java.sql.SQLException;

public interface ReportRepository {
    ReportResult load(ReportType type, DateRange range) throws SQLException;
}
